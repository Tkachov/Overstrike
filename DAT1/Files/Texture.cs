// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Texture;
using System.Diagnostics;
using System.IO;

namespace DAT1.Files {
	public class Texture: DAT1 {
        public const uint MAGIC = 0x5C4580B9;

        uint magic, dat1_size;
        byte[] unk;
        byte[] raw;

        public Texture(BinaryReader r): base() {
            magic = r.ReadUInt32();
            dat1_size = r.ReadUInt32();
            unk = r.ReadBytes(28);
            Debug.Assert(magic == MAGIC, "Texture(): bad magic");

            Init(r);
            raw = r.ReadBytes((int)(r.BaseStream.Length - r.BaseStream.Position));
        }

        public TextureHeaderSection HeaderSection => Section<TextureHeaderSection>(TextureHeaderSection.TAG);

		public byte[] GetDDS() {
			uint width = HeaderSection.hd_width;
			uint height = HeaderSection.hd_height;

            MemoryStream result = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(result);
			bw.Write(new byte[] { 0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 0x07, 0x10, 0x0A, 0x00 });
			bw.Write(height);
			bw.Write(width);

			// pitch, depth, mipmaps
			uint pitch = (width * 32 + 7) / 8;
			bw.Write(pitch);
			bw.Write((uint)0);
            bw.Write((uint)0);

			// reserved
			for (int i=0; i<11; ++i)
                bw.Write((uint)0);

			// pixelformat
			bw.Write(new byte[] { 0x20, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x44, 0x58, 0x31, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
			bw.Write(new byte[] { 0x00, 0x10, 0x00, 0x00 }); // DWCAPS0
			bw.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

			// dxt10
			bw.Write((uint)HeaderSection.fmt);
			bw.Write((uint)(height > 1 ? 3 : 2));
            bw.Write((uint)0);
            bw.Write((uint)1);
            bw.Write((uint)0);

			bw.Write(raw);

            bw.Flush();
            result.Flush();
            return result.ToArray();
        }
    }
}
