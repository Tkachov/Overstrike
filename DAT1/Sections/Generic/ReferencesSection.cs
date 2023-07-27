// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.Generic {
	public class ReferencesSection: ArraySection<ReferencesSection.ReferenceEntry> {
		public class ReferenceEntry {
			public ulong AssetId;
			public uint AssetPathStringOffset;
			public uint ExtensionHash;
		}

		public List<ReferenceEntry> Entries => Values;

		protected override uint GetValueByteSize() { return 16; }

		protected override ReferenceEntry Read(BinaryReader r) {
			var id = r.ReadUInt64();
			var so = r.ReadUInt32();
			var eh = r.ReadUInt32();
			return new ReferenceEntry() { AssetId = id, AssetPathStringOffset = so, ExtensionHash = eh };
		}

		protected override void Write(BinaryWriter w, ReferenceEntry v) {
			w.Write(v.AssetId);
			w.Write(v.AssetPathStringOffset);
			w.Write(v.ExtensionHash);
		}
	}
}
