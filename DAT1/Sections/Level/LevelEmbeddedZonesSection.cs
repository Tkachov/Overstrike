// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Level {
	public class LevelEmbeddedZonesSection: ArraySection<LevelEmbeddedZonesSection.Entry> {
		public const uint TAG = 0x5818CB19;

		public class Entry {
			public ulong ZoneId;
			public uint A;
			public uint B;
		}

		protected override uint GetValueByteSize() { return 16; }

		protected override Entry Read(BinaryReader r) {
			var result = new Entry();

			result.ZoneId = r.ReadUInt64();
			result.A = r.ReadUInt32();
			result.B = r.ReadUInt32();

			return result;
		}

		protected override void Write(BinaryWriter w, Entry v) {
			w.Write(v.ZoneId);
			w.Write(v.A);
			w.Write(v.B);
		}
	}
}
