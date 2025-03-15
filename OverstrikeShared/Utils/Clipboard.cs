// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

namespace OverstrikeShared.Utils {
	public class Clipboard {
		public static bool SetClipboard(string text) {
			try {
				System.Windows.Clipboard.SetText(text);
				return true;
			} catch {
				// if failed once, try a few more times (in some cases clipboard in windows might fail to open)
				for (int i = 0; i < 10; i++) {
					try {
						System.Windows.Clipboard.SetText(text);
						return true;
					} catch { }
				}
			}

			// if reached here, it means we failed all those times
			return false;
		}
	}
}
