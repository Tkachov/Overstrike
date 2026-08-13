// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO;

namespace Overstrike.Utils {
	internal static class FileAccess {
		internal static void RemoveReadOnlyAttribute(string path, bool throwOnFailure = false) {
			try {
				if (File.Exists(path)) {
					var attributes = File.GetAttributes(path);
					if ((attributes & FileAttributes.ReadOnly) != 0) {
						attributes &= ~FileAttributes.ReadOnly;
						File.SetAttributes(path, attributes);
					}
				}
			} catch {
				if (throwOnFailure) {
					throw;
				}
			}
		}
	}
}
