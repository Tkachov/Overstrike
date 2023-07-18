using DAT1;
using DAT1.Files;
using System.IO;
using System.IO.Compression;

namespace Overstrike.Installers {
	internal abstract class SuitInstallerBase: InstallerBase {
		protected SuitInstallerBase(TOC toc, string gamePath) : base(toc, gamePath) {}

		#region .suit

		protected string ReadId(ZipArchiveEntry idTxt) {
			using (var stream = idTxt.Open()) {
				using (StreamReader reader = new StreamReader(stream)) {
					var str = reader.ReadToEnd();
					var lines = str.Split("\n");
					if (lines.Length > 0) {
						return lines[0].Trim();
					}
				}
			}

			return null;
		}

		#endregion
		#region toc

		protected uint GetArchiveIndex(string filename) => GetArchiveIndex(filename, TOC.ArchiveAddingImpl.SUITTOOL);

		protected void AddOrUpdateAssetEntry(ulong assetId, byte span, uint archiveIndex, uint offset, uint size) {
			AssetEntry[] assetEntries = _toc.FindAssetEntriesById(assetId);
			
			int assetIndex = -1;
			foreach (var assetEntry in assetEntries) {
				if (GetSpan(assetEntry.index) == span) {
					assetIndex = assetEntry.index;
					break;
				}
			}

			if (assetIndex == -1) {
				var spansSection = _toc.Dat1.SpansSection;
				var spanEntry = spansSection.Entries[span];
				assetIndex = (int)(spanEntry.AssetIndex + spanEntry.Count); // TODO: insert into right place

				++spanEntry.Count;
				for (int i = span + 1; i < spansSection.Entries.Count; ++i) {
					++spansSection.Entries[i].AssetIndex;
				}

				_toc.Dat1.AssetIdsSection.Ids.Insert(assetIndex, assetId);
				_toc.Dat1.SizesSection.Entries.Insert(assetIndex, new DAT1.Sections.TOC.SizeEntriesSection.SizeEntry() { Always1 = 1, Index = size, Value = (uint)assetIndex });
				_toc.Dat1.OffsetsSection.Entries.Insert(assetIndex, new DAT1.Sections.TOC.OffsetsSection.OffsetEntry() { ArchiveIndex = archiveIndex, Offset = offset });
			}

			_toc.UpdateAssetEntry(new AssetEntry() {
				index = assetIndex,
				id = assetId,
				archive = archiveIndex,
				offset = offset,
				size = size
			});
		}

		protected void WriteArchive(string archivePath, uint archiveIndex, ulong assetId, byte span, byte[] bytes) {
			File.WriteAllBytes(archivePath, bytes);
			AddOrUpdateAssetEntry(assetId, span, archiveIndex, /*offset=*/0, (uint)bytes.Length);
		}

		protected void WriteArchive(string suitsPath, string archiveName, ulong assetId, byte[] bytes) {
			WriteArchive(suitsPath, archiveName, assetId, 0, bytes);
		}

		protected void WriteArchive(string suitsPath, string archiveName, ulong assetId, byte span, byte[] bytes) {
			var archivePath = Path.Combine(suitsPath, archiveName);
			var archiveIndex = GetArchiveIndex("Suits\\" + archiveName);
			WriteArchive(archivePath, archiveIndex, assetId, span, bytes);
		}

		#endregion
		#region .config

		protected void AddConfigReference(Config config, string path) {
			ulong aid = CRC64.Hash(path);
			foreach (var entry in config.ReferencesSection.Entries) {
				if (entry.AssetId == aid) {
					return;
				}
			}

			uint ext = 0;
			if (path.EndsWith(".config")) ext = 0xA9F149C4;
			else if (path.EndsWith(".texture")) ext = 0x95A3A227; // TODO: just calculate crc32 of extension lol

			config.ReferencesSection.Entries.Add(new DAT1.Sections.Generic.ReferencesSection.ReferenceEntry() {
				AssetId = aid,
				AssetPathStringOffset = config.AddString(path),
				ExtensionHash = ext
			});
		}

		#endregion
	}
}
