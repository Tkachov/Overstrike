using DAT1;
using System.IO.Compression;
using System.IO;
using System;
using System.Globalization;
using SharpCompress.Common;
using System.Xml.Linq;

namespace Overstrike.Installers {
	internal class SMPCModInstaller: InstallerBase {
		private TOC _unchangedToc;

		public SMPCModInstaller(TOC toc, TOC unchangedToc, string gamePath) : base(toc, gamePath) {
			_unchangedToc = unchangedToc;
		}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			var modsPath = Path.Combine(_gamePath, "asset_archive", "mods");
			var modPath = Path.Combine(modsPath, "mod" + index);

			var newArchiveIndex = _toc.AddNewArchive("mods\\mod" + index, TOC.ArchiveAddingImpl.SMPCTOOL); // TODO: switch to DEFAULT, it must be working fine

			using (var f = new FileStream(modPath, FileMode.Truncate, FileAccess.Write, FileShare.None)) {
				using (var w = new BinaryWriter(f)) {
					using (ZipArchive zip = ReadModFile()) {
						foreach (ZipArchiveEntry entry in zip.Entries) {
							if (entry.FullName.StartsWith("ModFiles/", StringComparison.OrdinalIgnoreCase)) {
								ReplaceAsset(w, newArchiveIndex, entry);
							}
						}
					}
				}
			}
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

			AssetEntry originalEntry = null;
			AssetEntry[] originalAssetEntries = _unchangedToc.FindAssetEntriesById(assetId);
			foreach (var assetEntry in originalAssetEntries) {
				if (assetEntry.archive == archiveIndex) { // TODO: fix this to go through ORIGINAL UNCHANGED TOC
					originalEntry = assetEntry;
					break;
				}
			}

			if (originalEntry != null) {
				var spanIndex = GetSpan(originalEntry.index, _unchangedToc);

				// now find the asset in modified toc that has the same id and is in the same span (could be in different archive already)
				var span = _toc.Dat1.SpansSection.Entries[(int)spanIndex];
				AssetEntry[] assetEntries = _toc.FindAssetEntriesById(assetId);
				foreach (var entry in assetEntries) {
					if (span.AssetIndex <= entry.index && entry.index < span.AssetIndex + span.Count) {
						_toc.UpdateAssetEntry(new AssetEntry() {
							index = entry.index,
							id = entry.id,
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
}
