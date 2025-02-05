using System.Windows;

namespace Updater {
	public partial class App: Application {
		protected override async void OnStartup(StartupEventArgs e) {
			var silentMode = e.Args.Length > 0 && e.Args[0] == "--silent";

			var window = new MainWindow(silentMode);
			if (!silentMode) {
				window.Show();
				return;
			}

			var hasUpdate = await window.CheckForUpdates();
			if (hasUpdate) {
				window.Show();
			} else {
				Shutdown();
			}
		}
	}
}
