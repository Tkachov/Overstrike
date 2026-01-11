// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.TOC {
	public class AssetHeadersSection: ByteBufferSection {
		public const uint TAG = 0x654BDED9; // Archive TOC Asset Header Data

		public virtual byte[] ReadHeaderAtOffset(int offset) {
			// class can't be abstract because DAT1.Section<> method tries to instantiate it
			// instead, derived class should be instantiated in TOC_I29.DetermineSectionsTypeDynamically()
			Utils.Assert(false, "AssetHeadersSection.ReadHeaderAtOffset() is used instead of override from derived class");
			return null;
		}
	}

	public class AssetHeadersSection_I29: AssetHeadersSection {
		public const uint TAG = 0x654BDED9; // Archive TOC Asset Header Data

		public override byte[] ReadHeaderAtOffset(int offset) {
			return Read(offset, 36);
		}
	}

	public class AssetHeadersSection_I30: AssetHeadersSection {
		public override byte[] ReadHeaderAtOffset(int offset) {
			byte[] sizes = Read(offset + 4, 4);

			using var r = new BinaryReader(new MemoryStream(sizes));
			r.ReadByte(); // unknown
			var pairs = r.ReadByte();
			var extra = r.ReadUInt16();

			var totalSize = 8 + pairs * 8 + extra;
			return Read(offset, totalSize);
		}
	}
}
