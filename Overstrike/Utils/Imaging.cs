using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
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
