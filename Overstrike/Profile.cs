// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Overstrike {
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

        protected Profile() {
			Name = null;
			FullPath = null;            
            Game = null;
            GamePath = null;

			Mods = new List<ModEntry>();

			Settings_Suit_Language = "us";
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

                if (path == null || install == null) continue; // { throw new Exception("bad profile"); }

                Mods.Add(new ModEntry(path, (bool)install));
			}

            var settings = (JObject)json["settings"];
            if (settings != null) {
                var suit = (JObject)settings["suit"];
                if (suit != null) {
					Settings_Suit_Language = (string)suit["language"];
					if (Settings_Suit_Language == null) { throw new Exception("bad profile"); }
				}
            }
		}

        public Profile(string name, string game, string gamePath): this() {
            Name = name;
            FullPath = Path.Combine(Directory.GetCurrentDirectory(), "Profiles/", Name + ".json");

            Game = game;
            GamePath = gamePath;
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
					mods.Add(mod_desc);
                }
                j["mods"] = mods;

				j["settings"] = new JObject() {
					["suit"] = new JObject() {
						["language"] = Settings_Suit_Language
					}
				};

				File.WriteAllText(FullPath, j.ToString());
                return true;
            } catch (Exception) {}

            return false;
        }
    }
}
