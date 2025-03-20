// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Games;
using System.IO;
using System.Windows;

namespace Overstrike.Windows {
	public partial class TocMismatchDialog : Window {
		private string _tocPath;
		private string _tocBakPath;

		internal TocMismatchDialog(string tocPath, string tocSha, string tocBakPath, string backupSha, GameBase game) {
			InitializeComponent();

			_tocPath = tocPath;
			_tocBakPath = tocBakPath;

			var tocDate = File.GetLastWriteTime(tocPath);
			var bakDate = File.GetLastWriteTime(tocBakPath);

			OptionsList.Items.Clear();
			OptionsList.Items.Add(new ButtonData {
				DisplayName = Path.GetFileName(tocPath),
				Message = "Yes, update 'toc.BAK'!",

				UpdateBak = true,
				Hash = tocSha,
				FriendlyName = game.GetTocHashFriendlyName(tocSha),
				ChangeDate = tocDate.ToString(),
				IsNewer = tocDate > bakDate,
			});
			OptionsList.Items.Add(new ButtonData {
				DisplayName = Path.GetFileName(tocBakPath),
				Message = "No, keep the one I had.",

				UpdateBak = false,
				Hash = backupSha,
				FriendlyName = game.GetTocHashFriendlyName(backupSha),
				ChangeDate = bakDate.ToString(),
				IsNewer = tocDate < bakDate,
			});

			OptionsList.SelectedIndex = 0;
			OptionsList.Focus();
		}

		private void Option_Clicked(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			if (OptionsList.SelectedItem is ButtonData item) {
				if (item.UpdateBak) {
					File.Copy(_tocPath, _tocBakPath, true);
				}
			}

			DialogResult = true;
			Close();
		}
	}

	public class ButtonData {
		public string DisplayName { get; set; }
		public string Modified => $"Modified: {ChangeDate}" + (IsNewer ? " (newer)" : "");
		public string SHA => $"SHA-1: {Hash[..7].ToUpper()}" + (string.IsNullOrEmpty(FriendlyName) ? "" : $" ({FriendlyName})");
		public string Message { get; set; }

		public bool UpdateBak;
		public string Hash;
		public string FriendlyName;
		public string ChangeDate;
		public bool IsNewer;
	}
}
