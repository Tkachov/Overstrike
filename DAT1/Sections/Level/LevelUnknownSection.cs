// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Level {
	public class LevelUnknownSection: ArraySection<LevelUnknownSection.Group> {
		public const uint TAG = 0xFD39FA81;

		public class Group {
			public uint A, B, C, Zero;
		}

		protected override uint GetValueByteSize() { return 16; }

		protected override Group Read(BinaryReader r) {
			var result = new Group();

			result.A = r.ReadUInt32();
			result.B = r.ReadUInt32();
			result.C = r.ReadUInt32();
			result.Zero = r.ReadUInt32();

			return result;
		}

		protected override void Write(BinaryWriter w, Group v) {
			w.Write(v.A);
			w.Write(v.B);
			w.Write(v.C);
			w.Write(v.Zero);
		}
	}
}
