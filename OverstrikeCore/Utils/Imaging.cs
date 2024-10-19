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
