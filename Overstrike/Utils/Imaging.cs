// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO;
using System.Windows.Media.Imaging;

namespace Overstrike.Utils {
	internal static class Imaging {
		#region .net types

		internal static BitmapImage ConvertToBitmapImage(byte[] bytes) {
			using (MemoryStream memory = new MemoryStream()) {
				memory.Write(bytes, 0, bytes.Length);
				memory.Position = 0;
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = memory;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();

				return bitmapImage;
			}
		}

		#endregion
	}
}
