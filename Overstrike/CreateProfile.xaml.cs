using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
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
			DetectedGame = DetectGame(GamePath);

			if (DetectedGame == null && GamePath.EndsWith("toc", StringComparison.OrdinalIgnoreCase)) {
				GamePath = GamePath.Substring(0, GamePath.Length - 3);
				DetectedGame = DetectGame(GamePath);
			}

			if (DetectedGame == null && GamePath.EndsWith("asset_archive", StringComparison.OrdinalIgnoreCase)) {
				GamePath = GamePath.Substring(0, GamePath.Length - 13);
				DetectedGame = DetectGame(GamePath);
			}

			PathTextBox.Text = GamePath;			
			UpdateErrorAndButton();
		}

		internal static string DetectGame(string gamePath) {
			try {
				if (!Directory.Exists(gamePath)) return null;

				if (!Directory.Exists(Path.Combine(gamePath, "asset_archive"))) return null;
				if (!File.Exists(Path.Combine(gamePath, "asset_archive", "toc"))) return null;

				if (File.Exists(Path.Combine(gamePath, "Spider-Man.exe"))) return Profile.GAME_MSMR;
				else if (File.Exists(Path.Combine(gamePath, "MilesMorales.exe"))) return Profile.GAME_MM;
			} catch (Exception) {}

			return null;
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

					message = "Detected game: " + UserFriendlyName(DetectedGame);
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

		private string UserFriendlyName(string name) {
			switch (name) {
				case Profile.GAME_MSMR: return "Marvel's Spider-Man Remastered";
				case Profile.GAME_MM: return "Marvel's Spider-Man: Miles Morales";
			}

			return "?";
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
