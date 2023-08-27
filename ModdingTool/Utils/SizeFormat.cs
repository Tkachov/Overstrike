// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

namespace ModdingTool.Utils {
	public static class SizeFormat {
		public static string FormatSize(uint bytesCount) {
			var v = bytesCount;
			var r = "";
			var u = "B";

			if (v > 1024) {
				r = Remainder(v);
				v /= 1024;
				u = "KB";

				if (v > 1024) {
					r = Remainder(v);
					v /= 1024;
					u = "MB";

					if (v > 1024) {
						r = Remainder(v);
						v /= 1024;
						u = "GB";
					}
				}
			}

			return $"{v}{r} {u}";
		}

		private static string Remainder(uint v) {
			if (v % 1024 == 0) return "";
			var v2 = (v % 1024) / 1024.0;
			int v3 = (int)(v2 * 10);
			if (v3 == 0) return ".1";
			return "." + v3;
		}
	}
}
