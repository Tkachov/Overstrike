// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;

namespace DAT1.Sections.Localization {
	public class ActorObjectBuiltSection: Section {
		public const uint TAG = 0x364A6C7C; // Actor Object Built

		public float[,] Matrix;
		public byte[] Zeroes64; // 28 bytes of 0s
		public uint Type;
		public byte[] Zeroes96; // 16 bytes of 0s
		public float X, Y, Z;
		public uint SectionSize;
		public byte[] Raw;

		public ActorObjectBuiltSection() {
			Matrix = new float[4, 4];
			Raw = Array.Empty<byte>();
		}

		override public void Load(byte[] bytes, DAT1 container) {
			using var r = new BinaryReader(new MemoryStream(bytes));

			for (int row = 0; row < 4; ++row) {
				for (int col = 0; col < 4; ++col) {
					Matrix[row, col] = r.ReadSingle();
				}
			}

			Zeroes64 = r.ReadBytes(28);
			Type = r.ReadUInt32();
			Zeroes96 = r.ReadBytes(16);
			X = r.ReadSingle();
			Y = r.ReadSingle();
			Z = r.ReadSingle();
			SectionSize = r.ReadUInt32();

			Raw = r.ReadBytes(bytes.Length - 4*4*4 - 32 - 16 - 16);
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);

			for (int row = 0; row < 4; ++row) {
				for (int col = 0; col < 4; ++col) {
					w.Write(Matrix[row, col]);
				}
			}

			w.Write(Zeroes64);
			w.Write(Type);
			w.Write(Zeroes96);
			w.Write(X);
			w.Write(Y);
			w.Write(Z);
			w.Write(SectionSize);
			w.Write(Raw);

			return s.ToArray();
		}
	}
}
