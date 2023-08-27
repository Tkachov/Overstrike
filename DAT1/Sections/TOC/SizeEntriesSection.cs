// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.TOC {
	public class SizeEntriesSection_I16 : ArraySection<SizeEntriesSection_I16.SizeEntry> {
		public class SizeEntry {
			public uint Always1, Value, Index;
		}

		public const uint TAG = 0x65BCF461; // Archive TOC Asset Metadata

		public List<SizeEntry> Entries => Values;

		protected override uint GetValueByteSize() { return 12; }

		protected override SizeEntry Read(BinaryReader r) {
			var a1 = r.ReadUInt32();
			var v = r.ReadUInt32();
			var ndx = r.ReadUInt32();
			return new SizeEntry() { Always1 = a1, Value = v, Index = ndx };
		}

		protected override void Write(BinaryWriter w, SizeEntry v) {
			w.Write(v.Always1);
			w.Write(v.Value);
			w.Write(v.Index);
		}
	}

	public class SizeEntriesSection_I29: ArraySection<SizeEntriesSection_I29.SizeEntry> {
		public class SizeEntry {
			public uint Size, ArchiveIndex, Offset;
			public int HeaderOffset;
		}

		public const uint TAG = 0x65BCF461; // Archive TOC Asset Metadata

		public List<SizeEntry> Entries => Values;

		protected override uint GetValueByteSize() { return 16; }

		protected override SizeEntry Read(BinaryReader r) {
			var sz = r.ReadUInt32();
			var ai = r.ReadUInt32();
			var o = r.ReadUInt32();
			var ho = r.ReadInt32();
			return new SizeEntry() { Size = sz, ArchiveIndex = ai, Offset = o, HeaderOffset = ho };
		}

		protected override void Write(BinaryWriter w, SizeEntry v) {
			w.Write(v.Size);
			w.Write(v.ArchiveIndex);
			w.Write(v.Offset);
			w.Write(v.HeaderOffset);
		}
	}
}
