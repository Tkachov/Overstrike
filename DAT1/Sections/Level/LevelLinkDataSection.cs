// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Level {
	public class LevelLinkDataSection: ArraySection<LevelLinkDataSection.Entry> {
		public const uint TAG = 0x3395AEC1;

		public class Entry {
			public ulong A, B, C;
			public uint NameHash, NameIndex, Index2;
			public uint Always65535;
			public float H, I, J;
			public uint Zero;
		}

		protected override uint GetValueByteSize() { return 56; }

		protected override Entry Read(BinaryReader r) {
			var result = new Entry();

			result.A = r.ReadUInt64();
			result.B = r.ReadUInt64();
			result.C = r.ReadUInt64();
			result.NameHash = r.ReadUInt32();
			result.NameIndex = r.ReadUInt32();
			result.Index2 = r.ReadUInt32();			
			result.Always65535 = r.ReadUInt32();
			result.H = r.ReadSingle();
			result.I = r.ReadSingle();
			result.J = r.ReadSingle();
			result.Zero = r.ReadUInt32();

			return result;
		}

		protected override void Write(BinaryWriter w, Entry v) {
			w.Write(v.A);
			w.Write(v.B);
			w.Write(v.C);
			w.Write(v.NameHash);
			w.Write(v.NameIndex);
			w.Write(v.Index2);
			w.Write(v.Always65535);
			w.Write(v.H);
			w.Write(v.I);
			w.Write(v.J);
			w.Write(v.Zero);
		}
	}
}
