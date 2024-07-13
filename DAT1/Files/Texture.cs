// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Texture;
using System.IO;

namespace DAT1.Files {
	public abstract class TextureBase: DAT1 {
		protected uint magic, dat1_size;
		protected byte[] unk;
		protected byte[] raw;

		public TextureBase() : base() {}

		public TextureHeaderSection HeaderSection => Section<TextureHeaderSection>(TextureHeaderSection.TAG);

		public byte[] GetDDS() {
			MemoryStream result = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(result);
			
			WriteDDSHeader(bw, HeaderSection.sd_width, HeaderSection.sd_height, HeaderSection.sd_mipmaps);
			bw.Write(raw);

			bw.Flush();
			result.Flush();
			return result.ToArray();
		}

		public byte[] GetBigDDS(byte[] hd_part) {
			MemoryStream result = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(result);

			WriteDDSHeader(bw, HeaderSection.hd_width, HeaderSection.hd_height, (uint)(HeaderSection.hd_mipmaps + HeaderSection.sd_mipmaps));
			bw.Write(hd_part);
			bw.Write(raw);

			bw.Flush();
			result.Flush();
			return result.ToArray();
		}

		private void WriteDDSHeader(BinaryWriter bw, uint width, uint height, uint mipmaps) {
			bw.Write(new byte[] { 0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 0x07, 0x10, 0x0A, 0x00 });
			bw.Write(height);
			bw.Write(width);

			// pitch, depth, mipmaps
			uint pitch = (width * 32 + 7) / 8;
			bw.Write(pitch);
			bw.Write((uint)0);
			bw.Write(mipmaps);

			// reserved
			for (int i = 0; i < 11; ++i)
				bw.Write((uint)0);

			// pixelformat
			bw.Write(new byte[] { 0x20, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x44, 0x58, 0x31, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
			bw.Write(new byte[] { 0x08, 0x10, 0x40, 0x00 }); // DWCAPS0
			bw.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

			// dxt10
			bw.Write((uint)HeaderSection.fmt);
			bw.Write((uint)(height > 1 ? 3 : 2));
			bw.Write((uint)0);
			bw.Write((uint)1);
			bw.Write((uint)0);
		}
	}

	public class Texture_I20: TextureBase {
		public const uint MAGIC = 0x5C4580B9;

		public Texture_I20(BinaryReader r) : base() {
			magic = r.ReadUInt32();
			dat1_size = r.ReadUInt32();
			unk = r.ReadBytes(28);
			Utils.Assert(magic == MAGIC, "Texture_I20(): bad magic");

			Init(r);
			raw = r.ReadBytes((int)(r.BaseStream.Length - r.BaseStream.Position));
		}
	}

	public class Texture_I29: TextureBase {
		public const uint MAGIC = 0x8F53A199;

		public Texture_I29(BinaryReader r) : base() {
			magic = r.ReadUInt32();
			dat1_size = r.ReadUInt32();
			unk = r.ReadBytes(28);
			Utils.Assert(magic == MAGIC, "Texture_I29(): bad magic");

			Init(r);
			raw = r.ReadBytes((int)(r.BaseStream.Length - r.BaseStream.Position));
		}
	}
}
