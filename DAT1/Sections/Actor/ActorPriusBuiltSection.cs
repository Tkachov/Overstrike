// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Localization {
	public class ActorPriusBuiltSection: ArraySection<ActorPriusBuiltSection.Entry> {
		public const uint TAG = 0x135832C8; // Actor Prius Built

		public class Entry {
			public ulong Unknown0;
			public uint StringOffset;
			public uint StringHash;
			public uint Unknown16;
			public uint Offset;
			public uint Size;
			public uint Unknown28;
		}

		protected override uint GetValueByteSize() { return 32; }

		protected override Entry Read(BinaryReader r) {
			var result = new Entry();

			result.Unknown0 = r.ReadUInt64();
			result.StringOffset = r.ReadUInt32();
			result.StringHash = r.ReadUInt32();
			result.Unknown16 = r.ReadUInt32();
			result.Offset = r.ReadUInt32();
			result.Size = r.ReadUInt32();
			result.Unknown28 = r.ReadUInt32();

			return result;
		}

		protected override void Write(BinaryWriter w, Entry v) {
			w.Write(v.Unknown0);
			w.Write(v.StringOffset);
			w.Write(v.StringHash);
			w.Write(v.Unknown16);
			w.Write(v.Offset);
			w.Write(v.Size);
			w.Write(v.Unknown28);
		}
	}
}
