using MS.WindowsAPICodePack.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Shapes;

namespace Overstrike {
	public partial class App: Application {
		AppSettings Settings = new AppSettings();
		List<Profile> Profiles = new List<Profile>();
		List<ModEntry> Mods = new List<ModEntry>();

		protected override void OnStartup(StartupEventArgs e) {
			CreateSubdirectories();
			ReadSettings();
			LoadProfiles();
			DetectMods();

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
			Mods.Clear();

			var cwd = Directory.GetCurrentDirectory();
			var path = System.IO.Path.Combine(cwd, "Mods Library");

			DetectSMPCMods(path);
			// TODO: detect .suits
			// TODO: detect zip/rar/7z archives & find those in there
		}

		private void DetectSMPCMods(string path) {
			string[] files = Directory.GetFiles(path, "*.smpcmod", SearchOption.AllDirectories);
			foreach (string file in files) {
				DetectSMPCMod(file, path);
			}

			files = Directory.GetFiles(path, "*.mmpcmod", SearchOption.AllDirectories);
			foreach (string file in files) {
				DetectSMPCMod(file, path);
			}
		}

		private void DetectSMPCMod(string file, string basepath) {
			try {
				bool hasFiles = false;
				bool hasInfo = false;

				string modName = null;
				string author = null;

				using (ZipArchive zip = ZipFile.Open(file, ZipArchiveMode.Read)) {
					foreach (ZipArchiveEntry entry in zip.Entries) {
						if (entry.FullName.Equals("SMPCMod.info", StringComparison.OrdinalIgnoreCase)) {
							using (var stream = entry.Open()) {
								using (StreamReader reader = new StreamReader(stream)) {
									var str = reader.ReadToEnd();
									var lines = str.Split("\n");
									foreach (var line in lines) {
										var sep = line.IndexOf("=");
										if (sep != -1) {
											var key = line.Substring(0, sep);
											var value = line.Substring(sep + 1);
											if (key.Equals("Title", StringComparison.OrdinalIgnoreCase)) {
												modName = value;
											} else if (key.Equals("Author", StringComparison.OrdinalIgnoreCase)) {
												author = value;
											}
										}
									}
								}
							}

							hasInfo = true;
						} else if (entry.FullName.StartsWith("ModFiles/", StringComparison.OrdinalIgnoreCase)) {
							hasFiles = true;
						}
					}
				}

				if (!hasFiles || !hasInfo) return;

				var name = System.IO.Path.GetFileName(file);
				if (modName != null && modName.Trim() != "") {
					name = modName.Trim();
					if (author != null && author.Trim() != "") {
						name += " by " + author.Trim();
					}
				}

				Mods.Add(new ModEntry(name, GetRelativePath(file, basepath), file.EndsWith(".smpcmod", StringComparison.OrdinalIgnoreCase) ? ModEntry.ModType.SMPC : ModEntry.ModType.MMPC));
			} catch (Exception) {}
		}

		private string GetRelativePath(string file, string basepath) {
			Debug.Assert(file.StartsWith(basepath, StringComparison.OrdinalIgnoreCase));
			var result = file.Substring(basepath.Length);
			if (result.Length > 0) {
				if (result[0] == '/' || result[0] == '\\')
					result = result.Substring(1);
			}
			return result;
		}
	}
}
