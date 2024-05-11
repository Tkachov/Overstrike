// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO;

namespace DAT1.Sections.Texture {
	public class TextureHeaderSection: Section {
		public const uint TAG = 0x4EDE3593;

		public uint sd_len, hd_len;
		public ushort hd_width, hd_height;
		public ushort sd_width, sd_height;
		public ushort array_size;
		public byte stex_format, planes;
		public ushort fmt;
		public ulong unk;
		public byte sd_mipmaps, unk2, hd_mipmaps, unk3;
		public byte[] unk4;

		public override void Load(byte[] bytes, DAT1 container) {
			using var r = new BinaryReader(new MemoryStream(bytes));
			var size = bytes.Length;

			sd_len = r.ReadUInt32();
			hd_len = r.ReadUInt32();
			hd_width = r.ReadUInt16();
			hd_height = r.ReadUInt16();
			sd_width = r.ReadUInt16();
			sd_height = r.ReadUInt16();
			array_size = r.ReadUInt16();
			stex_format = r.ReadByte();
			planes = r.ReadByte();
			fmt = r.ReadUInt16();
			unk = r.ReadUInt64();
			sd_mipmaps = r.ReadByte();
			unk2 = r.ReadByte();
			hd_mipmaps = r.ReadByte();
			unk3 = r.ReadByte();
			unk4 = r.ReadBytes(size - 34);
		}

		override public byte[] Save() {
			return null; // TODO
		}
	}
}
