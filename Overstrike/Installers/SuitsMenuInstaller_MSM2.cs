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
	internal class SuitsMenuInstaller_MSM2: InstallerBase_I29 {
		private SuitsModifications _modifications;

		public SuitsMenuInstaller_MSM2(TOC_I29 toc, string gamePath, SuitsModifications suits): base(toc, gamePath) {
			_modifications = suits;
		}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			const ulong SYSTEM_PROGRESSION_CONFIG_AID = 0x9C9C72A303FCFA30; // configs/system/system_progression.config
			var config = new Config_I30(_toc.GetAssetReader((byte)0, SYSTEM_PROGRESSION_CONFIG_AID));

			// read suits

			var root = config.ContentSection.Data;
			var suits = (JArray)root["SuitList"]["Suits"];

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
			var modify = _modifications.Modifications;
			foreach (var suit in oldSuits) {
				var name = (string)suit["Name"];

				// Only modify Hidden for vanilla suits (those that already have the field)
				// or for suits being deleted. Don't touch mod-added suits' Hidden field.
				if (deletedSuits.ContainsKey(name)) {
					suit["Hidden"] = true;
				} else if (suit["Hidden"] != null) {
					suit["Hidden"] = false;
				}

				if (modify.ContainsKey(name)) {
					var changes = modify[name];

					if (changes.ContainsKey("small_icon")) {
						var icon = (string)changes["small_icon"];
						if (suit["Icon"] is JObject iconObj) {
							iconObj["AssetPath"] = icon;
						}
					}

					if (changes.ContainsKey("model")) {
						suit["Item"] = (string)changes["model"];
					}
				}

				newSuits.Add(suit);
			}

			// reorder (push deleted suits to end)

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
				var aDeleted = deletedSuits.ContainsKey(aname);
				var bDeleted = deletedSuits.ContainsKey(bname);

				// push deleted suits to end
				if (aDeleted != bDeleted) return aDeleted ? 1 : -1;

				var ai = suitsOrder.ContainsKey(aname) ? suitsOrder[aname] : newSuits.Count;
				var bi = suitsOrder.ContainsKey(bname) ? suitsOrder[bname] : newSuits.Count;
				if (ai != bi) return ai - bi;

				ai = originalOrder[aname];
				bi = originalOrder[bname];
				if (ai != bi) return ai - bi;

				return aname.CompareTo(bname);
			});

			// apply changes to config

			var newSuitsArray = new JArray();
			foreach (var suit in newSuits) newSuitsArray.Add(suit);
			root["SuitList"]["Suits"] = newSuitsArray;
			config.ContentSection.Data = root;

			// save

			var modsPath = Path.Combine(_gamePath, "d", "mods");
			var archivePath = Path.Combine(modsPath, "suits_menu");
			var archiveIndex = GetArchiveIndex(Path.GetRelativePath(_gamePath, archivePath));

			var configBytes = config.Save();
			File.WriteAllBytes(archivePath, configBytes);

			OverwriteAsset(
				0, SYSTEM_PROGRESSION_CONFIG_AID,
				archiveIndex, 0, (uint)configBytes.Length,
				null, null
			);
		}
	}
}
