// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.TOC;
using System;
using System.IO;
using System.Text;

namespace DAT1 {
	// RCRA implementation
	// TODO: make a common interface instead of copy-pasting class entirely


	public class TOC_I29: TOCBase {
		private const uint MAGIC = 0x34E89035;

		public AssetHeadersSection AssetHeadersSection => Dat1.Section<AssetHeadersSection>(AssetHeadersSection.TAG);
		public SizeEntriesSection_I29 SizesSection => Dat1.Section<SizeEntriesSection_I29>(SizeEntriesSection_I29.TAG);
		public ArchivesMapSection_I29 ArchivesSection => Dat1.Section<ArchivesMapSection_I29>(ArchivesMapSection_I29.TAG);
		
		public class AssetEntry2: AssetEntryBase {
			public uint offset;
			public uint size;
			public byte[] header;
		}

		public override bool Load(string filename) {
			try {
				var f = File.OpenRead(filename);
				var r = new BinaryReader(f);
				uint magic = r.ReadUInt32();
				if (magic != MAGIC) {
					return false;
				}

				uint uncompressedLength = r.ReadUInt32();

				int length = (int)(f.Length - 8);
				byte[] bytes = r.ReadBytes(length);
				r.Close();
				r.Dispose();
				f.Close();
				f.Dispose();

				Dat1 = new DAT1(new BinaryReader(new MemoryStream(bytes)));
				AssetArchivePath = Path.GetDirectoryName(filename);
				return true;
			} catch (Exception e) {
				return false;
			}
		}

		public override bool Save(string filename) {
			if (!IsLoaded)
				return false;

			byte[] uncompressed = Dat1.Save();

			using (var f = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None)) {
				using (var w = new BinaryWriter(f)) {
					w.Write((uint)MAGIC);
					w.Write((uint)uncompressed.Length);
					w.Write(uncompressed);
				}
			}

			return true;
		}

		public override AssetEntryBase GetAssetEntryByIndex(int index) {
			try {
				AssetEntry2 result = new() {
					index = index,
					id = AssetIdsSection.Ids[index],
					archive = SizesSection.Entries[index].ArchiveIndex,
					offset = SizesSection.Entries[index].Offset,
					size = SizesSection.Entries[index].Size
				};

				var header_offset = SizesSection.Entries[index].HeaderOffset;
				if (header_offset != -1) {
					var header_index = header_offset / 36;
					result.header = AssetHeadersSection.Headers[header_index];
				} else {
					result.header = null;
				}

				return result;
			} catch (Exception) { }

			return null;
		}

		public void UpdateAssetEntry(AssetEntry2 entry) {
			int index = entry.index;
			AssetIdsSection.Ids[index] = entry.id;
			SizesSection.Entries[index].ArchiveIndex = entry.archive;
			SizesSection.Entries[index].Offset = entry.offset;
			SizesSection.Entries[index].Size = entry.size;
			if (entry.header != null) {
				var header_index = SizesSection.Entries[index].HeaderOffset / 36;
				AssetHeadersSection.Headers[header_index] = entry.header;
			}
		}

		public override byte[] ExtractAsset(AssetEntryBase AssetBase) {
			if (!IsLoaded)
				return null;

			AssetEntry2 Asset = (AssetEntry2)AssetBase;
			if (Asset == null)
				return null;

			byte[] b = DSAR.ExtractAsset(OpenArchive(Asset.archive), (int)Asset.offset, (int)Asset.size);

			if (Asset.header == null) {
				return b;
			}

			int real_size = (int)(Asset.size + 36);
			byte[] bytes = new byte[real_size];
			Asset.header.CopyTo(bytes, 0);
			b.CopyTo(bytes, 0);
			return bytes;
		}

		protected override FileStream OpenArchive(uint index) {
			return OpenArchive(ArchivesSection.Entries[(int)index].GetFilename());
		}

		public override uint AddNewArchive(string filename, ArchiveAddingImpl ignored = ArchiveAddingImpl.DEFAULT) {
			int index = ArchivesSection.Entries.Count;

			byte[] bytes = new byte[40];
			for (int i = 0; i < 40; ++i) bytes[i] = 0;
			byte[] fn = Encoding.ASCII.GetBytes(filename);
			fn.CopyTo(bytes, 0);
			bytes[fn.Length] = 0;
			
			ArchivesSection.Entries.Add(new ArchivesMapSection_I29.ArchiveFileEntry() {
				Filename = bytes,
				A = 2678794514496,
				B = 2678794514496,
				C = 3844228203,
				D = 32763,
				E = 0
			});

			return (uint)index;
		}
	}
}
