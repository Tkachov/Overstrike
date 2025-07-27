// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Wwiselookup {
	public class WwiselookupAssetsSection: ArraySection<WwiselookupAssetsSection.Entry> {
		public const uint TAG = 0x52B343E8;

		public class Entry {
			public ulong AssetId;
			public uint StringOffset;
		}

		protected override uint GetValueByteSize() { return 12; }

		protected override Entry Read(BinaryReader r) {
			var result = new Entry();

			result.AssetId = r.ReadUInt64();
			result.StringOffset = r.ReadUInt32();

			return result;
		}

		protected override void Write(BinaryWriter w, Entry v) {
			w.Write(v.AssetId);
			w.Write(v.StringOffset);
		}
	}
}
