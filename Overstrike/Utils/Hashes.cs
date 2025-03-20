// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Security.Cryptography;

namespace Overstrike.Utils {
	internal static class Hashes {
		internal static string GetFileSha1(string fn) {
			using var f = File.OpenRead(fn);
			return Convert.ToHexString(SHA1.HashData(f)).ToUpper();
		}
	}
}
