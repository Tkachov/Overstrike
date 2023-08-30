// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Windows;
using System.Windows.Input;

namespace ModdingTool;

public partial class JumpToWindow: Window {
	public bool Jumped = false;
	public string Path = null;

	public JumpToWindow() {
		InitializeComponent();
	}

	private void PathTextBox_KeyUp(object sender, KeyEventArgs e) {
		if (e.Key == Key.Enter) {
			Jump();
		}
	}

	private void JumpButton_Click(object sender, RoutedEventArgs e) {
		Jump();
	}

	private void Jump() {
		Jumped = true;
		Path = PathTextBox.Text;
		Close();
	}
}
