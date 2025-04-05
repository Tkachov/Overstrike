// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Data;
using Overstrike.Detectors;
using Overstrike.Games;
using Overstrike.MetaInstallers;
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

			if (e.Args.Length != 0) {
				var exitCode = HandleCommands(e.Args);
				if (exitCode != -1) {
					Shutdown(exitCode);
					return;
				}
			}
			
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

		#region CLI

		private int HandleCommands(string[] arguments) {
			var command = arguments[0];

			if (command == "create-profile" && arguments.Length >= 3) return Command_CreateProfile(arguments[1], arguments[2]);
			else if (command == "delete-profile" && arguments.Length >= 2) return Command_DeleteProfile(arguments[1]);
			else if (command == "add-mod" && arguments.Length >= 2) return Command_AddMod(arguments[1]);
			else if (command == "delete-mod" && arguments.Length >= 2) return Command_DeleteMod(arguments[1]);
			else if (command == "enable-mod" && arguments.Length >= 3) return Command_EnableMod(arguments[1], arguments[2]);
			else if (command == "disable-mod" && arguments.Length >= 3) return Command_DisableMod(arguments[1], arguments[2]);
			else if (command == "install-mods" && arguments.Length >= 2) return Command_InstallMods(arguments[1]);
			else if (command == "uninstall-mods" && arguments.Length >= 2) return Command_UninstallMods(arguments[1]);

			return -1;
		}

		private static int Command_CreateProfile(string profileName, string gameFolder) {
			var detectedGame = GameBase.DetectGameInstallation(gameFolder);
			if (detectedGame == null) {
				return 1;
			}

			try {
				var p = new Profile(profileName, detectedGame, gameFolder);
				if (!p.Save()) {
					return 2;
				}
			} catch {
				return 2;
			}

			return 0;
		}

		private static int Command_DeleteProfile(string profileName) {
			var cwd = Directory.GetCurrentDirectory();
			var path = Path.Combine(cwd, "Profiles");
			var filename = Path.Combine(path, profileName + ".json");

			if (!File.Exists(filename)) {
				return 1;
			}

			try {
				File.Delete(filename);
			} catch {
				return 2;
			}

			return 0;
		}

		private int Command_AddMod(string fileName) { // assuming file is in the library folder already
			var path = GetLibraryFolder();
			var warnings = new List<string>();
			var filenames = new List<string>() { fileName };

			try {
				var detection = Settings.CacheModsLibrary ? new ModsDetectionCached() : new ModsDetection();
				detection.DetectInFiles(path, filenames, Mods, warnings);
			} catch {
				return 1;
			}

			return (Mods.Count > 0 ? 0 : 2);
		}

		private static int Command_DeleteMod(string filename) {
			if (!File.Exists(filename)) {
				return 1;
			}

			try {
				File.Delete(filename);
			} catch {
				return 2;
			}

			return 0;
		}

		private int Command_EnableMod(string profileName, string fileName) {
			return ChangeModInstallInProfile(profileName, fileName, true);
		}

		private int Command_DisableMod(string profileName, string fileName) {
			return ChangeModInstallInProfile(profileName, fileName, false);
		}

		private int ChangeModInstallInProfile(string profileName, string fileName, bool newValue) {
			var profile = FindProfileByName(Profiles, profileName);
			if (profile == null) {
				return 1;
			}

			var path = GetLibraryFolder();
			var relName = Path.GetRelativePath(path, fileName);
			var found = false;
			foreach (var mod in profile.Mods) {
				if (mod.Path == relName) {
					found = true;
					mod.Install = newValue;
					break;
				}
			}

			if (!found) {
				return 2;
			}

			try {
				profile.Save();
			} catch {
				return 3;
			}

			return 0;
		}

		private int Command_InstallMods(string profileName) {
			return RunProfileModsInstallation(profileName, true);
		}

		private int Command_UninstallMods(string profileName) {
			return RunProfileModsInstallation(profileName, false);
		}

		private int RunProfileModsInstallation(string profileName, bool install) {
			var profile = FindProfileByName(Profiles, profileName);
			if (profile == null) {
				return 1;
			}

			var error = false;
			List<ModEntry> modsToInstall = new();

			if (install) {
				try {
					var syncModsLibrary = true;
					if (Settings.PreferCachedModsLibrary && ModsDetectionCached.CacheFileExists()) {
						syncModsLibrary = false;
					}

					if (syncModsLibrary) {
						DetectMods(false);
					} else {
						LoadModsFromCache();
					}
				} catch {
					return 2;
				}

				var builder1 = new ModCollectingThreadBuilder(profile, Mods);
				builder1.OnException = (string s, Exception ex) => {
					error = true;
				};
				builder1.OnSuccess = (List<ModEntry> result) => {
					modsToInstall = result;
				};

				var thread1 = builder1.Build();
				thread1.Start();
				thread1.Join();

				if (error) {
					return 3;
				}
			}

			var builder2 = new ModInstallingThreadBuilder(Settings, profile, modsToInstall, !install);
			builder2.OnErrorOccurred_BeforeWritingTrace = () => {
				error = true;
			};
			builder2.OnErrorOccurred_AfterTraceSaved = () => {
				error = true;
			};

			var thread2 = builder2.Build();
			thread2.Start();
			thread2.Join();

			if (error) {
				return 4;
			}

			return 0;
		}

		private static string GetLibraryFolder() {
			var cwd = Directory.GetCurrentDirectory();
			return Path.Combine(cwd, "Mods Library");
		}

		private static Profile FindProfileByName(List<Profile> profiles, string profileName) {
			foreach (var profile in profiles) {
				if (profile.Name == profileName)
					return profile;
			}

			return null;
		}

		#endregion

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

		private void DetectMods(bool showWarnings = true) {
			var cwd = Directory.GetCurrentDirectory();
			var path = Path.Combine(cwd, "Mods Library");
			var warnings = new List<string>();

			_detection = Settings.CacheModsLibrary ? new ModsDetectionCached() : new ModsDetection();
			_detection.Detect(path, Mods, warnings);
			_detection = null;

			if (showWarnings)
				ShowModsDetectionWarnings(warnings);
		}

		private void DetectModsInFiles(List<string> filenames) {
			var cwd = Directory.GetCurrentDirectory();
			var path = Path.Combine(cwd, "Mods Library");
			var warnings = new List<string>();

			_detection = Settings.CacheModsLibrary ? new ModsDetectionCached() : new ModsDetection();
			_detection.DetectInFiles(path, filenames, Mods, warnings);
			_detection = null;

			ShowModsDetectionWarnings(warnings);
		}

		private void ShowModsDetectionWarnings(List<string> warnings) {
			if (warnings.Count == 0) return;

			if (warnings.Count == 1) {
				MessageBox.Show(warnings[0], "Warning", MessageBoxButton.OK);
				return;
			}

			var message = $"During scan, {warnings.Count} warnings happened:\n";
			foreach (var warning in warnings) {
				message += $"\n* {warning}\n";
			}
			MessageBox.Show(message, "Warnings", MessageBoxButton.OK);
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
			Thread detectionThread = new(() => DetectMods(true));
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
