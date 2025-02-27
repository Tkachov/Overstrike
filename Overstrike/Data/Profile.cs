// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
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
		public bool Settings_Scripts_ModToc;

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
			Settings_Scripts_ModToc = false;

			Suits = null;
		}

		public Profile(string filename): this() {
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
					Settings_Scripts_ModToc = (bool)scripts["mod_toc"];
				}
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
						["mod_toc"] = Settings_Scripts_ModToc,
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
