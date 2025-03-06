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
	internal class StageModDetector: DetectorBase {
		public StageModDetector() : base() {}

		public override string[] GetExtensions() {
			return new string[] {"stage"};
		}

		public override void Detect(Stream file, string path, List<ModEntry> mods, List<string> warnings) {
			try {
				bool hasFiles = false;
				JObject info = null;

				using (ZipArchive zip = new ZipArchive(file)) {
					foreach (ZipArchiveEntry entry in zip.Entries) {
						if (entry.FullName.Equals("info.json", StringComparison.OrdinalIgnoreCase)) {
							using (var stream = entry.Open()) {
								using (StreamReader reader = new StreamReader(stream)) {
									var str = reader.ReadToEnd();
									info = JObject.Parse(str);
								}
							}
						} else {
							var root = GetRootFolder(entry.FullName);
							if (root != null) {
								int span;
								var isNumeric = int.TryParse(root, out span);
								if (isNumeric && span >= 0 && span <= 255) {
									hasFiles = true;
								}
							}
						}
					}
				}

				if (!hasFiles || info == null) return;

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
						if (g == GameMSMR.ID) type = ModEntry.ModType.STAGE_MSMR;
						else if (g == GameMM.ID) type = ModEntry.ModType.STAGE_MM;
						else if (g == GameRCRA.ID) type = ModEntry.ModType.STAGE_RCRA;
						else if (g == GameI30.ID) type = ModEntry.ModType.STAGE_I30;
						else if (g == GameI33.ID) type = ModEntry.ModType.STAGE_I33;
						else if (g == GameMSM2.ID) type = ModEntry.ModType.STAGE_MSM2;
					}

					if (info.ContainsKey("format_version")) {
						var version = (int)info["format_version"];
						if (version == 2) {
							if (type == ModEntry.ModType.STAGE_RCRA) type = ModEntry.ModType.STAGE_RCRA_V2;
							else if (type == ModEntry.ModType.STAGE_MSM2) type = ModEntry.ModType.STAGE_MSM2_V2;
						}
					}
				}

				if (type != ModEntry.ModType.UNKNOWN) {
					mods.Add(new ModEntry(name, path, type));
				}
			} catch (Exception) { }
		}

		private string GetRootFolder(string path) {
			if (path == null) return null;

			string root = "";
			foreach (var c in path) {
				if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar) break;
				root += c;
			}
			return root;
		}
	}
}
