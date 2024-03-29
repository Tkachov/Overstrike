﻿// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using System.IO.Compression;
using System.IO;
using System;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Overstrike.Data;

namespace Overstrike.Installers {
	internal class MMSuit2Installer: SuitInstallerBase {
		private string _language;

		public MMSuit2Installer(TOC_I20 toc, string gamePath, string language = "") : base(toc, gamePath) {
			if (MMSuit1Installer.LANGUAGES.ContainsKey(language))
				_language = language;
			else
				_language = "";
		}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			var suitsPath = Path.Combine(_gamePath, "asset_archive", "Suits");

			using (ZipArchive zip = ReadModFile()) {
				var idTxt = GetEntryByName(zip, "id.txt");
				var id = ReadId(idTxt);
				var name = ReadName(zip);
				var writeName = (_language != "");

				WriteAssetsFile(zip, suitsPath, id);
				WriteBaseFile(suitsPath, id, writeName);

				if (writeName) {
					WriteLanguage(suitsPath, id, name);
				}
			}

			_toc.SortAssets();
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

				if (infoTxt.Length % 17 != 2) {
					throw new Exception("Unexpected 'info.txt' length!");
				}

				var firstByte = r.ReadByte();
				if (firstByte != 2) {
					throw new Exception("Unexpected version value!");
				}

				for (var i = 0; i < infoTxt.Length / 17; ++i) {
					uint offset = r.ReadUInt32();
					uint size = r.ReadUInt32();
					ulong assetId = r.ReadUInt64();
					byte span = r.ReadByte();

					AddOrUpdateAssetEntry(span, assetId, suitArchiveIndex, offset, size);
				}
			}
		}

		private struct AssetToWrite {
			public byte[] bytes;
			public ulong assetId;
		};

		private void WriteBaseFile(string suitsPath, string id, bool writeName) {
			var progression = ModBase1(id, writeName);
			var loadoutlist = ModBase2(id);
			var vanitylist = ModBase3(id);
			var vanitylistlaunch = ModBase4(id);
			var characterlist = ModBase5(id);

			var baseArchiveIndex = GetArchiveIndex("Suits\\base");
			var baseArchivePath = Path.Combine(suitsPath, "base");

			var f = File.OpenWrite(baseArchivePath);
			using (var w = new BinaryWriter(f)) {
				WriteAssets(
					new List<AssetToWrite>() {
						progression,
						loadoutlist,
						vanitylist,
						vanitylistlaunch,
						characterlist
					},

					w, baseArchiveIndex
				);
			}
		}

		private void WriteAssets(List<AssetToWrite> assets, BinaryWriter w, uint archiveIndex) {
			uint offset = 0;
			foreach (var asset in assets) {
				w.Write(asset.bytes);
				AddOrUpdateAssetEntry(0, asset.assetId, archiveIndex, offset, (uint)asset.bytes.Length);
				offset += (uint)asset.bytes.Length;
			}
		}

		private AssetToWrite ModBase1(string id, bool writeName) {
			const ulong SYSTEM_PROGRESSION_CONFIG_AID = 0x9C9C72A303FCFA30; // configs/system/system_progression.config
			var config = new Config(_toc.GetAssetReader(SYSTEM_PROGRESSION_CONFIG_AID));

			// references
			var rewardRef = "configs\\inventory\\inv_reward_loadout_" + id + ".config";
			var chestIconRef = "ui\\textures\\pause\\character\\suit_chest\\suit_image_" + id + ".texture";
			var equipRef = "configs\\equipment\\equip_techweb_suit_" + id + ".config";
			var thumbIconRef = "ui\\textures\\pause\\character\\suit_thumbs\\suit_" + id + ".texture";
			AddConfigReference(config, rewardRef);
			AddConfigReference(config, chestIconRef);
			AddConfigReference(config, equipRef);
			AddConfigReference(config, thumbIconRef);

			// Suits
			var suit = "SUIT_" + id.ToUpper();
			var nameKey = (writeName ? suit : "SUIT_MILES_2020");
			AddToSuitsList(config, suit, rewardRef, chestIconRef, equipRef, thumbIconRef, nameKey);

			return new AssetToWrite {
				bytes = config.Save(),
				assetId = SYSTEM_PROGRESSION_CONFIG_AID
			};
		}

		private void AddToSuitsList(Config config, string suit, string rewardRef, string chestIconRef, string equipRef, string thumbIconRef, string nameKey) {
			var rewardNormalized = DAT1.Utils.Normalize(rewardRef);
			var chestIconNormalized = DAT1.Utils.Normalize(chestIconRef);
			var equipNormalized = DAT1.Utils.Normalize(equipRef);
			var thumbIconNormalized = DAT1.Utils.Normalize(thumbIconRef);

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
			suitObject["PreviewImage"] = chestIconNormalized;
			suitObject["DisplayName"] = nameKey;
			suitObject["Description"] = nameKey;
			suitObject["ThumbnailImage"] = thumbIconNormalized;
			suitObject["Name"] = suit;
			suits.Add(suitObject);
		}
		
		private AssetToWrite ModBase2(string id) {
			const ulong MASTERITEMLOADOUTLIST_CONFIG_AID = 0x9550E5741C2C7114; // configs/masteritemloadoutlist/masteritemloadoutlist.config

			var config = new Config(_toc.GetAssetReader(MASTERITEMLOADOUTLIST_CONFIG_AID));

			// references
			var configRef = "configs\\masteritemloadoutlist\\itemloadout_spiderman_" + id + ".config";
			AddConfigReference(config, configRef);

			// ItemLoadoutConfigs
			AddToLoadoutConfigs(config, configRef);

			return new AssetToWrite {
				bytes = config.Save(),
				assetId = MASTERITEMLOADOUTLIST_CONFIG_AID
			};
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

		private AssetToWrite ModBase3(string id) {
			const ulong VANITYMASTERLIST_CONFIG_AID = 0x9CEADD22304ADD84; // configs/vanitymasterlist/vanitymasterlist.config

			var config = new Config(_toc.GetAssetReader(VANITYMASTERLIST_CONFIG_AID));
			ModVanityConfig(config, id);

			return new AssetToWrite {
				bytes = config.Save(),
				assetId = VANITYMASTERLIST_CONFIG_AID
			};
		}

		private AssetToWrite ModBase4(string id) {
			const ulong VANITYMASTERLISTLAUNCH_CONFIG_AID = 0x939887A999564798; // configs/vanitymasterlist/vanitymasterlistlaunch.config

			var config = new Config(_toc.GetAssetReader(VANITYMASTERLISTLAUNCH_CONFIG_AID));
			ModVanityConfig(config, id);

			return new AssetToWrite {
				bytes = config.Save(),
				assetId = VANITYMASTERLISTLAUNCH_CONFIG_AID
			};
		}

		private void ModVanityConfig(Config config, string id) {
			var configRef = "configs\\VanityBodyType\\VanityBody_" + id + ".config";
			AddConfigReference(config, configRef);
			AddToItemConfigs(config, configRef);
		}

		private void AddToItemConfigs(Config config, string configRef) {
			var configNormalized = DAT1.Utils.Normalize(configRef);

			var root = config.ContentSection.Root;
			JArray? itemsList = null;
			foreach (var categoryListItem in root["ItemCategoryList"]) {
				if ((string)categoryListItem["SubMenu"] == "kBodyMenu" && (string)categoryListItem["Category"] == "kBodySize") {
					itemsList = (JArray)categoryListItem["ItemConfigs"];
				}
			}

			if (!itemsList.Contains(configNormalized)) {
				itemsList.Add(configNormalized);
			}
		}

		private AssetToWrite ModBase5(string id) {
			const ulong CHARACTERLIST_CONFIG_AID = 0xB596B20DFC3C2820; // configs/hero/hero_characterlistconfig.config

			var config = new Config(_toc.GetAssetReader(CHARACTERLIST_CONFIG_AID));

			// references
			var defaultLoadoutRef = "configs\\masteritemloadoutlist\\itemloadout_spiderman_" + id + ".config";
			var damagedLoadoutRef = "configs\\masteritemloadoutlist\\itemloadout_spiderman_miles_" + id + "_suit_damaged.config";
			var masklessLoadoutRef = "configs\\masteritemloadoutlist\\itemloadout_milesmorales_" + id + "_suit_maskless.config";
			var masklessDamagedLoadoutRef = "configs\\masteritemloadoutlist\\itemloadout_milesmorales_" + id + "_suit_dmg_maskless.config";

			AddConfigReference(config, damagedLoadoutRef);
			AddConfigReference(config, defaultLoadoutRef);
			AddConfigReference(config, masklessDamagedLoadoutRef);
			AddConfigReference(config, masklessLoadoutRef);

			// SuitVariations
			var defaultLoadoutNormalized = DAT1.Utils.Normalize(defaultLoadoutRef);
			var damagedLoadoutNormalized = DAT1.Utils.Normalize(damagedLoadoutRef);
			var masklessLoadoutNormalized = DAT1.Utils.Normalize(masklessLoadoutRef);
			var masklessDamagedLoadoutNormalized = DAT1.Utils.Normalize(masklessDamagedLoadoutRef);

			var root = config.ContentSection.Root;
			var suits = (JArray)root["SuitVariations"];
			var found = false;
			foreach (var suit in suits) {
				if ((string)suit["DefaultVanityLoadout"] == defaultLoadoutNormalized) {
					found = true;
					break;
				}
			}

			if (!found) {
				var suitObject = new JObject();

				var damagedMaskModelObject = new JObject();
				damagedMaskModelObject["AssetPath"] = "characters/hero/hero_spiderman_miles_" + id + "/hero_spiderman_miles_" + id + "_dmg_mask.model";
				damagedMaskModelObject["Autoload"] = false;
				suitObject["DamagedMaskModel"] = damagedMaskModelObject;

				var maskModelObject = new JObject();
				maskModelObject["AssetPath"] = "characters/hero/hero_spiderman_miles_" + id + "/hero_spiderman_miles_" + id + "_mask.model";
				maskModelObject["Autoload"] = false;
				suitObject["MaskModel"] = maskModelObject;

				suitObject["DamagedVanityLoadout"] = damagedLoadoutNormalized;
				suitObject["MasklessDamagedVanityLoadout"] = masklessDamagedLoadoutNormalized;
				suitObject["MasklessVanityLoadout"] = masklessLoadoutNormalized;
				suitObject["DefaultVanityLoadout"] = defaultLoadoutNormalized;

				suits.Add(suitObject);
			}

			return new AssetToWrite {
				bytes = config.Save(),
				assetId = CHARACTERLIST_CONFIG_AID
			};
		}

		private void WriteLanguage(string suitsPath, string id, string name) {
			var languagesPath = Path.Combine(suitsPath, "languages");
			if (!Directory.Exists(languagesPath)) {
				Directory.CreateDirectory(languagesPath);
			}

			const ulong LOCALIZATION_AID = 0xBE55D94F171BF8DE; // localization/localization_all.localization
			byte span = MMSuit1Installer.LANGUAGES[_language];
			var l = new Localization(_toc.GetAssetReader(span, LOCALIZATION_AID));
			var h = new LocalizationHelper(l);

			var idUpper = id.ToUpper();
			h.Add($"SUIT_{idUpper}", name);
			h.Add($"SUIT_{idUpper}_SHORT", name);
			h.Apply(l);

			byte[] moddedBytes = l.Save();
			WriteArchive(suitsPath, "languages\\base_" + _language, LOCALIZATION_AID, span, moddedBytes);
		}
	}
}
