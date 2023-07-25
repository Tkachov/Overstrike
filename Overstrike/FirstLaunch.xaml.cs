// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Overstrike {
	public partial class FirstLaunch : Window
    {
		public bool CreateAllProfiles = true;
		private ObservableCollection<FirstLaunchProfile> _profilesList = new ObservableCollection<FirstLaunchProfile>();

		public FirstLaunch()
        {
            InitializeComponent();

			DetectGameInstallations();
			RefreshUI();
		}

		private void DetectGameInstallations() {
			TryDetectGameAssetArchiveDirFile();

			TryDetectGameAllDisks("MSMR (Steam)", ":\\Program Files (x86)\\Steam\\steamapps\\common\\Marvel's Spider-Man Remastered");
			TryDetectGameAllDisks("MSMR (Steam)", ":\\Program Files\\Steam\\steamapps\\common\\Marvel's Spider-Man Remastered");
			TryDetectGameAllDisks("MSMR (Steam)", ":\\SteamLibrary\\steamapps\\common\\Marvel's Spider-Man Remastered");
			TryDetectGameAllDisks("MSMR (EGS)", ":\\Program Files (x86)\\Epic Games\\Marvel's Spider-Man Remastered");
			TryDetectGameAllDisks("MSMR (EGS)", ":\\Program Files\\Epic Games\\Marvel's Spider-Man Remastered");

			TryDetectGameAllDisks("MM (Steam)", ":\\Program Files (x86)\\Steam\\steamapps\\common\\Marvel's Spider-Man Miles Morales");
			TryDetectGameAllDisks("MM (Steam)", ":\\Program Files\\Steam\\steamapps\\common\\Marvel's Spider-Man Miles Morales");
			TryDetectGameAllDisks("MM (Steam)", ":\\SteamLibrary\\steamapps\\common\\Marvel's Spider-Man Miles Morales");
			TryDetectGameAllDisks("MM (EGS)", ":\\Program Files (x86)\\Epic Games\\Marvel's Spider-Man Miles Morales");
			TryDetectGameAllDisks("MM (EGS)", ":\\Program Files\\Epic Games\\Marvel's Spider-Man Miles Morales");
			TryDetectGameAllDisks("MM (EGS)", ":\\Program Files (x86)\\Epic Games\\MarvelsMilesMorales");
			TryDetectGameAllDisks("MM (EGS)", ":\\Program Files\\Epic Games\\MarvelsMilesMorales");

			if (_profilesList.Count > 0) {
				Hint.Text = _profilesList.Count + " supported game" + (_profilesList.Count > 1 ? "s" : "") + " found in usual places. Add more manually, if you want:";
			}
		}

		private bool SamePath(string a, string b) {
			return String.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase);
		}

		private bool AlreadyAdded(string path) {
			foreach (var profile in _profilesList)
				if (SamePath(profile.GamePath, path))
					return true;
			return false;
		}

		private void TryDetectGameAssetArchiveDirFile() {
			try {
				var path = File.ReadAllLines("assetArchiveDir.txt")[0];
				path = path.Substring(0, path.Length - 13); // drop "asset_archive"

				if (AlreadyAdded(path)) return;

				var game = CreateProfile.DetectGame(path);
				if (game != null) {
					AddToProfilesList(new Profile(game, game, path));
				}
			} catch {}
		}

		private void TryDetectGameAllDisks(string name, string path) {
			for (var d = 'A'; d <= 'Z'; ++d) {
				TryDetectGame(name, d + path);
			}
		}

		private void TryDetectGame(string name, string path) {
			if (AlreadyAdded(path)) return;

			var game = CreateProfile.DetectGame(path);
			if (game != null) {
				AddToProfilesList(new Profile(name, game, path));
			}
		}

		private void RefreshUI() {
			if (CreateAllProfilesCheckbox != null)
				CreateAllProfilesCheckbox.IsChecked = CreateAllProfiles;

			if (ProfilesList != null) {
				foreach (var profile in _profilesList) {
					profile.PropertyChanged += OnProfilePropertyChanged;
				}

				ProfilesList.ItemsSource = new CompositeCollection {
					new CollectionContainer() { Collection = _profilesList }
				};
			}

			if (CreateProfilesButton != null) {
				var profilesToCreate = 0;
				foreach (var profile in _profilesList) {
					if (profile.Create)
						++profilesToCreate;
				}
				CreateProfilesButton.IsEnabled = (profilesToCreate > 0);
			}
		}

		private void OnProfilePropertyChanged(object? sender, PropertyChangedEventArgs e) {
			RefreshUI();
		}

		private bool _handlingChecked = false;

		private void CreateAllProfilesCheckbox_Changed(object sender, RoutedEventArgs e) {
			if (_handlingChecked) return;

			_handlingChecked = true;

			CreateAllProfiles = ((CheckBox)sender).IsChecked == true;
			foreach (var profile in _profilesList) {
				profile.Create = CreateAllProfiles;
			}
			RefreshUI();

			_handlingChecked = false;
		}

		private void AddGameButton_Click(object sender, RoutedEventArgs e) {
			var window = new CreateProfile();
			window.ShowDialog();

            var p = window.GetProfile();
            if (p != null) {
				AddToProfilesList(p);
				RefreshUI();
            }
		}

		private void AddToProfilesList(Profile p) {
			var index = 1;
			var name = p.Name;
			while (NameIsTaken(p)) {
				p.Name = name + " (" + index + ")";
				++index;
			}

			_profilesList.Add(new FirstLaunchProfile(p));
		}

		private bool NameIsTaken(Profile p) {
			if (p == null) return false;

			var profileExists = File.Exists(Path.Combine("Profiles/", p.Name + ".json"));
			if (profileExists) {
				return true;
			}

			foreach (var profile in _profilesList) {
				if (p.Name == profile.Name)
					return true;
			}

			return false;
		}

		private void CreateProfilesButton_Click(object sender, RoutedEventArgs e) {
			if (_profilesList.Count == 0) return;

			foreach (var profile in _profilesList) {
				if (profile.Create) {
					try {
						var p = new Profile(profile.Name, profile.Game, profile.GamePath);
						p.Save();
					} catch {}
				}
			}

			if (Directory.Exists("ModManager\\SMPCMods\\") || Directory.Exists("ModManager\\MMPCMods\\")) {
				MessageBoxResult result = MessageBox.Show("SMPCTool installation found.\nDo you want to copy mods from there?", "SMPCTool migration", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes) {
					CopyMods();
					CopyModsOrder();
				}
			}

			Close();
		}

		private void CopyMods() {
			try {
				var path = "";
				if (Directory.Exists("ModManager\\SMPCMods\\")) path = "ModManager\\SMPCMods\\";
				else path = "ModManager\\MMPCMods\\";

				string[] files = Directory.GetFiles(path, "*.smpcmod", SearchOption.AllDirectories);
				foreach (var file in files) {
					File.Copy(file, Path.Combine("Mods Library/", Path.GetFileName(file)), true);
				}

				files = Directory.GetFiles(path, "*.mmpcmod", SearchOption.AllDirectories);
				foreach (var file in files) {
					File.Copy(file, Path.Combine("Mods Library/", Path.GetFileName(file)), true);
				}

				((App)App.Current).ReloadMods();
			} catch {}
		}

		private void CopyModsOrder() {
			try {
				List<(string, bool)> modsOrder = new();
				foreach (var line in File.ReadAllLines("ModManager\\ModManager.txt")) {
					string[] parts = line.Split(',');
					if (parts.Length == 2) {
						modsOrder.Add((parts[0], parts[1] == "1"));
					}
				}

				foreach (var profile in _profilesList) {
					if (!profile.Create) continue;

					try {
						var path = Path.Combine(Directory.GetCurrentDirectory(), "Profiles/", profile.Name + ".json");
						JObject json = JObject.Parse(File.ReadAllText(path));
						var mods = (JArray)json["mods"];

						foreach (var mod in modsOrder) {
							mods.Add(new JArray {
								mod.Item1,
								mod.Item2
							});
						}

						File.WriteAllText(path, json.ToString());
					} catch {}
				}
			} catch {}
		}
	}

	public class FirstLaunchProfile: INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		private bool _create;
		public bool Create {
			get { return _create; }
			set {
				_create = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Create)));
			}
		}
		public string Name { get; set; }
		public string Game { get; set; }
		public string GamePath { get; set; }

		public FirstLaunchProfile(Profile profile) {
			_create = true;
			Name = profile.Name;
			Game = profile.Game;
			GamePath = profile.GamePath;
		}
	}
}
