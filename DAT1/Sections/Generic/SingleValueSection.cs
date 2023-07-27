// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO;

namespace DAT1.Sections.Generic {
	public abstract class SingleValueSection<T>: Section {
		public T? Value;

		protected abstract T Read(BinaryReader r);
		protected abstract void Write(BinaryWriter w, T v);

		public override void Load(byte[] bytes, DAT1 container) {
			using var r = new BinaryReader(new MemoryStream(bytes));
			Value = Read(r);
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);
			Write(w, Value);
			return s.ToArray();
		}
	}

	public class SingleUInt32Section: SingleValueSection<uint> {
		protected override uint Read(BinaryReader r) { return r.ReadUInt32(); }
		protected override void Write(BinaryWriter w, uint v) { w.Write(v); }
	}
}
