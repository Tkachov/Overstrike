// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using System.IO.Compression;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Overstrike.Installers {
	internal class RCRAStageInstaller: RCRAInstallerBase {
		public RCRAStageInstaller(TOC_I29 toc, string gamePath) : base(toc, gamePath) {}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			var modsPath = Path.Combine(_gamePath, "d", "mods");
			var modPath = Path.Combine(modsPath, "mod" + index);
			var relativeModPath = "d\\mods\\mod" + index;

			var newArchiveIndex = _toc.AddNewArchive(relativeModPath);

			using (var f = new FileStream(modPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
				using (var w = new BinaryWriter(f)) {
					using (ZipArchive zip = ReadModFile()) {
						foreach (ZipArchiveEntry entry in zip.Entries) {
							HandleFileEntry(entry, newArchiveIndex, w);
						}
					}
				}
			}

			SortAssets();
		}

		private void HandleFileEntry(ZipArchiveEntry entry, uint archiveIndexToWriteInto, BinaryWriter archiveWriter) {
			if (entry.Name == "" && entry.FullName.EndsWith("/")) return; // directory

			byte span;
			ulong assetId;
			if (IsAssetFile(entry.FullName, out span, out assetId)) {
				long archiveOffset = archiveWriter.BaseStream.Position;
				byte[] header = new byte[36];
				using (var stream = entry.Open()) {
					stream.Read(header, 0, 36);
					stream.CopyTo(archiveWriter.BaseStream);
				}
				long fileSize = archiveWriter.BaseStream.Position - archiveOffset;

				AddOrUpdateAssetEntry(assetId, span, archiveIndexToWriteInto, (uint)archiveOffset, (uint)fileSize, header);
			}
		}

		private bool IsAssetFile(string path, out byte span, out ulong assetId) {
			span = 0;
			assetId = 0;

			if (path == null) return false;

			var index = -1;
			for (var i = 0; i < path.Length; ++i) {
				if (path[i] == Path.DirectorySeparatorChar || path[i] == Path.AltDirectorySeparatorChar) {
					index = i;
					break;
				}
			}
			if (index < 0) return false;

			var spanDir = path.Substring(0, index);
			int spanNum;
			var isNumeric = int.TryParse(spanDir, out spanNum);
			if (isNumeric && spanNum >= 0 && spanNum <= 255) {
				span = (byte)spanNum;
			} else {
				return false;
			}

			var filePath = path.Substring(index + 1);
			if (filePath == "") return false;
			if (Regex.IsMatch(filePath, "^[0-9A-Fa-f]{16}$")) {
				assetId = ulong.Parse(filePath, NumberStyles.HexNumber);
			} else {
				assetId = CRC64.Hash(filePath);
			}

			return true;
		}
	}
}
