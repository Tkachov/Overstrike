// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Data;
using Overstrike.Detectors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;

namespace Overstrike {
	public partial class App: Application {
		AppSettings Settings = new AppSettings();
		List<Profile> Profiles = new List<Profile>();
		List<ModEntry> Mods = new List<ModEntry>();
		public SuitsCache SuitsCache = new();

		ModsDetection? _detection = null;

		protected override void OnStartup(StartupEventArgs e) {
			CreateSubdirectories();
			ReadSettings();
			LoadProfiles();
			
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			if (Profiles.Count == 0) {
				var window = new FirstLaunch();
				window.ShowDialog();

				LoadProfiles();
			}

			bool syncModsLibrary = true;
			if (Settings.PreferCachedModsLibrary && ModsDetectionCached.CacheFileExists()) {
				syncModsLibrary = false;
			}

			if (syncModsLibrary) {
				if (!RunDetectionAndShowSplash()) {
					Shutdown();
					return;
				}
			} else {
				LoadModsFromCache();
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
				var path = Path.Combine(cwd, dirname);

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
			var path = Path.Combine(cwd, "Profiles/Settings.json");
			try {
				var s = new AppSettings(path);
				Settings = s;
			} catch (Exception) {}
		}

		public void WriteSettings() {
			var cwd = Directory.GetCurrentDirectory();
			var path = Path.Combine(cwd, "Profiles/Settings.json");
			try {
				Settings.Save(path);
			} catch (Exception) {}
		}

		private void LoadProfiles() {
			Profiles.Clear();

			var cwd = Directory.GetCurrentDirectory();
			var path = Path.Combine(cwd, "Profiles");

			string[] files = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
			foreach (string file in files) {
				LoadProfile(file);
			}
		}

		private void LoadProfile(string file) {
			var basename = Path.GetFileName(file);
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
			var path = Path.Combine(cwd, "Mods Library");

			_detection = Settings.CacheModsLibrary ? new ModsDetectionCached() : new ModsDetection();
			_detection.Detect(path, Mods);
			_detection = null;
		}

		private void DetectModsInFiles(List<string> filenames) {
			var cwd = Directory.GetCurrentDirectory();
			var path = Path.Combine(cwd, "Mods Library");

			_detection = Settings.CacheModsLibrary ? new ModsDetectionCached() : new ModsDetection();
			_detection.DetectInFiles(path, filenames, Mods);
			_detection = null;
		}

		private bool LoadModsFromCache() {
			return ModsDetectionCached.LoadModsFromCache(Mods);
		}

		public List<ModEntry> ReloadMods(bool forceSync = false) {
			var oldMods = Mods;
			Mods = new();

			bool syncModsLibrary = true;
			if (Settings.PreferCachedModsLibrary && ModsDetectionCached.CacheFileExists()) {
				syncModsLibrary = false;
			}
			if (forceSync) {
				syncModsLibrary = true;
			}

			if (syncModsLibrary) {
				if (!RunDetectionAndShowSplash()) {
					Mods = oldMods;
				}
			} else {
				if (!LoadModsFromCache()) {
					Mods = oldMods;
				}
			}

			return Mods;
		}

		public List<ModEntry> ReloadModsOnlyForFiles(List<string> filenames) {
			var oldMods = Mods;
			Mods = new();

			// new mods = old ones + detected in given files
			foreach (var mod in oldMods) {
				Mods.Add(mod);
			}

			if (!RunDetectionAndShowSplash(filenames)) {
				Mods = oldMods;
			}

			return Mods;
		}

		// threads

		private bool RunDetectionAndShowSplash() {
			Thread detectionThread = new(DetectMods);
			return RunDetectionAndShowSplash(detectionThread);
		}

		private bool RunDetectionAndShowSplash(List<string> filenames) {
			Thread detectionThread = new(() => DetectModsInFiles(filenames));
			return RunDetectionAndShowSplash(detectionThread);
		}

		private bool RunDetectionAndShowSplash(Thread detectionThread) {
			detectionThread.Start();

			var splashWindow = new ModsDetectionSplash();
			Thread splashThread = new(() => UpdateDetectionSplash(detectionThread, splashWindow));

			splashThread.Start();
			if (detectionThread.Join(500)) {
				return true;
			}

			if (detectionThread.IsAlive) {
				splashWindow.ShowDialog();

				if (detectionThread.IsAlive) {
					return false;
				}
			}

			return true;
		}

		private void UpdateDetectionSplash(Thread detectionThread, ModsDetectionSplash window) {
			if (detectionThread != null) {
				while (detectionThread.IsAlive) {
					try {
						if (_detection != null) {
							var file = _detection.CurrentFile;
							Dispatcher.Invoke(() => {
								window.SetCurrentMod(file);
							});
						}
					} catch {}

					Thread.Sleep(100);
				}
			}

			try {
				Dispatcher.Invoke(window.Close);
			} catch {}
		}
	}
}
