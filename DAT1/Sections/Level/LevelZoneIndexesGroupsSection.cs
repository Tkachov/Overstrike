// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Level {
	public class LevelZoneIndexesGroupsSection: ArraySection<LevelZoneIndexesGroupsSection.Group> {
		public const uint TAG = 0xFC984113;

		public class Group {
			public ushort A, B, ZoneIndexesListFirstIndex, Zero1, ZoneIndexesListCount, Zero2, Zero3, Zero4;
		}

		protected override uint GetValueByteSize() { return 16; }

		protected override Group Read(BinaryReader r) {
			var result = new Group();

			result.A = r.ReadUInt16();
			result.B = r.ReadUInt16();
			result.ZoneIndexesListFirstIndex = r.ReadUInt16();
			result.Zero1 = r.ReadUInt16();
			result.ZoneIndexesListCount = r.ReadUInt16();
			result.Zero2 = r.ReadUInt16();
			result.Zero3 = r.ReadUInt16();
			result.Zero4 = r.ReadUInt16();

			return result;
		}

		protected override void Write(BinaryWriter w, Group v) {
			w.Write(v.A);
			w.Write(v.B);
			w.Write(v.ZoneIndexesListFirstIndex);
			w.Write(v.Zero1);
			w.Write(v.ZoneIndexesListCount);
			w.Write(v.Zero2);
			w.Write(v.Zero3);
			w.Write(v.Zero4);
		}
	}
}
