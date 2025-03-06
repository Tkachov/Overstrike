// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

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
