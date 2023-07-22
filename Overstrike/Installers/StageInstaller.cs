using DAT1;
using System.IO.Compression;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Overstrike.Installers {
	internal class StageInstaller: InstallerBase {
		public StageInstaller(TOC toc, string gamePath) : base(toc, gamePath) {}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			var modsPath = Path.Combine(_gamePath, "asset_archive", "mods");
			var modPath = Path.Combine(modsPath, "mod" + index);

			var newArchiveIndex = _toc.AddNewArchive("mods\\mod" + index, TOC.ArchiveAddingImpl.DEFAULT);

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
			byte span;
			ulong assetId;
			if (IsAssetFile(entry.FullName, out span, out assetId)) {
				long archiveOffset = archiveWriter.BaseStream.Position;
				using (var stream = entry.Open()) {
					stream.CopyTo(archiveWriter.BaseStream);
				}
				long fileSize = archiveWriter.BaseStream.Position - archiveOffset;

				AddOrUpdateAssetEntry(assetId, span, archiveIndexToWriteInto, (uint)archiveOffset, (uint)fileSize);
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
			if (Regex.IsMatch(filePath, "^[0-9A-Fa-f]{16}$")) {
				assetId = ulong.Parse(filePath, NumberStyles.HexNumber);
			} else {
				assetId = CRC64.Hash(filePath);
			}

			return true;
		}
	}
}
