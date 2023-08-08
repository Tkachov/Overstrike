using System.Windows;

namespace Overstrike {
	public partial class ModsDetectionSplash: Window {
		public ModsDetectionSplash() {
			InitializeComponent();
		}

		public void SetCurrentMod(string path) {
			Dispatcher.Invoke(() => {
				OverlayOperationLabel.Text = path;
			});
		}
	}
}
