using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Updater {
	public partial class MainWindow: Window {
		private const string EXE_TO_UPDATE = "Overstrike.exe";
		private const string REPO_API_LINK = "https://api.github.com/repos/Tkachov/Overstrike/releases/latest";
		private const string NEXUS_LINK = "https://www.nexusmods.com/marvelsspiderman2/mods/1?tab=files";

		public MainWindow(bool silentMode) {
			InitializeComponent();

			if (!silentMode) {
				CheckForUpdates();
			}
		}

		#region main logic

		private bool _inProgress = false;

		private Version _localVersion = null;
		private string _localName = "";

		private Version _remoteVersion = null;
		private string _remoteName = "";
		private string _patchNotes = "";

		public async Task<bool> CheckForUpdates() {
			if (_inProgress) return false;

			_inProgress = true;

			SetLocalVersionText("Loading...", true, false);
			SetRemoteVersionText("Loading...", true, false);
			ShowLayout(Layout.None);

			GetLocalVersion();
			if (_localVersion != null) {
				SetLocalVersionText(_localName, false, false);
			} else {
				SetLocalVersionText("Failed to determine", false, true);
			}

			await GetRemoteVersion();
			if (_remoteVersion != null) {
				SetRemoteVersionText(_remoteName, false, false);
			} else {
				SetRemoteVersionText("Failed to determine", false, true);
			}

			var failedToDetermine = false;
			var updateAvailable = false;

			if (_localVersion == null) {
				failedToDetermine = true;

				if (_remoteVersion != null) {
					// no local, but has remote, so suggest newer version
					updateAvailable = true;
				}
			} else {
				if (_remoteVersion == null) {
					// failed to determine remote, can't compare
					failedToDetermine = true;
				} else {
					// both versions are there, we can actually compare
					if (_remoteVersion.IsNewerThan(_localVersion)) {
						updateAvailable = true;
					}
				}
			}

			if (updateAvailable) {
				// show even if failed to determine version
				PatchNotesTextBox.Text = _patchNotes;
				ShowLayout(Layout.HasUpdates);
			} else if (failedToDetermine) {
				ShowLayout(Layout.Failed);
			} else {
				ShowLayout(Layout.NoUpdates);
			}

			_inProgress = false;

			// but return true only if didn't fail to determine version
			return (!failedToDetermine && updateAvailable);
		}

		private void GetLocalVersion() {
			try {
				var info = FileVersionInfo.GetVersionInfo(EXE_TO_UPDATE);
				var version = ToCanonVersion(info.FileVersion);
				_localName = $"{info.ProductName} (v. {version})";
				_localVersion = new Version(version);
			} catch {
				_localName = "";
				_localVersion = null;
			}
		}

		private async Task GetRemoteVersion() {
			try {
				var info = await GetLatestInfo();
				_remoteName = info.name;
				_patchNotes = info.body;
				_remoteVersion = new Version(GetVersionFromTag((string)info.tag_name));
			} catch {
				_remoteName = "";
				_patchNotes = "";
				_remoteVersion = null;
			}
		}

		private static async Task<dynamic> GetLatestInfo() {
			using var client = new HttpClient();
			SetRandomUserAgent(client);

			var response = await client.GetStringAsync(REPO_API_LINK);
			return Newtonsoft.Json.JsonConvert.DeserializeObject(response);
		}

		#endregion

		#region event handlers

		private void OpenNexusButtonClicked(object sender, RoutedEventArgs e) {
			try {
				Process.Start(new ProcessStartInfo() {
					FileName = NEXUS_LINK,
					UseShellExecute = true
				});
			} catch {}
		}

		private void CloseButtonClicked(object sender, RoutedEventArgs e) {
			Close();
		}

		private void RetryButtonClicked(object sender, RoutedEventArgs e) {
			CheckForUpdates();
		}

		#endregion

		#region ui

		private enum Layout {
			None,
			HasUpdates,
			NoUpdates,
			Failed
		};

		private void ShowLayout(Layout layout) {
			try {
				UpdateAvailable.Visibility = (layout == Layout.HasUpdates ? Visibility.Visible : Visibility.Collapsed);
				NoUpdates.Visibility = (layout == Layout.NoUpdates ? Visibility.Visible : Visibility.Collapsed);
				Failed.Visibility = (layout == Layout.Failed ? Visibility.Visible : Visibility.Collapsed);

				MinHeight = (layout == Layout.HasUpdates ? 250 : 130);
				MaxHeight = (layout == Layout.HasUpdates ? double.PositiveInfinity : 130);
				Height = Math.Min(Math.Max(MinHeight, Height), MaxHeight);
			} catch {}
		}

		private void SetLocalVersionText(string text, bool loading, bool error) {
			UpdateVersionLabel(LocalVersionLabel, text, loading, error);
		}

		private void SetRemoteVersionText(string text, bool loading, bool error) {
			UpdateVersionLabel(RemoteVersionLabel, text, loading, error);
		}

		private static readonly Brush ERROR_COLOR = new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0x44));

		private static void UpdateVersionLabel(Label label, string text, bool loading, bool error) {
			try {
				label.Content = text;
				label.Foreground = (loading ? Brushes.Gray : (error ? ERROR_COLOR : Brushes.Black));
			} catch {}
		}

		#endregion

		#region version utils

		private class Version {
			public int major, minor, patch;

			public Version(string version) {
				var parts = version.Split('.');
				major = int.Parse(parts[0]);
				minor = int.Parse(parts[1]);
				patch = int.Parse(parts[2]);
			}

			public bool IsNewerThan(Version another) {
				if (major > another.major) return true;
				if (major < another.major) return false;

				if (minor > another.minor) return true;
				if (minor < another.minor) return false;

				return (patch > another.patch);
			}
		}

		private static string ToCanonVersion(string version) {
			if (version == null) return "0.0.0";

			var dots = 0;
			var index = 0;
			foreach (var c in version) {
				++index;
				if (c == '.') {
					++dots;
					if (dots == 3) {
						return version.Substring(0, index - 1);
					}
				}
			}

			return version;
		}

		private static string GetVersionFromTag(string tag) {
			var index = tag.IndexOf('v');
			if (index >= 0) {
				if (tag.Length > index + 1 && tag[index + 1] == '.') {
					++index;
				}

				return tag.Substring(index + 1);
			}

			return tag;
		}

		#endregion

		#region http client

		private static void SetRandomUserAgent(HttpClient client) {
			var userAgents = new List<string> {
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0",
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0",
			};

			var randomIndex = new Random().Next(userAgents.Count);
			var randomUserAgent = userAgents[randomIndex];
			client.DefaultRequestHeaders.Add("User-Agent", randomUserAgent);
		}

		#endregion
	}
}
