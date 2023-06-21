using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT1.Sections
{
	public class SectionOffsets: Section
	{
		public class OffsetEntry
		{
			public uint ArchiveIndex, Offset;
		}

		public List<OffsetEntry> Entries = new List<OffsetEntry>();

		public SectionOffsets(BinaryReader r, uint size) {
			uint count = size / 8;
			for (uint i = 0; i<count; ++i) {
				var a = r.ReadUInt32();
				var b = r.ReadUInt32();
				Entries.Add(new OffsetEntry() { ArchiveIndex = a, Offset = b });
			}
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);
			foreach (var e in Entries) {
				w.Write((uint)e.ArchiveIndex);
				w.Write((uint)e.Offset);
			}
			return s.ToArray();
		}
	}
}
