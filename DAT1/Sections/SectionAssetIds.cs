using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT1.Sections
{
	public class SectionAssetIds: Section
	{
		public List<UInt64> Ids = new List<UInt64>();

		public SectionAssetIds(BinaryReader r, uint size) {
			uint count = size / 8;
			for (uint i = 0; i<count; ++i) {
				Ids.Add(r.ReadUInt64());
			}
		}
		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);
			foreach (var e in Ids) {
				w.Write((UInt64)e);
			}
			return s.ToArray();
		}
	}
}
