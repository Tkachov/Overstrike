using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Overstrike {
	public class Profile {
        public const string GAME_MSMR = "MSMR";
		public const string GAME_MM = "MM";

		// meta
		public string FullPath;
        public string Name;

        // game
        public string Game;
        public string GamePath;

        public Profile(string filename) {
            FullPath = filename;
            Name = Path.GetFileName(filename).Replace(".json", "");

			JObject json = JObject.Parse(File.ReadAllText(FullPath));
			Game = (string)json["game"];
			GamePath = (string)json["path"];

            if (Game == null || GamePath == null) { throw new Exception("bad profile"); }
		}

        public Profile(string name, string game, string gamePath) {
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
                File.WriteAllText(FullPath, j.ToString());
                return true;
            } catch (Exception) {}

            return false;
        }
    }
}
