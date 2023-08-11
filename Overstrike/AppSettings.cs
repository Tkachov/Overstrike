// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using System.IO;

namespace Overstrike {
	public class AppSettings {
		public string? CurrentProfile;
		public bool CacheModsLibrary;

		public AppSettings() {
			CurrentProfile = null;
			CacheModsLibrary = true;
		}

		public AppSettings(string file) {
			JObject json = JObject.Parse(File.ReadAllText(file));

			CurrentProfile = (string)json["profile"];
			CacheModsLibrary = (bool)json["cache_mods_library"];
		}

		public void Save(string file) {
			JObject j = new() {
				["profile"] = CurrentProfile,
				["cache_mods_library"] = CacheModsLibrary
			};
			File.WriteAllText(file, j.ToString());
		}
	}
}
