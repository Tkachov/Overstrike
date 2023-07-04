using Newtonsoft.Json.Linq;
using System.IO;

namespace Overstrike {
	public class Profile {
        public static readonly string GAME_MSMR = "MSMR";

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

            if (Game == null || GamePath == null) { throw new System.Exception("bad profile"); }
		}
    }
}
