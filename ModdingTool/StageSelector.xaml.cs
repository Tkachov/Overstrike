// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ModdingTool;

public partial class StageSelector: Window {
	private ObservableCollection<string> _stages = new();
	private bool _verified = false;

	public string Stage = null;

	public StageSelector() {
		InitializeComponent();

		_stages.Clear();

		var cwd = Directory.GetCurrentDirectory();
		var path = Path.Combine(cwd, "stages");
		if (Directory.Exists(path)) {
			var dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
			foreach (var dir in dirs) {
				_stages.Add(Path.GetRelativePath(path, dir));
			}
		}

		NameComboBox.ItemsSource = _stages;
		NameComboBox.SelectedIndex = 0;
		Verify();
	}

	private void SelectButton_Click(object sender, RoutedEventArgs e) {
		Select();
	}

	private void NameComboBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
		Verify();
	}

	private void NameComboBox_KeyUp(object sender, KeyEventArgs e) {
		if (e.Key == Key.Enter) {
			Select();
		}
	}

	//

	private bool Verify() {
		_verified = false;

		var text = NameComboBox.Text;
		var isValid = Regex.IsMatch(text, "^[A-Za-z0-9 _-]+$");
		var stageExists = _stages.Contains(text);
		_verified = isValid;

		WarningMessage.Text = (isValid ? "" : "Stage name isn't valid!");
		SelectButton.IsEnabled = isValid;
		SelectButton.Content = (stageExists ? "Select" : "Create");

		return _verified;
	}

	private void Select() {
		if (!Verify()) return;

		Stage = NameComboBox.Text;
		Close();
	}
}
