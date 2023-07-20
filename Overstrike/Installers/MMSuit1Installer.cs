using DAT1;
using System.IO.Compression;
using System.IO;
using System;
using DAT1.Files;
using Newtonsoft.Json.Linq;

namespace Overstrike.Installers {
	internal class MMSuit1Installer: SuitInstallerBase {
		public MMSuit1Installer(TOC toc, string gamePath) : base(toc, gamePath) {} // TODO: setting to install or ignore name.txt

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			var suitsPath = Path.Combine(_gamePath, "asset_archive", "Suits");

			using (ZipArchive zip = ReadModFile()) {
				var idTxt = GetEntryByName(zip, "id.txt");
				var id = ReadId(idTxt);

				WriteAssetsFile(zip, suitsPath, id);
				WriteBase1(suitsPath, id);
				WriteBase2(suitsPath, id);
				WriteBase3(suitsPath, id);
				WriteBase4(suitsPath, id);
				//WriteLanguageEn(suitsPath, id); // TODO: support name.txt
			}

			SortAssets();
		}

		private void WriteAssetsFile(ZipArchive zip, string suitsPath, string id) {
			var suitArchivePath = Path.Combine(suitsPath, id);
			var suitArchiveIndex = GetArchiveIndex("Suits\\" + id);
			var assets = GetEntryByFullName(zip, id + "/" + id);

			using (var f = new FileStream(suitArchivePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
				using (var w = new BinaryWriter(f)) {
					using (var stream = assets.Open()) {
						stream.CopyTo(w.BaseStream);
					}
				}
			}

			var infoTxt = GetEntryByName(zip, "info.txt");
			using (var stream = infoTxt.Open()) {
				var r = new BinaryReader(stream);

				if (infoTxt.Length % 21 != 2) {
					throw new Exception("Unexpected 'info.txt' length!");
				}

				var firstByte = r.ReadByte();
				if (firstByte != 1) {
					throw new Exception("Unexpected version value!");
				}

				for (var i = 0; i < infoTxt.Length / 21; ++i) {
					uint offset = r.ReadUInt32();
					uint unk = r.ReadUInt32();
					uint size = r.ReadUInt32();
					ulong assetId = r.ReadUInt64();
					byte span = r.ReadByte();

					AddOrUpdateAssetEntry(assetId, span, suitArchiveIndex, offset, size);
				}
			}
		}

		private void WriteBase1(string suitsPath, string id) {
			const ulong SYSTEM_PROGRESSION_CONFIG_AID = 0x9C9C72A303FCFA30; // configs/system/system_progression.config

			var config = new Config(GetAssetReader(SYSTEM_PROGRESSION_CONFIG_AID));

			// references
			var rewardRef = "configs\\inventory\\inv_reward_loadout_" + id + ".config";
			var iconRef = "ui\\textures\\pause\\character\\suit_" + id + ".texture";
			var equipRef = "configs\\equipment\\equip_techweb_suit_" + id + ".config";
			AddConfigReference(config, rewardRef);
			AddConfigReference(config, iconRef);
			AddConfigReference(config, equipRef);

			// Suits & UnlockForFree
			var suit = "SUIT_" + id;
			AddToSuitsList(config, suit, rewardRef, iconRef, equipRef);
			AddToUnlockForFree(config, suit);

			byte[] moddedBytes = config.Save();
			WriteArchive(suitsPath, "base1", SYSTEM_PROGRESSION_CONFIG_AID, moddedBytes);
		}

		private void AddToSuitsList(Config config, string suit, string rewardRef, string iconRef, string equipRef) {
			var rewardNormalized = DAT1.Utils.Normalize(rewardRef);
			var iconNormalized = DAT1.Utils.Normalize(iconRef);
			var equipNormalized = DAT1.Utils.Normalize(equipRef);

			var root = config.ContentSection.Root;
			JArray? suits = null;
			foreach (var techlist in root["TechWebLists"]) {
				if ((string)techlist["Description"] == "Suits") {
					suits = (JArray)techlist["TechWebItems"];
				}
			}

			foreach (var s in suits) {
				if ((string)s["Name"] == suit) {
					return; // already added
				}
			}

			var suitObject = new JObject();
			var itemObject = new JObject();
			itemObject["Item"] = rewardNormalized;
			suitObject["GivesItems"] = itemObject;
			suitObject["SkillItem"] = equipNormalized;
			suitObject["UnhideByItem"] = equipNormalized;
			suitObject["PreviewImage"] = iconNormalized;
			suitObject["DisplayName"] = "SUIT_MILES_2020"; // TODO: support name.txt
			suitObject["Description"] = "SUIT_MILES_2020";
			suitObject["Icon"] = "icon_tech_web_suit";
			suitObject["Name"] = suit;
			suits.Add(suitObject);
		}

		private void AddToUnlockForFree(Config config, string suit) {
			var root = config.ContentSection.Root;

			JArray uff;
			if (root["UnlockForFree"].Type != JTokenType.Array) {
				uff = new JArray();
				uff.Add(root["UnlockForFree"]);
			} else {
				uff = (JArray)root["UnlockForFree"];
			}

			if (!uff.Contains(suit)) {
				uff.Add(suit);
			}

			root["UnlockForFree"] = uff;
		}

		private void WriteBase2(string suitsPath, string id) {
			const ulong MASTERITEMLOADOUTLIST_CONFIG_AID = 0x9550E5741C2C7114; // configs/masteritemloadoutlist/masteritemloadoutlist.config

			var config = new Config(GetAssetReader(MASTERITEMLOADOUTLIST_CONFIG_AID));

			// references
			var configRef = "configs\\masteritemloadoutlist\\itemloadout_spiderman_" + id + ".config";
			AddConfigReference(config, configRef);

			// ItemLoadoutConfigs
			AddToLoadoutConfigs(config, configRef);

			byte[] moddedBytes = config.Save();
			WriteArchive(suitsPath, "base2", MASTERITEMLOADOUTLIST_CONFIG_AID, moddedBytes);
		}

		private void AddToLoadoutConfigs(Config config, string configRef) {
			var configNormalized = DAT1.Utils.Normalize(configRef);

			var root = config.ContentSection.Root;
			JArray? vanityList = null;
			foreach (var categoryList in root["ItemLoadoutCategoryList"]) {
				if ((string)categoryList["Category"] == "kVanity") {
					vanityList = (JArray)categoryList["ItemLoadoutConfigs"];
				}
			}

			if (!vanityList.Contains(configNormalized)) {
				vanityList.Add(configNormalized);
			}
		}

		private void WriteBase3(string suitsPath, string id) {
			const ulong VANITYMASTERLIST_CONFIG_AID = 0x9CEADD22304ADD84; // configs/vanitymasterlist/vanitymasterlist.config

			// unmodded
			byte[] asset = GetAssetBytes(VANITYMASTERLIST_CONFIG_AID);
			WriteArchive(suitsPath, "base3", VANITYMASTERLIST_CONFIG_AID, asset);
		}

		private void WriteBase4(string suitsPath, string id) {
			const ulong VANITYMASTERLISTLAUNCH_CONFIG_AID = 0x939887A999564798; // configs/vanitymasterlist/vanitymasterlistlaunch.config

			// unmodded
			byte[] asset = GetAssetBytes(VANITYMASTERLISTLAUNCH_CONFIG_AID);
			WriteArchive(suitsPath, "base4", VANITYMASTERLISTLAUNCH_CONFIG_AID, asset);
		}

		private void WriteLanguageEn(string suitsPath, string id) {
			var languagesPath = Path.Combine(suitsPath, "languages");
			if (!Directory.Exists(languagesPath)) {
				Directory.CreateDirectory(languagesPath);
			}

			const ulong LOCALIZATION_AID = 0xBE55D94F171BF8DE; // localization/localization_all.localization
			byte[] asset = GetAssetBytes(LOCALIZATION_AID);
			WriteArchive(suitsPath, "languages\\base_en", LOCALIZATION_AID, 8, asset);
		}
	}
}
