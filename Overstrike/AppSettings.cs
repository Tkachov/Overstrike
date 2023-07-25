// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

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
