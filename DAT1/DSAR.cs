// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using GDeflateWrapper;
using K4os.Compression.LZ4;
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
			public byte compressionType;
			//public byte[7] unk3;
		}

		private class BlockHeaderComparer: IComparer<BlockHeader> {
			public int Compare(BlockHeader x, BlockHeader y) {
				if (x == null) {
					if (y == null) {
						return 0;
					} else {
						return -1;
					}
				}

				if (y == null) {
					return 1;
				}

				return x.realOffset.CompareTo(y.realOffset);
			}
		}

		public static byte[] ExtractAsset(FileStream archive, long offset, long size) {
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
			List<BlockHeader> blocks = new();
			while (archive.Position < blocks_header_end) {
				BlockHeader header = new();
				header.realOffset = r.ReadUInt32();
				r.ReadUInt32();
				header.compOffset = r.ReadUInt32();
				r.ReadUInt32();
				header.realSize = r.ReadUInt32();
				header.compSize = r.ReadUInt32();
				header.compressionType = r.ReadByte();
				r.ReadBytes(7);
				blocks.Add(header);
			}

			uint asset_offset = (uint)offset;
			uint asset_end = (uint)(asset_offset + size);

			uint bytes_ptr = 0;

			// binary search starting and ending blocks' indexes
			var comparer = new BlockHeaderComparer();

			var fakeBlock = new BlockHeader() { realOffset = asset_offset };
			int firstIndex = blocks.BinarySearch(fakeBlock, comparer);
			if (firstIndex < 0) firstIndex = ~firstIndex;
			if (firstIndex >= blocks.Count || blocks[firstIndex].realOffset > asset_offset) --firstIndex;

			fakeBlock.realOffset = asset_end;
			int lastIndex = blocks.BinarySearch(fakeBlock, comparer);
			if (lastIndex < 0) lastIndex = ~lastIndex;
			if (lastIndex >= blocks.Count || blocks[lastIndex].realOffset == asset_end) --lastIndex;

			bool started_reading = false;
			for (var blockIndex = firstIndex; blockIndex <= lastIndex; ++blockIndex) {
				var block = blocks[blockIndex];
				uint real_end = block.realOffset + block.realSize;
				bool is_first_block = (block.realOffset <= asset_offset && asset_offset < real_end);
				bool is_last_block = (block.realOffset < asset_end && asset_end <= real_end);

				if (is_first_block) started_reading = true;

				if (started_reading) {
					archive.Seek(block.compOffset, SeekOrigin.Begin);
					byte[] compressed = new byte[block.compSize];
					archive.Read(compressed, 0, compressed.Length);
					byte[] decompressed = Decompress(block, compressed);
					uint block_start = Math.Max(block.realOffset, asset_offset) - block.realOffset;
					uint block_end = Math.Min(asset_end, real_end) - block.realOffset;

					for (int i = (int)block_start; i < block_end; ++i)
						bytes[bytes_ptr++] = decompressed[i];
				}

				if (is_last_block) break;
			}

			archive.Close();
			return bytes;
		}

		private static byte[] Decompress(BlockHeader header, byte[] compressedData) {
			switch (header.compressionType) {
				case 2: return GDeflate.Decompress(compressedData, header.realSize);

				case 3:
					var output = new byte[header.realSize];
					LZ4Codec.Decode(compressedData, 0, compressedData.Length, output, 0, output.Length);
					return output;

				default:
					Utils.Assert(false, "DSAR.Decompress(): unknown compression type");
					return new byte[header.realSize];
			}
		}
	}
}
