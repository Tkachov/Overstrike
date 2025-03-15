// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Overstrike.Windows {
	public partial class ErrorLogWindow : Window {
		public ErrorLogWindow() {
			InitializeComponent();

			var text = GetLatestErrorLog();
			LogTextBox.Text = text;			
		}

		protected override void OnActivated(EventArgs e) {
			base.OnActivated(e);
			LogTextBox.ScrollToEnd();
		}

		private static string GetLatestErrorLog() {
			try {
				var fullText = File.ReadAllText("errors.log");
				var separator = "--------\nOverstrike";
				var i = fullText.LastIndexOf(separator);
				i = fullText.IndexOf("\n", i) + 1;
				return fullText[i..];
			} catch {}

			return "Failed to read latest error message!";
		}

		public void CopyErrorText() {
			OverstrikeShared.Utils.Clipboard.SetClipboard(LogTextBox.Text);
		}

		private void Window_KeyUp(object sender, KeyEventArgs e) {
			if (e.Key == Key.PrintScreen || e.SystemKey == Key.PrintScreen) {
				CopyErrorText();
			}
		}

		private void CopyButton_Click(object sender, RoutedEventArgs e) {
			CopyErrorText();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e) {
			Close();
		}
	}
}
