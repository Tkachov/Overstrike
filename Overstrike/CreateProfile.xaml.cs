// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Microsoft.WindowsAPICodePack.Dialogs;
using Overstrike.Data;
using Overstrike.Games;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Overstrike {
	public partial class CreateProfile: Window {
		bool Initializing = true;

		// profile name
		string ProfileName = null;
		bool IsNameValid = false;
		bool IsNameTaken = false;

		// game path
		string GamePath = null;
		string DetectedGame = null;

		public CreateProfile() {
			InitializeComponent();
			Initializing = false;
		}

		private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			if (Initializing) return;

			ProfileName = NameTextBox.Text;
			IsNameValid = Regex.IsMatch(ProfileName, "^[A-Za-z0-9 _-]+$");
			IsNameTaken = File.Exists(Path.Combine("Profiles/", ProfileName + ".json"));

			UpdateErrorAndButton();
		}

		private void BrowseButton_Click(object sender, RoutedEventArgs e) {
			CommonOpenFileDialog dialog = new CommonOpenFileDialog();
			dialog.Title = "Select game directory...";
			dialog.IsFolderPicker = true;
			dialog.RestoreDirectory = true;

			var result = dialog.ShowDialog();
			this.Activate();

			if (result != CommonFileDialogResult.Ok) {
				return;
			}

			GamePath = dialog.FileName;
			DetectedGame = GameBase.DetectGameInstallation(GamePath);

			if (DetectedGame == null && GamePath.EndsWith("toc", StringComparison.OrdinalIgnoreCase)) {
				GamePath = GamePath.Substring(0, GamePath.Length - 3);
				DetectedGame = GameBase.DetectGameInstallation(GamePath);
			}

			if (DetectedGame == null && GamePath.EndsWith("asset_archive", StringComparison.OrdinalIgnoreCase)) {
				GamePath = GamePath.Substring(0, GamePath.Length - 13);
				DetectedGame = GameBase.DetectGameInstallation(GamePath);
			}

			PathTextBox.Text = GamePath;
			UpdateErrorAndButton();
		}

		private void UpdateErrorAndButton() {
			string message = "";
			bool profileOk = false;
			bool pathOk = false;

			if (GamePath != null) {
				if (DetectedGame == null) {
					message = "Couldn't detect a supported game under specified path!";
				} else {
					pathOk = true;

					message = "Detected game: " + GameBase.GetGame(DetectedGame).UserFriendlyName;
				}
			}

			if (ProfileName != null) {
				if (!IsNameValid) {
					message = "Profile name is not valid!";
				} else if (IsNameTaken) {
					message = "Profile with such name already exists!";
				} else {
					profileOk = true;
				}
			}

			ErrorMessage.Content = message;
			CreateProfileButton.IsEnabled = profileOk && pathOk;
		}

		private void CreateProfileButton_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		internal Profile GetProfile() {
			if (CreateProfileButton.IsEnabled) {
				return new Profile(ProfileName, DetectedGame, GamePath);
			}

			return null;
		}
	}
}
