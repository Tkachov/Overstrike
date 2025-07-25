// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Level {
	public class LevelRandomListSection: ArraySection<LevelRandomListSection.Entry> {
		public const uint TAG = 0xC30D92B6;

		public class Entry {
			public uint Flags1, Zero, Flags2;
			public uint D; // 0 or 362
			public uint E; // 144, 700, 750, 900, 1150, 2000, 2164, 4000
			public uint F; // 154, 800, 950, 1200, 1350, 2050, 2292, 4050
		}

		protected override uint GetValueByteSize() { return 24; }

		protected override Entry Read(BinaryReader r) {
			var result = new Entry();

			result.Flags1 = r.ReadUInt32();
			result.Zero = r.ReadUInt32();
			result.Flags2 = r.ReadUInt32();
			result.D = r.ReadUInt32();
			result.E = r.ReadUInt32();
			result.F = r.ReadUInt32();

			return result;
		}

		protected override void Write(BinaryWriter w, Entry v) {
			w.Write(v.Flags1);
			w.Write(v.Zero);
			w.Write(v.Flags2);
			w.Write(v.D);
			w.Write(v.E);
			w.Write(v.F);
		}
	}
}
