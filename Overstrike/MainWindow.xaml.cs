using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Overstrike {
	public partial class MainWindow: Window {
		private AppSettings _settings;
		private List<Profile> _profiles;

		public MainWindow(AppSettings settings, List<Profile> profiles) {
			InitializeComponent();

			_settings = settings;
			_profiles = profiles;

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
			ProfileGamePath.Content = profile.GamePath;
		}
	}
}
