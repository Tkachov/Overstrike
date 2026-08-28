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
	internal partial class SuitsMenuInstaller_MSM2: InstallerBase_I29 {
		private SuitsModifications _modifications;
		private readonly bool _allowCrossCharacterSuitModels;
		private readonly Dictionary<string, SuitModelPaths> _modelPathsCache = new(System.StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, MSM2SuitCharacter?> _suitCharacterCache = new(System.StringComparer.OrdinalIgnoreCase);
		private static readonly HashSet<string> SPIDER_ARMS_MODELS = new(System.StringComparer.Ordinal) {
			"hero_spiderman_advanced_legs",
			"hero_spiderman_momoko_legs",
			"hero_spiderman_superior_legs",
			"hero_spiderman_itsvnoir_legs",
			"hero_spiderman_ironspider_legs",
			"hero_spiderman_iw_legs"
		};

		public SuitsMenuInstaller_MSM2(TOC_I29 toc, string gamePath, SuitsModifications suits, bool allowCrossCharacterSuitModels): base(toc, gamePath) {
			_modifications = suits;
			_allowCrossCharacterSuitModels = allowCrossCharacterSuitModels;
		}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;
			_modelPathsCache.Clear();
			_suitCharacterCache.Clear();

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

			var forceRequests = new List<SuitModelRequest>();
			var spiderArmsRequests = new List<SpiderArmsRequest>();
			var modify = _modifications.Modifications;
			foreach (var suit in oldSuits) {
				var name = (string)suit["Name"];
				if (deletedSuits.ContainsKey(name)) continue;

				if (modify.ContainsKey(name)) {
					var changes = modify[name];

					if (MSM2CutsceneSuits.IsEligible(name) && (bool?)changes["ignore_story_progression"] == true) {
						IgnoreStorySuitProgression(suit);
					}

					if (changes.ContainsKey("small_icon")) {
						var icon = (string)changes["small_icon"];
						if (suit["Icon"] is JObject iconObj) {
							iconObj["AssetPath"] = icon;
						}
					}

					if (changes.ContainsKey("model")) {
						var sourceItem = (string)changes["model"];
						if (!string.IsNullOrEmpty(sourceItem)) {
							forceRequests.Add(new SuitModelRequest(name, (string)suit["Item"], sourceItem));
						}
					}

					if (changes.ContainsKey("force_arms")) {
						var armsModel = (string?)changes["force_arms"];
						if (!string.IsNullOrEmpty(armsModel)) {
							if (!MSM2SpiderArmsBlockedSuits.IsBlocked(name)) {
								spiderArmsRequests.Add(new SpiderArmsRequest(name, (string)suit["Item"], armsModel));
							}
						}
					}
				}
			}

			var newSuits = BuildMenuSuitList(oldSuits, deletedSuits);
			if (newSuits.Count == 0) {
				ErrorLogger.WriteInfo("Bad user preferences: can't have 0 suits!");
				throw new System.Exception();
			}
			ValidateMenuSuitCharacters(newSuits);

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

			var configBytes = config.Save();
			var configHeader = PrepareConfigHeader(SYSTEM_PROGRESSION_CONFIG_AID, configBytes.Length, "system_progression.config");
			ApplyForcedSuitModels(forceRequests);
			var rewardConfigs = ApplyForcedSpiderArms(spiderArmsRequests);
			WriteSuitsMenuArchive(SYSTEM_PROGRESSION_CONFIG_AID, configBytes, configHeader, rewardConfigs);
		}

		private static void IgnoreStorySuitProgression(JObject suit) {
			suit["Hidden"] = false;
			if (suit["MissionUnlocked"] != null) {
				suit["MissionUnlocked"] = "GP_A1_SANDMAN";
			}
			if (suit["ObjectiveUnlocked"] != null) {
				suit["ObjectiveUnlocked"] = "GP_A1_SANDMAN";
			}
			if (suit["ScriptedRequirement"] != null) {
				suit["ScriptedRequirement"] = "";
			}
			if (suit["HideAfterMissionName"] != null) {
				suit["HideAfterMissionName"] = "";
			}
			if (suit["HideAfterMissionObjectiveName"] != null) {
				suit["HideAfterMissionObjectiveName"] = "";
			}
			if (suit["HideSuitAfterMissionObjective"] != null) {
				suit["HideSuitAfterMissionObjective"] = false;
			}

			if (suit["PlayMoreMsgData"] is JObject playMoreMsgData) {
				if (playMoreMsgData["MissionOnCompleteStopMsg"] != null) {
					playMoreMsgData["MissionOnCompleteStopMsg"] = "GP_A1_SANDMAN";
				}
				if (playMoreMsgData["ObjectiveOnCompleteStopMsg"] != null) {
					playMoreMsgData["ObjectiveOnCompleteStopMsg"] = "GP_A1_SANDMAN";
				}
			}
		}

		private static List<JObject> BuildMenuSuitList(List<JObject> processedSuits, Dictionary<string, bool> deletedSuits) {
			var menuSuits = new List<JObject>();
			foreach (var suit in processedSuits) {
				var name = (string?)suit["Name"];
				if (string.IsNullOrEmpty(name) || deletedSuits.ContainsKey(name)) continue;

				// Preserve the existing Suit Menu behavior for visible vanilla suits: show their
				// cards even when the base progression entry starts as Hidden.
				if (suit["Hidden"] != null) {
					suit["Hidden"] = false;
				}
				menuSuits.Add(suit);
			}
			return menuSuits;
		}

		private void ValidateMenuSuitCharacters(List<JObject> menuSuits) {
			var hasPeter = false;
			var hasMiles = false;
			foreach (var suit in menuSuits) {
				var character = ResolveSuitCharacter((string?)suit["Item"] ?? "");
				if (character == MSM2SuitCharacter.Peter) {
					hasPeter = true;
				}
				if (character == MSM2SuitCharacter.Miles) {
					hasMiles = true;
				}
				if (hasPeter && hasMiles) return;
			}

			ErrorLogger.WriteInfo("Bad user preferences: MSM2 Suit Menu needs at least one verified Peter suit and one verified Miles suit.\n");
			throw new InvalidDataException("MSM2 Suit Menu has no verified visible suit for one of its characters");
		}

		private MSM2SuitCharacter? ResolveSuitCharacter(string rewardLoadoutPath) {
			if (string.IsNullOrEmpty(rewardLoadoutPath)) return null;
			rewardLoadoutPath = DAT1.Utils.Normalize(rewardLoadoutPath);
			if (_suitCharacterCache.TryGetValue(rewardLoadoutPath, out var cached)) return cached;

			var result = MSM2SuitCharacterResolver.TryResolve(_toc, rewardLoadoutPath);
			_suitCharacterCache[rewardLoadoutPath] = result;
			return result;
		}
	}
}
