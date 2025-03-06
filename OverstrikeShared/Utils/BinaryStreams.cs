// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO;

namespace OverstrikeShared.Utils {
	public class BinaryStreams {
		public static void Align16(BinaryReader br) {
			Align(br, 16);
		}

		public static void Align(BinaryReader br, int count) {
			var pos = br.BaseStream.Position % count;
			if (pos != 0) {
				var rem = count - pos;
				br.ReadBytes((int)rem);
			}
		}

		public static void Align16(BinaryWriter bw) {
			Align(bw, 16);
		}

		public static void Align(BinaryWriter bw, int count) {
			var pos = bw.BaseStream.Position % count;
			if (pos != 0) {
				var rem = count - pos;
				bw.Write(new byte[rem]);
			}
		}
	}
}
