// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Soundbank {
	public class SoundbankStreamLookupSection: ArraySection<SoundbankStreamLookupSection.Pair> {
		public const uint TAG = 0x024A788B; // Sound Bank Stream Lookup

		public class Pair {
			public uint SourceId, EventId;
		}

		protected override uint GetValueByteSize() { return 8; }

		protected override Pair Read(BinaryReader r) {
			var result = new Pair();

			result.SourceId = r.ReadUInt32();
			result.EventId = r.ReadUInt32();

			return result;
		}

		protected override void Write(BinaryWriter w, Pair v) {
			w.Write(v.SourceId);
			w.Write(v.EventId);
		}
	}
}
