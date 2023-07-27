// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static DAT1.Sections.TOC.ArchivesMapSection;

namespace DAT1 {
	// RCRA implementation
	// TODO: make a common interface instead of copy-pasting class entirely

	public class AssetEntry2
	{
		public int index;
		public UInt64 id;
		public uint archive;
		public uint offset;
		public uint size;
		public byte[] header;
    }

	public class TOC2
	{
		public DAT1 Dat1 = null;
		private string AssetArchivePath = null;
		public bool IsLoaded => Dat1 != null;

		private const uint TOC_MAGIC = 0x34E89035;

		public bool Load(string filename) {
			try {
				var f = File.OpenRead(filename);
				var r = new BinaryReader(f);
				uint magic = r.ReadUInt32();
				if (magic != TOC_MAGIC) {
					return false;
				}

				uint uncompressedLength = r.ReadUInt32();

				int length = (int)(f.Length - 8);
				byte[] bytes = r.ReadBytes(length);
				r.Close();
				r.Dispose();
				f.Close();
				f.Dispose();

				Dat1 = new DAT1(new BinaryReader(new MemoryStream(bytes)), FormatVersion.RCRA);
				AssetArchivePath = Path.GetDirectoryName(filename);
				return true;
			} catch (Exception e) {
				return false;
			}
		}

		public bool Save(String filename) {
			if (!IsLoaded)
				return false;

			byte[] uncompressed = Dat1.Save();

			using (var f = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None)) {
				using (var w = new BinaryWriter(f)) {
					w.Write((uint)TOC_MAGIC);
					w.Write((uint)uncompressed.Length);
					w.Write(uncompressed);
				}
			}

			return true;
		}

		public AssetEntry2[] FindAssetEntriesByPath(string assetPath, bool stopOnFirst = false)
		{
			return FindAssetEntriesById(CRC64.Hash(assetPath), stopOnFirst);
		}

        public AssetEntry2[] FindAssetEntriesById(UInt64 assetId, bool stopOnFirst = false)
        {
			List<AssetEntry2> results = new List<AssetEntry2>();

			if (IsLoaded) {
				var ids = Dat1.AssetIdsSection.Ids;
				for (int i = 0; i < ids.Count; ++i) { // linear search =\
					if (ids[i] == assetId) {
						results.Add(GetAssetEntryByIndex(i));
						if (stopOnFirst) break;
					}
				}
			}

            return results.ToArray();
        }

        public AssetEntry2 FindAssetEntryByPath(string assetPath)
        {
            return FindAssetEntriesByPath(assetPath, true)[0];
        }

        public AssetEntry2 FindAssetEntryById(UInt64 assetId)
        {
            return FindAssetEntriesById(assetId, true)[0];
        }

        public AssetEntry2 GetAssetEntryByIndex(int index)
		{
			try {
				AssetEntry2 result = new AssetEntry2();
				result.index = index;
				result.id = Dat1.AssetIdsSection.Ids[index];
				result.archive = Dat1.SizesSection2.Entries[index].ArchiveIndex;
                result.offset = Dat1.SizesSection2.Entries[index].Offset;
				result.size = Dat1.SizesSection2.Entries[index].Size;

				var header_offset = Dat1.SizesSection2.Entries[index].HeaderOffset;
				if (header_offset != -1) {
					var header_index = header_offset / 36;
					result.header = Dat1.AssetHeadersSection.Headers[header_index];
				} else {
					result.header = null;
				}

				return result;
            } catch (Exception) {}

            return null;
		}

		public void UpdateAssetEntry(AssetEntry2 entry) {
			int index = entry.index;
			Dat1.AssetIdsSection.Ids[index] = entry.id;
			Dat1.SizesSection2.Entries[index].ArchiveIndex = entry.archive;
			Dat1.SizesSection2.Entries[index].Offset = entry.offset;
			Dat1.SizesSection2.Entries[index].Size = entry.size;
			if (entry.header != null) {
				var header_index = Dat1.SizesSection2.Entries[index].HeaderOffset / 36;
				Dat1.AssetHeadersSection.Headers[header_index] = entry.header;
			}
		}

		public byte[] ExtractAsset(int index)
		{
			return ExtractAsset(GetAssetEntryByIndex(index));
		}

		class BlockHeader
		{
			public uint realOffset;
			//public uint unk1;
			public uint compOffset;
			//public uint unk2;
			public uint realSize;
			public uint compSize;
            //public uint unk3;
            //public uint unk4;
        }

        public byte[] ExtractAsset(AssetEntry2 Asset)
		{
			if (!IsLoaded)
				return null;

			if (Asset == null)
				return null;

			ArchivePair p = GetArchive(Asset.archive);

			int archived_size = (int)Asset.size;
			int real_size = archived_size;
			if (Asset.header != null) {
				real_size += 36;
			}

            byte[] bytes = new byte[real_size];

			if (Asset.header != null) {
				Asset.header.CopyTo(bytes, 0);
			}

            if (!p.compressed)
			{
				p.f.Seek(Asset.offset, SeekOrigin.Begin);
				p.f.Read(bytes, real_size - archived_size, archived_size);
				p.f.Close();
				return bytes;
			}

            var r = new BinaryReader(p.f);
			p.f.Seek(12, SeekOrigin.Begin);
			//r.BaseStream.Position = 12;
            uint blocks_header_end = r.ReadUInt32();

			p.f.Seek(32, SeekOrigin.Begin);
			// r.ReadBytes(32 - 12 - 4);
			//r.BaseStream.Position = 32;
			List<BlockHeader> blocks = new List<BlockHeader>();
			while (p.f.Position < blocks_header_end)
			{
				BlockHeader header = new BlockHeader();
				header.realOffset = r.ReadUInt32();
				r.ReadUInt32();
				header.compOffset = r.ReadUInt32();
                r.ReadUInt32();
                header.realSize = r.ReadUInt32();
                header.compSize = r.ReadUInt32();
                r.ReadUInt32();
                r.ReadUInt32();
                blocks.Add(header);
			}

			uint asset_offset = Asset.offset;
			uint asset_end = asset_offset + Asset.size;

			int bytes_ptr = real_size - archived_size;

			// TODO: binary search starting block index and ending block index
			// (because this code anyways assumes blocks are sorted by real_offset asc)

			bool started_reading = false;
			foreach (var block in blocks)
			{
				uint real_end = block.realOffset + block.realSize;
				bool is_first_block = (block.realOffset <= asset_offset && asset_offset < real_end);
				bool is_last_block = (block.realOffset < asset_end && asset_end <= real_end);

				if (is_first_block) started_reading = true;

				if (started_reading)
				{
					p.f.Seek(block.compOffset, SeekOrigin.Begin);
					byte[] compressed = new byte[block.compSize];
					p.f.Read(compressed, 0, compressed.Length);
					byte[] decompressed = Decompress(compressed, block.realSize);
					uint block_start = Math.Max(block.realOffset, asset_offset) - block.realOffset;
					uint block_end = Math.Min(asset_end, real_end) - block.realOffset;

					for (int i=(int)block_start; i<block_end; ++i)
						bytes[bytes_ptr++] = decompressed[i];
				}

				if (is_last_block) break;
			}

			p.f.Close();

            return bytes;
        }

		byte[] Decompress(byte[] comp_data, uint real_size)
		{
			int comp_size = comp_data.Length;
			byte[] real_data = new byte[real_size];
			int real_i = 0;
			int comp_i = 0;

			while (real_i <= real_size && comp_i < comp_size) {
				// direct
				byte a = comp_data[comp_i++];
				byte b = 0;
				
				if ((a&240) == 240)
					b = comp_data[comp_i++];

				int direct = (a >> 4) + b;
				while (direct >= 270 && (direct-15) % 255 == 0)
				{
					byte v = comp_data[comp_i++];
					direct += v;
					if (v == 0) break;
				}

				for (int i=0; i<direct; ++i)
				{
					if (real_i + i >= real_size || comp_i + i >= comp_size) break;
					real_data[real_i + i] = comp_data[comp_i + i];
				}
				real_i += direct;
				comp_i += direct;

				int reverse = (a & 15) + 4;
				if (!(real_i <= real_size && comp_i < comp_size)) break;

                // reverse

				a = comp_data[comp_i++];
				b = comp_data[comp_i++];

				int reverse_offset = a + (b << 8);
				if (reverse == 19)
				{
					reverse += comp_data[comp_i++];
					while (reverse >= 274 && (reverse-19) % 255 == 0)
					{
						byte v = comp_data[comp_i++];
						reverse += v;
						if (v == 0) break;
					}
				}

				for ( int i=0; i<reverse; ++i)
				{
					try
					{
						real_data[real_i + i] = real_data[real_i - reverse_offset + i];
					} catch ( Exception e ) { }
					
				}
                real_i += reverse;
            }

			return real_data;
		}

        struct ArchivePair
		{
			public FileStream f;
			public bool compressed;
		}

		ArchivePair GetArchive(uint index)
		{
            Sections.TOC.ArchivesMapSection2.ArchiveFileEntry a = Dat1.ArchivesSection2.Entries[(int)index];
            string fn = a.GetFilename();
			string full = Path.Combine(AssetArchivePath, fn);
			FileStream fs = System.IO.File.OpenRead(full);

            var r = new BinaryReader(fs);
            uint magic = r.ReadUInt32();
			bool compressed = (magic == 0x52415344);

			ArchivePair p;
			p.f = fs;
			p.compressed = compressed;
			return p;
        }

		public uint AddNewArchive(string filename) {
			return AddNewArchive_Default(filename);
		}

		private uint AddNewArchive_Default(string filename) {
			int index = Dat1.ArchivesSection2.Entries.Count;

			byte[] bytes = new byte[40];
			for (int i = 0; i < 40; ++i) bytes[i] = 0;
			byte[] fn = Encoding.ASCII.GetBytes(filename);
			fn.CopyTo(bytes, 0);
			bytes[fn.Length] = 0;
			
			Dat1.ArchivesSection2.Entries.Add(new Sections.TOC.ArchivesMapSection2.ArchiveFileEntry() {
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
