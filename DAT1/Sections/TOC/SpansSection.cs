// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.TOC {
	public class SpansSection: ArraySection<SpansSection.Span> {
		public class Span {
			public uint AssetIndex, Count;
		}

		public const uint TAG = 0xEDE8ADA9; // Archive TOC Header

		public List<Span> Entries => Values;

		protected override uint GetValueByteSize() { return 8; }

		protected override Span Read(BinaryReader r) {
			var a = r.ReadUInt32();
			var b = r.ReadUInt32();
			return new Span() { AssetIndex = a, Count = b };
		}

		protected override void Write(BinaryWriter w, Span v) {
			w.Write(v.AssetIndex);
			w.Write(v.Count);
		}
	}
}
