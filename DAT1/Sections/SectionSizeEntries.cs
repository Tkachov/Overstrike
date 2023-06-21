using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT1.Sections
{
	public class SectionSizeEntries: Section
	{
		public class SizeEntry
		{
			public uint Always1, Value, Index;
		}

		public List<SizeEntry> Entries = new List<SizeEntry>();

		public SectionSizeEntries(BinaryReader r, uint size) {
			uint count = size / 12;
			for (uint i=0; i<count; ++i) {
				var a1 = r.ReadUInt32();
				var v = r.ReadUInt32();
				var ndx = r.ReadUInt32();
				Entries.Add(new SizeEntry() { Always1 = a1, Value = v, Index = ndx });
			}
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);
			foreach (var e in Entries) {
				w.Write((uint)e.Always1);
				w.Write((uint)e.Value);
				w.Write((uint)e.Index);
			}
			return s.ToArray();
		}
	}
}
