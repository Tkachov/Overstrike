// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Level {
	public class LevelZonesBuiltSection: ArraySection<LevelZonesBuiltSection.Zone> {
		public const uint TAG = 0x4E023760; // Level Zones Built

		public class Zone {
			public ulong NameHash;
			public uint Zero1, Zero2;
			public uint NameIndex;
			public int MainZoneIndex; // -1 if not "main" (not sure how else to call that, a lot of these are tiles)
			public uint Type; // 0 -- regular, 1 -- impostors, 2 -- lgt, 3 -- light_grid
			public uint Zero3;
		}

		/*
		# bit of stats:
		2137 "main"

		4490 impostors
		2027 lgt
		1935 light_grid
		*/

		protected override uint GetValueByteSize() { return 32; }

		protected override Zone Read(BinaryReader r) {
			var result = new Zone();

			result.NameHash = r.ReadUInt64();
			result.Zero1 = r.ReadUInt32();
			result.Zero2 = r.ReadUInt32();
			result.NameIndex = r.ReadUInt32();
			result.MainZoneIndex = r.ReadInt32();
			result.Type = r.ReadUInt32();
			result.Zero3 = r.ReadUInt32();

			return result;
		}

		protected override void Write(BinaryWriter w, Zone v) {
			w.Write(v.NameHash);
			w.Write(v.Zero1);
			w.Write(v.Zero2);
			w.Write(v.NameIndex);
			w.Write(v.MainZoneIndex);
			w.Write(v.Type);
			w.Write(v.Zero3);
		}
	}
}
