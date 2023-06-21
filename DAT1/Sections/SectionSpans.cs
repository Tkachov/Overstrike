using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT1.Sections
{
	public class SectionSpans: Section
	{
		public class Span
		{
			public uint AssetIndex, Count;
		}

		public List<Span> Entries = new List<Span>();

		public SectionSpans(BinaryReader r, uint size) {
			uint count = size / 8;
			for (uint i = 0; i<count; ++i) {
				var a = r.ReadUInt32();
				var b = r.ReadUInt32();
				Entries.Add(new Span() { AssetIndex = a, Count = b });
			}
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);
			foreach (var e in Entries) {
				w.Write((uint)e.AssetIndex);
				w.Write((uint)e.Count);
			}
			return s.ToArray();
		}
	}
}
