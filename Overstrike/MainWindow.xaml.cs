using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace Overstrike {
	public partial class MainWindow: Window {
		private AppSettings _settings;
		private List<Profile> _profiles;
		private Profile _selectedProfile;

		private class ProfileItem {
			public string Text { get; set; }
			public Profile Profile { get; set; }
		}

		private ObservableCollection<ProfileItem> _profilesItems = new ObservableCollection<ProfileItem>();

		public MainWindow(AppSettings settings, List<Profile> profiles) {
			InitializeComponent();

			_settings = settings;
			_profiles = profiles;

			MakeProfileItems();
			FirstSwitchToProfile();			

			

		}

		private void FirstSwitchToProfile() {
			foreach (Profile p in _profiles) {
				if (p.Name == _settings.CurrentProfile) {
					SwitchToProfile(p);
					return;
				}
			}

			SwitchToProfile(_profiles[0]);
		}

		private void SwitchToProfile(Profile profile) {
			_selectedProfile = profile;
			UpdateSelectedProfileItem();

			ProfileGamePath.Content = profile.GamePath;
		}

		private void UpdateSelectedProfileItem() {
			ProfileItem profile = null;
			foreach (var item in _profilesItems) {
				if (item.Profile == _selectedProfile) {
					profile = item;
					break;
				}
			}
			ProfileComboBox.SelectedItem = profile;
		}

		private void MakeProfileItems() {
			_profilesItems.Clear();

			foreach (var profile in _profiles) {
				_profilesItems.Add(new ProfileItem() { Text = profile.Name, Profile = profile });
			}
			_profilesItems.Add(new ProfileItem() { Text = "Add new profile...", Profile = null });
			
			ProfileComboBox.ItemsSource = new CompositeCollection {
				new CollectionContainer() { Collection = _profilesItems }
			};
		}

		private void ProfileComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
			if (e.AddedItems.Count <= 0) return;
			Debug.Assert(ProfileComboBox.SelectedItem != null);

			ProfileItem item = (ProfileItem)e.AddedItems[0];
			if (item.Profile == null) {
				var window = new CreateProfile();
				window.ShowDialog();

				var p = window.GetProfile();
				if (p != null) {
					_profiles = ((App)App.Current).ReloadProfiles();
					Debug.Assert(_profiles.Count > 0);

					MakeProfileItems();

					// try switching to this new profile
					foreach (var profile in _profiles) {
						if (profile.Name == p.Name) {
							SwitchToProfile(profile);
							return;
						}
					}

					// if couldn't find it, try switching back to what was selected before
					foreach (var profile in _profiles) {
						if (profile.Name == _selectedProfile.Name) {
							SwitchToProfile(profile);
							return;
						}
					}

					// last resort: do default profile loading
					FirstSwitchToProfile();
				} else {
					UpdateSelectedProfileItem();
				}

				return;
			}

			if (item.Profile == _selectedProfile) return;
			
			SwitchToProfile(item.Profile);
		}
	}
}
