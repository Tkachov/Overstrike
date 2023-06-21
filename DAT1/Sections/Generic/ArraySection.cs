using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT1.Sections.Generic {
	public abstract class ArraySection<T>: Section {
		public List<T> Values = new List<T>();

		protected abstract uint GetValueByteSize();
		protected abstract T Read(BinaryReader r);
		protected abstract void Write(BinaryWriter w, T v);

		public ArraySection(BinaryReader r, uint size) {
			uint count = size / GetValueByteSize();
			for (uint i = 0; i < count; ++i) {
				Values.Add(Read(r));
			}
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);
			foreach (var e in Values) {
				Write(w, e);
			}
			return s.ToArray();
		}
	}

	public class UInt32ArraySection: ArraySection<UInt32> {
		protected override uint GetValueByteSize() { return 4; }
		protected override UInt32 Read(BinaryReader r) { return r.ReadUInt32(); }
		protected override void Write(BinaryWriter w, UInt32 v) { w.Write(v); }

		public UInt32ArraySection(BinaryReader r, uint size): base(r, size) {}
	}
}
