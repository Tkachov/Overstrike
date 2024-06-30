// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO.Compression;
using System.IO;
using System;
using System.Collections.Generic;
using Overstrike.Games;
using Overstrike.Data;
using Overstrike.Installers;

namespace Overstrike.Detectors {
	internal class ModularModDetector: DetectorBase {
		public ModularModDetector() : base() {}

		public override string[] GetExtensions() {
			return new string[] {"modular"};
		}

		public override void Detect(Stream file, string path, List<ModEntry> mods, List<string> warnings) {
			try {
				using var zip = new ZipArchive(file);
				var info = ModularInstaller.GetInfo(zip);
				if (info == null) return;

				var shortPath = GetShortPath(path);
				var name = Path.GetFileName(shortPath);
				var type = ModEntry.ModType.UNKNOWN;
				if (info != null) {
					var version = -1;
					if (info.ContainsKey("format_version"))
						version = (int)info["format_version"];
					if (version < 1) return;

					if (version > 1) {
						warnings.Add($"'{shortPath}': file seems to use newer version of the format. Check if there's an update for Overstrike.");
						return;
					}

					var n = (string)info["name"];
					var a = (string)info["author"];
					if (n != null && n.Trim() != "") {
						name = n;
						if (a != null && a.Trim() != "") {
							name += " by " + a;
						}
					}

					var g = (string)info["game"];
					if (g != null && g.Trim() != "") {
						if (g == GameMSMR.ID) type = ModEntry.ModType.MODULAR_MSMR;
						else if (g == GameMM.ID) type = ModEntry.ModType.MODULAR_MM;
						else if (g == GameRCRA.ID) type = ModEntry.ModType.MODULAR_RCRA;
						else if (g == GameI30.ID) type = ModEntry.ModType.MODULAR_I30;
						else if (g == GameI33.ID) type = ModEntry.ModType.MODULAR_I33;
					}
				}

				if (type != ModEntry.ModType.UNKNOWN) {
					mods.Add(new ModEntry(name, path, type));
				}
			} catch (Exception) {}
		}
	}
}
