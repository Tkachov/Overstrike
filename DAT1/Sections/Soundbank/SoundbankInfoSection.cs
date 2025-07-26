// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Soundbank {
	public class SoundbankInfoSection: ArraySection<SoundbankInfoSection.Event> {
		public const uint TAG = 0x0E19E37F; // Sound Bank Info

		public class Event {
			public uint EventId;
			public ushort Small, Flags, Zero, Flags2, A, B;
			// A == B most of the time (if not, A < B)
		}

		protected override uint GetValueByteSize() { return 16; }

		protected override Event Read(BinaryReader r) {
			var result = new Event();

			result.EventId = r.ReadUInt32();
			result.Small = r.ReadUInt16();
			result.Flags = r.ReadUInt16();
			result.Zero = r.ReadUInt16();
			result.Flags2 = r.ReadUInt16();
			result.A = r.ReadUInt16();
			result.B = r.ReadUInt16();

			return result;
		}

		protected override void Write(BinaryWriter w, Event v) {
			w.Write(v.A);
			w.Write(v.B);
		}

		static public uint FNV1(string s) {
			uint v = 2166136261;
			s = s.ToLower();
			foreach (var c in s) {
				v = (v * 16777619) ^ ((uint)c & 0xFF);
			}
			return v;
		}
	}
}
