// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Wwiselookup {
	public class WwiselookupEventsSection: ArraySection<WwiselookupEventsSection.Entry> {
		public const uint TAG = 0x739B21E0;

		public class Entry {
			public ulong SoundbankAssetId, AssetId; // asset is not always existing asset, sometimes is performanceset
			public uint Zero1, Zero2;
			public uint NameStringOffset;
			public int IndexOfNextEntry; // entry that has longer name that has this entry's name as prefix; on practice all such entries just have "_npc" suffix
			public uint Flags;
		}

		protected override uint GetValueByteSize() { return 36; }

		protected override Entry Read(BinaryReader r) {
			var result = new Entry();

			result.SoundbankAssetId = r.ReadUInt64();
			result.AssetId = r.ReadUInt64();
			result.Zero1 = r.ReadUInt32();
			result.Zero2 = r.ReadUInt32();
			result.NameStringOffset = r.ReadUInt32();
			result.IndexOfNextEntry = r.ReadInt32();
			result.Flags = r.ReadUInt32();

			return result;
		}

		protected override void Write(BinaryWriter w, Entry v) {
			w.Write(v.SoundbankAssetId);
			w.Write(v.AssetId);
			w.Write(v.Zero1);
			w.Write(v.Zero2);
			w.Write(v.NameStringOffset);
			w.Write(v.IndexOfNextEntry);
			w.Write(v.Flags);
		}
	}
}
