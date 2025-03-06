// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO;
using System.Windows.Media.Imaging;

namespace OverstrikeShared.Utils {
	public static class Imaging {
		public static BitmapImage LoadImage(byte[] bytes) {
			using var memoryStream = new MemoryStream();
			memoryStream.Write(bytes, 0, bytes.Length);
			memoryStream.Position = 0;

			return LoadImage(memoryStream);
		}

		public static BitmapImage LoadImage(Stream stream) {
			var bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = stream;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			return bitmapImage;
		}
	}
}
