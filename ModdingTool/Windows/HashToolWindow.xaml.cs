// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Windows;
using System.Windows.Input;

namespace ModdingTool;

public partial class HashToolWindow: Window {
	protected bool _normalizeInput = true;
	public bool NormalizeInput {
		get => _normalizeInput;
		set {
			_normalizeInput = value;
			Hash();
		}
	}

	public HashToolWindow() {
		InitializeComponent();

		InputTextBox.Text = "";
		Hash();

		DataContext = this;
	}

	private void InputTextBox_KeyUp(object sender, KeyEventArgs e) {
		Hash();
	}

	private void Hash() {
		var input = InputTextBox.Text;
		var crc32 = DAT1.CRC32.Hash(input, NormalizeInput);
		var crc64 = DAT1.CRC64.Hash(input, NormalizeInput);
		Crc32TextBox.Text = $"{crc32:X08}";
		Crc64TextBox.Text = $"{crc64:X016}";
	}
}
