// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace GDeflateWrapper {
	// in compliance with https://github.com/microsoft/DirectStorage/
	public static class GDeflate {
		public const byte kGDeflateId = 4;
		public const long kDefaultTileSize = 64 * 1024;

		public static byte[] Decompress(byte[] compressed, uint outputSize) {
			var r = new BinaryReader(new MemoryStream(compressed));

			// read TileStream

			byte id = r.ReadByte();
			byte magic = r.ReadByte();

			Assert(id == kGDeflateId, "GDeflate.Decompress(): bad TileStream.id");
			Assert(id == (magic ^ 0xFF), "GDeflate.Decompress(): bad TileStream.magic");

			var numTiles = r.ReadUInt16();
			var ignored = r.ReadUInt32(); // tileSizeIdx : 2, lastTileSize : 18, reserved1 : 12

			List<uint> tileOffsets = new();
			for (var i = 0; i < numTiles; ++i) tileOffsets.Add(r.ReadUInt32());

			// decompress tile by tile

			var output = new byte[outputSize];
			var tile = new byte[kDefaultTileSize];

			for (var tileIndex = 0; tileIndex < numTiles; ++tileIndex) {
				long tileOffset = (tileIndex > 0 ? tileOffsets[tileIndex] : 0);
				long sz = (tileIndex < numTiles - 1 ? tileOffsets[tileIndex + 1] - tileOffset : tileOffsets[0]);

				var compressedTile = r.ReadBytes((int)sz);
				DecompressTile(compressedTile, tile);

				var outputOffset = tileIndex * kDefaultTileSize;
				var end = Math.Min(kDefaultTileSize, outputSize - outputOffset);
				for (var i = 0; i < end; ++i) {
					output[outputOffset + i] = tile[i];
				}
			}

			return output;
		}

		public unsafe static uint DecompressTile(byte[] source, byte[] target) {
			void* decompressor = Imports.libdeflate_alloc_gdeflate_decompressor();

			fixed (byte* data = &source[0]) {
				Imports.libdeflate_gdeflate_in_page libdeflate_gdeflate_in_page = default;
				libdeflate_gdeflate_in_page.data = data;
				libdeflate_gdeflate_in_page.nbytes = (uint)source.Length;

				fixed (byte* output = &target[0]) {
					uint actualRead = 0;
					var result = Imports.libdeflate_gdeflate_decompress(decompressor, &libdeflate_gdeflate_in_page, 1, output, 65536, &actualRead);

					if (result != Imports.libdeflate_result.LIBDEFLATE_SUCCESS) {
						throw new Exception($"libdeflate error: {result}");
					}

					return actualRead;
				}
			}
		}

		[Conditional("DEBUG")]
		public static void Assert(bool condition, string message) {
			if (!condition) {
				throw new Exception(message);
			}
		}
	}

	// built from https://github.com/NVIDIA/libdeflate/
	internal static class Imports {
		internal const string DllName = "libdeflate.dll";

		internal enum libdeflate_result {
			LIBDEFLATE_SUCCESS = 0,
			LIBDEFLATE_BAD_DATA = 1,
			LIBDEFLATE_SHORT_OUTPUT = 2,
			LIBDEFLATE_INSUFFICIENT_SPACE = 3,
		}

		internal struct libdeflate_gdeflate_in_page {
			public unsafe void* data;
			public uint nbytes;
		}

		[DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
		internal unsafe static extern void* libdeflate_alloc_gdeflate_decompressor();

		[DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
		internal unsafe static extern void libdeflate_free_gdeflate_decompressor(void* decomp);

		[DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
		internal unsafe static extern libdeflate_result libdeflate_gdeflate_decompress(void* decomp, libdeflate_gdeflate_in_page* in_pages, uint in_npages, void* output, uint out_nbytes_avail, uint* actual_out_nbytes_ret);
	}
}
