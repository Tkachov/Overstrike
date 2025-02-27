// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO.Compression;
using System.IO;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Overstrike.Games;
using Overstrike.Data;

namespace Overstrike.Detectors {
	internal class ScriptModDetector: DetectorBase {
		public ScriptModDetector() : base() {}

		public override string[] GetExtensions() {
			return new string[] {"script"};
		}

		public override void Detect(Stream file, string path, List<ModEntry> mods, List<string> warnings) {
			try {
				JObject info = null;

				using (var zip = new ZipArchive(file)) {
					foreach (ZipArchiveEntry entry in zip.Entries) {
						if (entry.FullName.Equals("info.json", StringComparison.OrdinalIgnoreCase)) {
							using var stream = entry.Open();
							using var reader = new StreamReader(stream);
							var str = reader.ReadToEnd();
							info = JObject.Parse(str);
						}
					}
				}

				if (info == null) return;

				var shortPath = GetShortPath(path);
				var name = Path.GetFileName(shortPath);
				var type = ModEntry.ModType.UNKNOWN;
				if (info != null) {
					string n = (string)info["name"];
					string a = (string)info["author"];
					if (n != null && n.Trim() != "") {
						name = n;
						if (a != null && a.Trim() != "") {
							name += " by " + a;
						}
					}

					string g = (string)info["game"];
					if (g != null && g.Trim() != "") {
						if (g == GameMSM2.ID) type = ModEntry.ModType.SCRIPT_MSM2;
					}
				}

				if (type != ModEntry.ModType.UNKNOWN) {
					mods.Add(new ModEntry(name, path, type));
				}
			} catch (Exception) {}
		}
	}
}
