// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO;

namespace DAT1.Sections.Generic {
	public class ByteBufferSection: Section {
		public byte[] Buffer = null;

		public override void Load(byte[] bytes, DAT1 container) {
			Buffer = new byte[bytes.Length];

			using var ms = new MemoryStream(bytes);
			ms.Read(Buffer, 0, bytes.Length);
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			s.Write(Buffer);
			return s.ToArray();
		}

		public byte[] Read(int offset, int count) {
			var bytes = new byte[count];
			using var ms = new MemoryStream(bytes);
			ms.Write(Buffer, offset, count);
			return bytes;
		}

		public void Write(int offset, byte[] bytes) {
			for (int i = 0; i < bytes.Length; ++i) {
				Buffer[offset + i] = bytes[i];
			}
		}

		public void Extend(int count) {
			var newBuffer = new byte[Buffer.Length + count];
			using var ms = new MemoryStream(newBuffer);
			ms.Write(Buffer);
			Buffer = newBuffer;
		}
	}
}
