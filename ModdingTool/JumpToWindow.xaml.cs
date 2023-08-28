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
