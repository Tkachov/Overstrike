// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using Overstrike;
using System;
using System.Collections.Generic;
using System.IO;

namespace Overstrike.Data {
	public class Profile {
		// meta
		public string FullPath;
		public string Name;

		// game
		public string Game;
		public string GamePath;

		// mods
		public List<ModEntry> Mods;

		// settings > suit
		public string Settings_Suit_Language;

		// settings > scripts
		public bool Settings_Scripts_Enabled;
		public bool Settings_Scripts_CommandLine;

		// settings > suit_menu
		public bool Settings_SuitMenu_AllowCrossCharacterSuitModels;
		public bool Settings_SuitMenu_EnableSpiderArms;
		public bool Settings_SuitMenu_EnableChangeModel;
		public bool Settings_SuitMenu_EnableStoryProgressionOverride;

		// suits
		public SuitsModifications Suits;

		protected Profile() {
			Name = null;
			FullPath = null;
			Game = null;
			GamePath = null;

			Mods = new List<ModEntry>();

			Settings_Suit_Language = "us";
			Settings_Scripts_Enabled = false;
			Settings_Scripts_CommandLine = false;
			Settings_SuitMenu_AllowCrossCharacterSuitModels = false;
			Settings_SuitMenu_EnableSpiderArms = false;
			Settings_SuitMenu_EnableChangeModel = false;
			Settings_SuitMenu_EnableStoryProgressionOverride = false;

			Suits = null;
		}

		public Profile(string filename, AppSettings legacySettings = null): this() {
			FullPath = filename;
			Name = Path.GetFileName(filename).Replace(".json", "");

			JObject json = JObject.Parse(File.ReadAllText(FullPath));
			Game = (string)json["game"];
			GamePath = (string)json["path"];

			if (Game == null || GamePath == null) { throw new Exception("bad profile"); }

			var mods = (JArray)json["mods"];
			if (mods == null) { throw new Exception("bad profile"); }

			foreach (var mod in mods) {
				var path = (string?)mod[0];
				var install = (bool?)mod[1];
				JObject extras = null;

				if (path == null || install == null) continue; // { throw new Exception("bad profile"); }

				try {
					var modArr = (JArray)mod;
					if (modArr.Count > 2) {
						extras = (JObject?)modArr[2];
					}
				} catch {}

				Mods.Add(new ModEntry(path, (bool)install, extras));
			}

			var settings = (JObject)json["settings"];
			JObject suitMenu = null;
			if (settings != null) {
				var suit = (JObject)settings["suit"];
				if (suit != null) {
					Settings_Suit_Language = (string)suit["language"];
					if (Settings_Suit_Language == null) { throw new Exception("bad profile"); }
				}

				if (settings.ContainsKey("scripts")) {
					var scripts = (JObject)settings["scripts"];
					if (scripts == null) { throw new Exception("bad profile"); }

					Settings_Scripts_Enabled = (bool)scripts["enabled"];
					if (scripts.ContainsKey("commandline")) {
						Settings_Scripts_CommandLine = (bool)scripts["commandline"];
					} else {
						Settings_Scripts_CommandLine = false;
					}
				}

				suitMenu = (JObject)settings["suit_menu"];
				if (suitMenu != null) {
					Settings_SuitMenu_AllowCrossCharacterSuitModels = (bool?)suitMenu["allow_cross_character_suit_models"] ?? false;
					Settings_SuitMenu_EnableSpiderArms = (bool?)suitMenu["enable_spider_arms"] ?? false;
					Settings_SuitMenu_EnableChangeModel = (bool?)suitMenu["enable_change_model"] ?? false;
					Settings_SuitMenu_EnableStoryProgressionOverride = (bool?)suitMenu["enable_story_progression_override"] ?? false;
				}
			}

			// No "suit_menu" section yet: either an old profile predating per-profile Suit Menu
			// settings, or a fresh one with no settings block at all. Migrate the app-wide values
			// this profile would have used before, instead of silently defaulting to disabled.
			if (suitMenu == null && legacySettings != null) {
				Settings_SuitMenu_AllowCrossCharacterSuitModels = legacySettings.Legacy_AllowCrossCharacterSuitModels ?? false;
				Settings_SuitMenu_EnableSpiderArms = legacySettings.Legacy_EnableSuitMenuSpiderArms ?? false;
				Settings_SuitMenu_EnableChangeModel = legacySettings.Legacy_EnableSuitMenuChangeModel ?? false;
			}

			var suits = (JObject)json["suits"];
			Suits = new SuitsModifications(suits);
		}

		public Profile(string name, string game, string gamePath): this() {
			Name = name;
			FullPath = Path.Combine(Directory.GetCurrentDirectory(), "Profiles/", Name + ".json");

			Game = game;
			GamePath = gamePath;

			Suits = new SuitsModifications(null);
		}

		public bool Save() {
			try {
				JObject j = new JObject();
				j["game"] = Game;
				j["path"] = GamePath;

				JArray mods = new JArray();
				foreach (var mod in Mods) {
					var mod_desc = new JArray {
						mod.Path,
						mod.Install
					};
					if (mod.Extras != null) {
						mod_desc.Add(mod.Extras);
					}
					mods.Add(mod_desc);
				}
				j["mods"] = mods;

				j["settings"] = new JObject() {
					["suit"] = new JObject() {
						["language"] = Settings_Suit_Language
					},
					["scripts"] = new JObject() {
						["enabled"] = Settings_Scripts_Enabled,
						["commandline"] = Settings_Scripts_CommandLine,
					},
					["suit_menu"] = new JObject() {
						["allow_cross_character_suit_models"] = Settings_SuitMenu_AllowCrossCharacterSuitModels,
						["enable_spider_arms"] = Settings_SuitMenu_EnableSpiderArms,
						["enable_change_model"] = Settings_SuitMenu_EnableChangeModel,
						["enable_story_progression_override"] = Settings_SuitMenu_EnableStoryProgressionOverride,
					}
				};

				j["suits"] = Suits.Save();

				File.WriteAllText(FullPath, j.ToString());
				return true;
			} catch (Exception) {}

			return false;
		}
	}
}
