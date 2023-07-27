// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Sections.TOC;
using System;
using System.Collections.Generic;
using System.IO;

namespace Overstrike.Installers {
	internal abstract class RCRAInstallerBase: InstallerBase {
		protected TOC_I29 _toc;

		public RCRAInstallerBase(TOC_I29 toc, string gamePath) : base(gamePath) {
			_toc = toc;
		}

		#region toc

		protected byte[] GetAssetBytes(string path) => _toc.ExtractAsset(_toc.FindAssetEntryByPath(path));
		protected byte[] GetAssetBytes(ulong assetId) => _toc.ExtractAsset(_toc.FindAssetEntryById(assetId));
		protected BinaryReader GetAssetReader(string path) => new BinaryReader(new MemoryStream(GetAssetBytes(path)));
		protected BinaryReader GetAssetReader(ulong assetId) => new BinaryReader(new MemoryStream(GetAssetBytes(assetId)));

		protected uint GetArchiveIndex(string filename) {
			uint index = 0;
			foreach (var entry in _toc.ArchivesSection.Entries) {
				if (entry.GetFilename() == filename) {
					return index;
				}

				++index;
			}

			return _toc.AddNewArchive(filename);
		}

		protected byte? GetSpan(int assetIndex) {
			return GetSpan(assetIndex, _toc);
		}

		protected byte? GetSpan(int assetIndex, TOC_I29 toc) {
			byte span = 0;
			foreach (var entry in toc.SpansSection.Entries) {
				if (entry.AssetIndex <= assetIndex && assetIndex < entry.AssetIndex + entry.Count) {
					return span;
				}

				++span;
			}

			return null;
		}

		protected void AddOrUpdateAssetEntry(ulong assetId, byte span, uint archiveIndex, uint offset, uint size, byte[] header) {
			AssetEntryBase[] assetEntries = _toc.FindAssetEntriesById(assetId);

			int assetIndex = -1;
			foreach (var assetEntry in assetEntries) {
				if (GetSpan(assetEntry.index) == span) {
					assetIndex = assetEntry.index;
					break;
				}
			}

			if (assetIndex == -1) {
				// TODO
				/*
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
				*/
				throw new NotImplementedException();
			}

			_toc.UpdateAssetEntry(new DAT1.TOC_I29.AssetEntry2() {
				index = assetIndex,
				id = assetId,
				archive = archiveIndex,
				offset = offset,
				size = size,
				header = header
			});
		}

		protected void SortAssets() {
			var ids = _toc.AssetIdsSection.Ids;
			var sizes = _toc.SizesSection.Entries;

			foreach (var span in _toc.SpansSection.Entries) {
				var start = span.AssetIndex;
				var end = span.AssetIndex + span.Count;

				var assets = new List<(ulong Id, SizeEntriesSection_I29.SizeEntry Size)>();
				for (int i = (int)start; i < end; ++i) {
					assets.Add((ids[i], sizes[i]));
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
				}
			}
		}

		#endregion
	}
}
