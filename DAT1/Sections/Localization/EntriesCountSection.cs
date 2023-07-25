// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;

namespace DAT1.Sections.Localization {
	public class EntriesCountSection: Section {
		public const uint TAG = 0xD540A903;

		public uint Count;

		public EntriesCountSection(BinaryReader r, uint size) {
			Count = r.ReadUInt32();
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);
			w.Write((UInt32)Count);
			return s.ToArray();
		}
	}
}
