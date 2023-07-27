// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Sections.TOC;
using System.Collections.Generic;
using System.IO;

namespace Overstrike.Installers {
	internal abstract class MSMRInstallerBase: InstallerBase {
		protected TOC_I20 _toc;

		public MSMRInstallerBase(TOC_I20 toc, string gamePath): base(gamePath) {
			_toc = toc;
		}

		#region toc

		protected byte[] GetAssetBytes(string path) => _toc.ExtractAsset(_toc.FindAssetEntryByPath(path));
		protected byte[] GetAssetBytes(ulong assetId) => _toc.ExtractAsset(_toc.FindAssetEntryById(assetId));
		protected BinaryReader GetAssetReader(string path) => new BinaryReader(new MemoryStream(GetAssetBytes(path)));
		protected BinaryReader GetAssetReader(ulong assetId) => new BinaryReader(new MemoryStream(GetAssetBytes(assetId)));

		protected uint GetArchiveIndex(string filename, TOC_I20.ArchiveAddingImpl mode) {
			uint index = 0;
			foreach (var entry in _toc.ArchivesSection.Entries) {
				if (entry.GetFilename() == filename) {
					return index;
				}

				++index;
			}

			return _toc.AddNewArchive(filename, mode);
		}

		protected byte? GetSpan(int assetIndex) {
			return GetSpan(assetIndex, _toc);
		}

		protected byte? GetSpan(int assetIndex, TOC_I20 toc) {
			byte span = 0;
			foreach (var entry in toc.SpansSection.Entries) {
				if (entry.AssetIndex <= assetIndex && assetIndex < entry.AssetIndex + entry.Count) {
					return span;
				}

				++span;
			}

			return null;
		}

		protected void AddOrUpdateAssetEntry(ulong assetId, byte span, uint archiveIndex, uint offset, uint size) {
			AssetEntryBase[] assetEntries = _toc.FindAssetEntriesById(assetId);

			int assetIndex = -1;
			foreach (var assetEntry in assetEntries) {
				if (GetSpan(assetEntry.index) == span) {
					assetIndex = assetEntry.index;
					break;
				}
			}

			if (assetIndex == -1) {
				var spansSection = _toc.SpansSection;
				var spanEntry = spansSection.Entries[span];
				assetIndex = (int)(spanEntry.AssetIndex + spanEntry.Count); // TODO: insert into right place

				++spanEntry.Count;
				for (int i = span + 1; i < spansSection.Entries.Count; ++i) {
					++spansSection.Entries[i].AssetIndex;
				}

				_toc.AssetIdsSection.Ids.Insert(assetIndex, assetId);
				_toc.SizesSection.Entries.Insert(assetIndex, new SizeEntriesSection_I16.SizeEntry() { Always1 = 1, Index = size, Value = (uint)assetIndex });
				_toc.OffsetsSection.Entries.Insert(assetIndex, new OffsetsSection.OffsetEntry() { ArchiveIndex = archiveIndex, Offset = offset });
			}

			_toc.UpdateAssetEntry(new DAT1.TOC_I20.AssetEntry() {
				index = assetIndex,
				id = assetId,
				archive = archiveIndex,
				offset = offset,
				size = size
			});
		}

		protected void SortAssets() {
			var ids = _toc.AssetIdsSection.Ids;
			var sizes = _toc.SizesSection.Entries;
			var offsets = _toc.OffsetsSection.Entries;

			foreach (var span in _toc.SpansSection.Entries) {
				var start = span.AssetIndex;
				var end = span.AssetIndex + span.Count;

				var assets = new List<(ulong Id, SizeEntriesSection_I16.SizeEntry Size, OffsetsSection.OffsetEntry Offset)>();
				for (int i = (int)start; i < end; ++i) {
					assets.Add((ids[i], sizes[i], offsets[i]));
				}

				var compare = (ulong a, ulong b) => {
					if (a == b) return 0;
					return (a < b ? -1 : 1);
				};
				assets.Sort((x, y) => compare(x.Id, y.Id));

				for (int i = (int)start; i < end; ++i) {
					int j = (int)(i - start);
					ids[i] = assets[j].Id;
					sizes[i] = assets[j].Size;
					offsets[i] = assets[j].Offset;
				}
			}

			for (var i = 0; i < ids.Count; ++i) {
				sizes[i].Index = (uint)i;
			}
		}

		#endregion
	}
}
