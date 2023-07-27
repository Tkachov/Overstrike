// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Detectors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Overstrike {
	public partial class App: Application {
		AppSettings Settings = new AppSettings();
		List<Profile> Profiles = new List<Profile>();
		List<ModEntry> Mods = new List<ModEntry>();

		private ModsDetection _detection = new ModsDetection();

		protected override void OnStartup(StartupEventArgs e) {
			CreateSubdirectories();
			ReadSettings();
			LoadProfiles();
			DetectMods(); // TODO: launch a separate thread that waits a sec and shows a window with progress bar / current filename if detection still goes

			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			if (Profiles.Count == 0) {
				var window = new FirstLaunch();
				window.ShowDialog();

				LoadProfiles();
			}

			ShutdownMode = ShutdownMode.OnLastWindowClose;
			if (Profiles.Count > 0) {
				var window = new MainWindow(Settings, Profiles, Mods);
				window.Show();
			} else {
				Shutdown();
			}
		}

		private void CreateSubdirectories() {
			bool success = CreateSubdirectory("Mods Library");
			success = CreateSubdirectory("Profiles") && success;

			if (!success) {
				MessageBox.Show("Couldn't create app directories!", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
				Shutdown();
			}
		}

		private bool CreateSubdirectory(string dirname) {
			try {
				var cwd = Directory.GetCurrentDirectory();
				var path = System.IO.Path.Combine(cwd, dirname);

				if (!Directory.Exists(path)) {
					Directory.CreateDirectory(path);
					return true;
				}

				return true;
			} catch (Exception) {}

			return false;
		}

		private void ReadSettings() {
			var cwd = Directory.GetCurrentDirectory();
			var path = System.IO.Path.Combine(cwd, "Profiles/Settings.json");
			try {
				var s = new AppSettings(path);
				Settings = s;
			} catch (Exception) {}
		}

		public void WriteSettings() {
			var cwd = Directory.GetCurrentDirectory();
			var path = System.IO.Path.Combine(cwd, "Profiles/Settings.json");
			try {
				Settings.Save(path);
			} catch (Exception) {}
		}

		private void LoadProfiles() {
			Profiles.Clear();

			var cwd = Directory.GetCurrentDirectory();
			var path = System.IO.Path.Combine(cwd, "Profiles");

			string[] files = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
			foreach (string file in files) {
				LoadProfile(file);
			}
		}

		private void LoadProfile(string file) {
			var basename = System.IO.Path.GetFileName(file);
			if (basename == "Settings.json") {
				return;
			}

			try {
				var p = new Profile(file);
				if (p != null) {
					Profiles.Add(p);
				}
			} catch (Exception) {}
		}

		public List<Profile> ReloadProfiles() {
			LoadProfiles();
			return Profiles;
		}

		private void DetectMods() {
			var cwd = Directory.GetCurrentDirectory();
			var path = System.IO.Path.Combine(cwd, "Mods Library");
			_detection.Detect(path, Mods);
		}

		public List<ModEntry> ReloadMods() {
			DetectMods();
			return Mods;
		}
	}
}
