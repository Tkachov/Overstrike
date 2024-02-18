// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using Overstrike.Data;
using Overstrike.Utils;
using System.Collections.Generic;
using System.IO;

namespace Overstrike.Installers {
	internal class SuitsMenuInstaller: InstallerBase_I20 {
		private SuitsModifications _modifications;

		public SuitsMenuInstaller(TOC_I20 toc, string gamePath, SuitsModifications suits) : base(toc, gamePath) {
			_modifications = suits;
		}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			const ulong SYSTEM_PROGRESSION_CONFIG_AID = 0x9C9C72A303FCFA30; // configs/system/system_progression.config
			var config = new Config(_toc.GetAssetReader(SYSTEM_PROGRESSION_CONFIG_AID));
			bool isMM = config.HasString("ThumbnailImage");

			// read suits

			var root = config.ContentSection.Root;
			JArray suits = null;
			foreach (var techlist in root["TechWebLists"]) {
				if ((string)techlist["Description"] == "Suits") {
					suits = (JArray)techlist["TechWebItems"];
				}
			}

			if (suits == null) {
				ErrorLogger.WriteInfo("Corrupted .config: no suits found!");
				throw new System.Exception();
			}

			// make new suits

			var oldSuits = new List<JObject>();
			foreach (var suit in suits) {
				oldSuits.Add((JObject)suit);
			}

			var deletedSuits = new Dictionary<string, bool>();
			foreach (var suit in _modifications.DeletedSuits) {
				deletedSuits.Add(suit, true);
			}

			var newSuits = new List<JObject>();
			var availableSuits = new HashSet<string>();
			var modify = _modifications.Modifications;
			foreach (var suit in oldSuits) {
				var name = (string)suit["Name"];
				if (deletedSuits.ContainsKey(name)) continue;

				if (modify.ContainsKey(name)) {
					var changes = modify[name];
					if (changes.ContainsKey("small_icon")) {
						var icon = (string)changes["small_icon"];
						if (isMM) {
							suit["ThumbnailImage"] = icon;
						} else {
							suit["PreviewImage"] = icon;
						}
					}

					if (isMM) {
						if (changes.ContainsKey("big_icon")) {
							var icon = (string)changes["big_icon"];
							suit["PreviewImage"] = icon;
						}
					}

					if (changes.ContainsKey("model")) {
						var loadout = (string)changes["model"];

						JToken GivesItems = suit["GivesItems"];
						if (GivesItems != null) {
							JObject GivesItem = GivesItems as JObject;
							if (GivesItem == null) {
								JArray GivesItemsArray = GivesItems as JArray;
								if (GivesItemsArray != null && GivesItemsArray.Count > 0) {
									GivesItem = (JObject)GivesItems[0];
								}
							}

							if (GivesItem != null) {
								GivesItem["Item"] = loadout;
							}
						}
					}
				}

				newSuits.Add(suit);
				availableSuits.Add(name);
			}

			// reorder

			var suitsOrder = new Dictionary<string, int>();
			var order = _modifications.SuitsOrder;
			for (int i = 0; i < order.Count; ++i) {
				suitsOrder.Add(order[i], i);
			}

			var originalOrder = new Dictionary<string, int>();
			for (int i = 0; i < oldSuits.Count; ++i) {
				originalOrder.Add((string)oldSuits[i]["Name"], i);
			}

			newSuits.Sort((a, b) => {
				var aname = (string)a["Name"];
				var bname = (string)b["Name"];
				var ai = suitsOrder.ContainsKey(aname) ? suitsOrder[aname] : newSuits.Count;
				var bi = suitsOrder.ContainsKey(bname) ? suitsOrder[bname] : newSuits.Count;
				if (ai != bi) {
					return ai - bi;
				}

				ai = originalOrder[aname];
				bi = originalOrder[bname];
				if (ai != bi) {
					return ai - bi;
				}

				return aname.CompareTo(bname);
			});

			// apply changes to config

			if (newSuits.Count == 0) {
				ErrorLogger.WriteInfo("Bad user preferences: can't have 0 suits!");
				throw new System.Exception();
			}

			var newSuitsArray = new JArray();
			foreach (var suit in newSuits) newSuitsArray.Add(suit);

			foreach (var techlist in root["TechWebLists"]) {
				if ((string)(techlist["Description"]) == "Suits") {
					techlist["TechWebItems"] = newSuitsArray;
					break;
				}
			}

			JArray origUnlockedArray = (root["UnlockForFree"] is JArray ? (JArray)root["UnlockForFree"] : new JArray { root["UnlockForFree"] });
			JArray newUnlocked = new JArray();
			foreach (var suit_id in origUnlockedArray) {
				if (availableSuits.Contains((string)suit_id)) {
					newUnlocked.Add(suit_id);
				}
			}
			if (newUnlocked.Count == 0) {
				newUnlocked.Add(newSuits[0]["Name"]);
			}
			root["UnlockForFree"] = newUnlocked;

			// save

			var suitsPath = Path.Combine(_gamePath, "asset_archive", "Suits");
			byte[] moddedBytes = config.Save();
			WriteArchive(suitsPath, "base1", SYSTEM_PROGRESSION_CONFIG_AID, moddedBytes);
		}

		#region toc

		// copied from SuitInstallerBase

		protected uint GetArchiveIndex(string filename) => _toc.FindOrAddArchive(filename, TOCBase.ArchiveAddingImpl.SUITTOOL);

		protected void WriteArchive(string archivePath, uint archiveIndex, ulong assetId, byte span, byte[] bytes) {
			File.WriteAllBytes(archivePath, bytes);
			AddOrUpdateAssetEntry(span, assetId, archiveIndex, /*offset=*/0, (uint)bytes.Length);
		}

		protected void WriteArchive(string suitsPath, string archiveName, ulong assetId, byte[] bytes) {
			WriteArchive(suitsPath, archiveName, assetId, 0, bytes);
		}

		protected void WriteArchive(string suitsPath, string archiveName, ulong assetId, byte span, byte[] bytes) {
			var archivePath = Path.Combine(suitsPath, archiveName);
			var archiveIndex = GetArchiveIndex("Suits\\" + archiveName);
			WriteArchive(archivePath, archiveIndex, assetId, span, bytes);
		}

		#endregion
	}
}
