// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Level {
	public class LevelSomeIndexesSection: ArraySection<int> {
		public const uint TAG = 0x95F91E24;

		// value == -1 or zone index

		protected override uint GetValueByteSize() { return 4; }
		protected override int Read(BinaryReader r) { return r.ReadInt32(); }
		protected override void Write(BinaryWriter w, int v) { w.Write(v); }
	}
}
