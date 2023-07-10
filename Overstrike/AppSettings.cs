using Newtonsoft.Json.Linq;
using System.IO;

namespace Overstrike {
	public class AppSettings {
		public string? CurrentProfile;

		public AppSettings() {
			CurrentProfile = null;
		}

		public AppSettings(string file) {
			JObject json = JObject.Parse(File.ReadAllText(file));

			CurrentProfile = (string)json["profile"];
		}

		public void Save(string file) {
			JObject j = new JObject();
			j["profile"] = CurrentProfile;
			File.WriteAllText(file, j.ToString());
		}
	}
}
