// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Diagnostics;

namespace DAT1 {
	public class Utils {
		public static string Normalize(string data) {
			string result = data.ToLower().Replace('\\', '/');
			string replaced = "";
			bool slash = false;

			foreach (var c in result) {
				if (c == '/') {
					if (slash) continue;
					slash = true;
				} else {
					slash = false;
				}

				replaced += c;
			}

			return replaced;
		}

		[Conditional("DEBUG")]
		public static void Assert(bool condition, string message) {
			if (!condition) {
				throw new System.Exception(message);
			}
		}

		[Conditional("DEBUG")]
		public static void Assert(bool condition) => Assert(condition, string.Empty);
	}
}
