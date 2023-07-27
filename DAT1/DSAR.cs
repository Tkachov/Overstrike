// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;

namespace DAT1 {
	public abstract class DSAR {
		public static bool IsCompressed(FileStream fs) {
			var r = new BinaryReader(fs);
			uint magic = r.ReadUInt32();
			return (magic == 0x52415344);
		}

		private class BlockHeader {
			public uint realOffset;
			//public uint unk1;
			public uint compOffset;
			//public uint unk2;
			public uint realSize;
			public uint compSize;
			//public uint unk3;
			//public uint unk4;
		}

        public static byte[] ExtractAsset(FileStream archive, int offset, int size) {
            byte[] bytes = new byte[size];

            if (!IsCompressed(archive)) {
				archive.Seek(offset, SeekOrigin.Begin);
				archive.Read(bytes, 0, bytes.Length);
				archive.Close();
				return bytes;
			}			

            var r = new BinaryReader(archive);
			archive.Seek(12, SeekOrigin.Begin);
            uint blocks_header_end = r.ReadUInt32();

			archive.Seek(32, SeekOrigin.Begin);
			List<BlockHeader> blocks = new List<BlockHeader>();
			while (archive.Position < blocks_header_end) {
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

			uint asset_offset = (uint)offset;
			uint asset_end = (uint)(asset_offset + size);

			uint bytes_ptr = 0;

			// TODO: binary search starting block index and ending block index
			// (because this code anyways assumes blocks are sorted by real_offset asc)

			bool started_reading = false;
			foreach (var block in blocks) {
				uint real_end = block.realOffset + block.realSize;
				bool is_first_block = (block.realOffset <= asset_offset && asset_offset < real_end);
				bool is_last_block = (block.realOffset < asset_end && asset_end <= real_end);

				if (is_first_block) started_reading = true;

				if (started_reading) {
					archive.Seek(block.compOffset, SeekOrigin.Begin);
					byte[] compressed = new byte[block.compSize];
					archive.Read(compressed, 0, compressed.Length);
					byte[] decompressed = Decompress(compressed, block.realSize);
					uint block_start = Math.Max(block.realOffset, asset_offset) - block.realOffset;
					uint block_end = Math.Min(asset_end, real_end) - block.realOffset;

					for (int i=(int)block_start; i<block_end; ++i)
						bytes[bytes_ptr++] = decompressed[i];
				}

				if (is_last_block) break;
			}

			archive.Close();
            return bytes;
        }

		public static byte[] Decompress(byte[] comp_data, uint real_size) {
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
				while (direct >= 270 && (direct-15) % 255 == 0) {
					byte v = comp_data[comp_i++];
					direct += v;
					if (v == 0) break;
				}

				for (int i=0; i<direct; ++i) {
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
				if (reverse == 19) {
					reverse += comp_data[comp_i++];
					while (reverse >= 274 && (reverse-19) % 255 == 0) {
						byte v = comp_data[comp_i++];
						reverse += v;
						if (v == 0) break;
					}
				}

				for ( int i=0; i<reverse; ++i) {
					try {
						real_data[real_i + i] = real_data[real_i - reverse_offset + i];
					} catch ( Exception e ) { }
					
				}
                real_i += reverse;
            }

			return real_data;
		}
	}
}
