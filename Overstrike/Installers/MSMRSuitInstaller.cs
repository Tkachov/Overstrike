// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
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

namespace Overstrike.Installers {
	internal class MSMRSuitInstaller: SuitInstallerBase {
		public static readonly Dictionary<string, byte> LANGUAGES = new() {
			//{"en", 0},
			{"us", 8},
			{"da", 24},
			{"nl", 32},
			{"fi", 40},
			{"fr", 48},
			{"de", 56},
			{"it", 64},
			{"jp", 72},
			{"ko", 80},
			{"no", 88},
			{"pl", 96},
			{"pt", 104},
			{"ru", 112},
			{"es", 120},
			{"sv", 128},
			{"br", 144},
			{"ar", 152},
			{"la", 168},
			{"zh", 184},
			{"cs", 200},
			{"hu", 208},
			{"el", 216},
		};

		private string _language;

		public MSMRSuitInstaller(TOC_I20 toc, string gamePath, string language = "") : base(toc, gamePath) {
			if (LANGUAGES.ContainsKey(language))
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
				WriteBase1(suitsPath, id, writeName);
				WriteBase2(suitsPath, id);
				WriteBase3(suitsPath, id);
				WriteBase4(suitsPath, id);

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

				if (infoTxt.Length % 21 == 2) {
					var firstByte = r.ReadByte();
					if (firstByte != 0) {
						throw new Exception("Unexpected version value!");
					}
				}

				if (infoTxt.Length % 21 != 1) {
					throw new Exception("Unexpected 'info.txt' length!");
				}

				for (var i = 0; i < infoTxt.Length / 21; ++i) {
					uint offset = r.ReadUInt32();
					uint unk = r.ReadUInt32();
					uint size = r.ReadUInt32();
					ulong assetId = r.ReadUInt64();
					byte span = r.ReadByte();

					AddOrUpdateAssetEntry(span, assetId, suitArchiveIndex, offset, size);
				}
			}
		}

		private void WriteBase1(string suitsPath, string id, bool writeName) {
			const ulong SYSTEM_PROGRESSION_CONFIG_AID = 0x9C9C72A303FCFA30; // configs/system/system_progression.config

			var config = new Config(_toc.GetAssetReader(SYSTEM_PROGRESSION_CONFIG_AID));

			// references
			var rewardRef = "configs\\inventory\\inv_reward_loadout_" + id + ".config";
			var iconRef = "ui\\textures\\pause\\character\\suit_" + id + ".texture";
			var equipRef = "configs\\equipment\\equip_techweb_suit_" + id + ".config";
			AddConfigReference(config, rewardRef);
			AddConfigReference(config, iconRef);
			AddConfigReference(config, equipRef);

			// Suits & UnlockForFree
			var suit = "SUIT_" + id;
			var nameKey = (writeName ? ("SUIT_" + id.ToUpper()) : "CHARWEB_MAY");
			AddToSuitsList(config, suit, rewardRef, iconRef, equipRef, nameKey);
			AddToUnlockForFree(config, suit);

			byte[] moddedBytes = config.Save();
			WriteArchive(suitsPath, "base1", SYSTEM_PROGRESSION_CONFIG_AID, moddedBytes);
		}

		private void AddToSuitsList(Config config, string suit, string rewardRef, string iconRef, string equipRef, string nameKey) {
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
			suitObject["DisplayName"] = nameKey;
			suitObject["Description"] = nameKey;
			suitObject["Icon"] = "icon_tech_web_suit";
			suitObject["Name"] = suit;
			suits.Add(suitObject);
		}

		private void AddToUnlockForFree(Config config, string suit) {
			var root = config.ContentSection.Root;
			JArray uff = (JArray)root["UnlockForFree"];
			if (!uff.Contains(suit)) {
				uff.Add(suit);
			}
		}

		private void WriteBase2(string suitsPath, string id) {
			const ulong MASTERITEMLOADOUTLIST_CONFIG_AID = 0x9550E5741C2C7114; // configs/masteritemloadoutlist/masteritemloadoutlist.config

			var config = new Config(_toc.GetAssetReader(MASTERITEMLOADOUTLIST_CONFIG_AID));

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
			byte[] asset = _toc.GetAssetBytes(VANITYMASTERLIST_CONFIG_AID);
			WriteArchive(suitsPath, "base3", VANITYMASTERLIST_CONFIG_AID, asset);
		}

		private void WriteBase4(string suitsPath, string id) {
			const ulong VANITYMASTERLISTLAUNCH_CONFIG_AID = 0x939887A999564798; // configs/vanitymasterlist/vanitymasterlistlaunch.config

			// unmodded
			byte[] asset = _toc.GetAssetBytes(VANITYMASTERLISTLAUNCH_CONFIG_AID);
			WriteArchive(suitsPath, "base4", VANITYMASTERLISTLAUNCH_CONFIG_AID, asset);
		}

		private void WriteLanguage(string suitsPath, string id, string name) {
			var languagesPath = Path.Combine(suitsPath, "languages");
			if (!Directory.Exists(languagesPath)) {
				Directory.CreateDirectory(languagesPath);
			}

			const ulong LOCALIZATION_AID = 0xBE55D94F171BF8DE; // localization/localization_all.localization
			byte span = LANGUAGES[_language];
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
