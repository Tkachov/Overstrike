// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.Generic {
	public abstract class ArraySection<T>: Section {
		public List<T> Values = new();

		protected abstract uint GetValueByteSize();
		protected abstract T Read(BinaryReader r);
		protected abstract void Write(BinaryWriter w, T v);

		public override void Load(byte[] bytes, DAT1 container) {
			using var r = new BinaryReader(new MemoryStream(bytes));
			var size = bytes.Length;
			var count = size / GetValueByteSize();
			Values.Clear();
			for (var i = 0; i < count; ++i) {
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

	public class UInt8ArraySection: ArraySection<byte> {
		protected override uint GetValueByteSize() { return 1; }
		protected override byte Read(BinaryReader r) { return r.ReadByte(); }
		protected override void Write(BinaryWriter w, byte v) { w.Write(v); }
	}

	public class UInt16ArraySection: ArraySection<ushort> {
		protected override uint GetValueByteSize() { return 2; }
		protected override ushort Read(BinaryReader r) { return r.ReadUInt16(); }
		protected override void Write(BinaryWriter w, ushort v) { w.Write(v); }
	}

	public class UInt32ArraySection: ArraySection<uint> {
		protected override uint GetValueByteSize() { return 4; }
		protected override uint Read(BinaryReader r) { return r.ReadUInt32(); }
		protected override void Write(BinaryWriter w, uint v) { w.Write(v); }
	}

	public class UInt64ArraySection: ArraySection<ulong> {
		protected override uint GetValueByteSize() { return 8; }
		protected override ulong Read(BinaryReader r) { return r.ReadUInt64(); }
		protected override void Write(BinaryWriter w, ulong v) { w.Write(v); }
	}
}
