// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using BCnEncoder.Decoder;

namespace Overstrike.Utils {
	internal static class Imaging {
		#region dds

		internal static Bitmap DdsToBitmap(byte[] ddsBytes) {
			var d = new BcDecoder();
			var dds = BCnEncoder.Shared.ImageFiles.DdsFile.Load(new MemoryStream(ddsBytes));
			var colors = d.Decode(dds);

			int w = (int)dds.Faces[0].Width;
			int h = (int)dds.Faces[0].Height;
			var bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			for (int y = 0; y < h; ++y) {
				for (int x = 0; x < w; ++x) {
					var rgba = colors[y * w + x];
					var clr = Color.FromArgb(rgba.a, rgba.r, rgba.g, rgba.b);
					bitmap.SetPixel(x, y, clr);
				}
			}

			return bitmap;
		}

		#endregion
		#region .net types

		internal static BitmapImage ConvertToBitmapImage(byte[] bytes) { // for resources
			using var memoryStream = new MemoryStream();
			memoryStream.Write(bytes, 0, bytes.Length);
			memoryStream.Position = 0;

			var bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = memoryStream;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			return bitmapImage;
		}

		internal static BitmapSource ConvertToBitmapImage(Bitmap bitmap) {
			using var memoryStream = new MemoryStream();
			bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png); // expecting argb bitmap here, like one made with DdsToBitmap()
			memoryStream.Position = 0;

			var bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = memoryStream;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			return bitmapImage;
		}

		#endregion
	}
}
