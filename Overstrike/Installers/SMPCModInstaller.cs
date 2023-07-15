using DAT1;
using System.IO.Compression;
using System.IO;
using System;
using System.Globalization;

namespace Overstrike.Installers {
	internal class SMPCModInstaller: InstallerBase {
		public SMPCModInstaller(TOC toc, string gamePath) : base(toc, gamePath) {}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			var modsPath = Path.Combine(_gamePath, "asset_archive", "mods");
			var modPath = Path.Combine(modsPath, "mod" + index);

			var newArchiveIndex = _toc.AddNewArchive("mods\\mod" + index, TOC.ArchiveAddingImpl.SMPCTOOL); // TODO: switch to DEFAULT, it must be working fine

			var f = File.OpenWrite(modPath);
			var w = new BinaryWriter(f);

			using (ZipArchive zip = ReadModFile()) {
				foreach (ZipArchiveEntry entry in zip.Entries) {
					if (entry.FullName.StartsWith("ModFiles/", StringComparison.OrdinalIgnoreCase)) {
						ReplaceAsset(w, newArchiveIndex, entry);
					}
				}
			}

			w.Close();
			w.Dispose();
		}

		private void ReplaceAsset(BinaryWriter modArchiveFile, uint modArchiveIndex, ZipArchiveEntry asset) {
			string[] parts = asset.Name.Split("_"); // FullName.Substring(9)
			if (parts.Length != 2) return;

			int archiveIndex = int.Parse(parts[0]);
			ulong assetId = ulong.Parse(parts[1], NumberStyles.HexNumber);

			long archiveOffset = modArchiveFile.BaseStream.Position;
			using (var stream = asset.Open()) {
				stream.CopyTo(modArchiveFile.BaseStream);
			}
			long fileSize = modArchiveFile.BaseStream.Position - archiveOffset;

			AssetEntry[] assetEntries = _toc.FindAssetEntriesById(assetId);
			foreach (var assetEntry in assetEntries) {
				if (assetEntry.archive == archiveIndex) {
					_toc.UpdateAssetEntry(new AssetEntry() {
						index = assetEntry.index,
						id = assetEntry.id,
						archive = modArchiveIndex,
						offset = (uint)archiveOffset,
						size = (uint)fileSize
					});
					break;
				}
			}
		}		
	}
}
