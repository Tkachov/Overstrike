// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ModdingTool;

public partial class ModularCreationWindow: Window {
	#region state

	private bool _initializing = true;

	#region files tab
	
	private List<ModulePath> _modules = new();
	private List<IconPath> _icons = new();

	class ModulePath {
		public string Name { get; set; }
		public string Path;
	}

	class IconPath {
		public string Name { get; set; }
		public string Path;
		public BitmapSource Icon { get; set; }
	}

	#endregion
	#region layout tab

	private string _selectedStyle;
	private List<IconsStyle> _styles = new();

	class IconsStyle {
		public string Name { get; set; }
		public string Id;
	}

	#endregion
	#region info tab

	private string _modName;
	private string _author;
	private string _gameId;
	private List<Game> _games = new();

	class Game {
		public string Name { get; set; }
		public string Id;
	}

	#endregion
	
	#endregion

	public ModularCreationWindow() {
		InitializeComponent();
		_initializing = false;
		DataContext = this;

		MakeFileLists();
		MakeIconsStyleSelector();
		MakeGamesSelector();
	}

	#region applying state

	#region files tab

	private void MakeFileLists() {
		ModulesList.ItemsSource = new CompositeCollection {
			new CollectionContainer() { Collection = _modules }
		};

		IconsList.ItemsSource = new CompositeCollection {
			new CollectionContainer() { Collection = _icons }
		};
	}

	private void AddFile(string filename) {
		var extension = Path.GetExtension(filename).ToLower();
		if (extension == ".png") {
			AddIcon(filename);
			return;
		}
		
		if (extension == ".suit" || extension == ".stage") {
			AddModule(filename);
		}
	}

	private void AddIcon(string filename) {
		// don't add the file if it's present already
		foreach (var icon in _icons) {
			if (icon.Path == filename) {
				return;
			}
		}

		BitmapSource png;
		try {
			png = LoadPng(File.ReadAllBytes(filename));
		} catch {
			return; // bad .png => don't add to the list
		}

		var basename = Path.GetFileName(filename);
		_icons.Add(new IconPath() { Name = basename, Path = filename, Icon = png });
	}

	// copied from Overstrike.Utils.Imaging
	// TODO: move to some common assembly
	private static BitmapImage LoadPng(byte[] bytes) {
		using var memoryStream = new MemoryStream();
		memoryStream.Write(bytes, 0, bytes.Length);
		memoryStream.Position = 0;

		return LoadPng(memoryStream);
	}

	private static BitmapImage LoadPng(Stream stream) {
		var bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.StreamSource = stream;
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.EndInit();
		return bitmapImage;
	}

	private void AddModule(string filename) {
		// don't add the file if it's present already
		foreach (var module in _modules) {
			if (module.Path == filename) {
				return;
			}
		}

		var basename = Path.GetFileName(filename);
		_modules.Add(new ModulePath() { Name = basename, Path = filename });
	}

	private void UpdateFileLists() {
		MakeFileLists();
	}

	#endregion
	#region layout tab

	private void MakeIconsStyleSelector() {
		_styles.Clear();
		_styles.Add(new IconsStyle() { Name = "No icons", Id = "none" });

		IconsStyleComboBox.ItemsSource = new CompositeCollection {
			new CollectionContainer() { Collection = _styles }
		};

		var selected = _styles[0];
		_selectedStyle = selected.Id;
		IconsStyleComboBox.SelectedItem = selected;
	}

	#endregion
	#region info tab

	private void MakeGamesSelector() {
		// should be in sync with Overstrike.Games
		_games.Clear();
		_games.Add(new Game() { Name = "Marvel's Spider-Man Remastered", Id = "MSMR" });
		_games.Add(new Game() { Name = "Marvel's Spider-Man: Miles Morales", Id = "MM" });
		_games.Add(new Game() { Name = "Ratchet & Clank: Rift Apart", Id = "RCRA" });
		_games.Add(new Game() { Name = "i30", Id = "i30" });
		_games.Add(new Game() { Name = "i33", Id = "i33" });

		GameComboBox.ItemsSource = new CompositeCollection {
			new CollectionContainer() { Collection = _games }
		};

		var selected = _games[0];
		_gameId = selected.Id;
		GameComboBox.SelectedItem = selected;
	}

	private void RefreshButton() {
		static bool isEmpty(string s) { return (s == null || s == ""); }

		SaveModularButton.IsEnabled = (!isEmpty(_modName) && !isEmpty(_author));
	}

	#endregion
	
	#endregion
	#region event handlers

	private void Window_Drop(object sender, DragEventArgs e) {
		var filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
		if (filenames != null) {
			foreach (string filename in filenames) {
				AddFile(filename);
			}

			UpdateFileLists();
		}
	}

	private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		// TODO: if preview tab is open, rebuild it
		// TODO: maybe make a button instead in layout tab, so it opens a new preview window with the same resize logic as in Overstrike?

		var isFilesTab = (Tabs.SelectedIndex == 0);
		AllowDrop = isFilesTab;
	}

	#region files tab

	private void AddFilesButton_Click(object sender, RoutedEventArgs e) {
		var dialog = new CommonOpenFileDialog();
		dialog.Title = "Select files to add...";
		dialog.Multiselect = true;
		dialog.RestoreDirectory = true;

		dialog.Filters.Add(new CommonFileDialogFilter("All supported files", "*.suit;*.stage;*.png") { ShowExtensions = true });
		dialog.Filters.Add(new CommonFileDialogFilter("All supported module files", "*.suit;*.stage") { ShowExtensions = true });
		dialog.Filters.Add(new CommonFileDialogFilter("All supported icon files", "*.png") { ShowExtensions = true });
		dialog.Filters.Add(new CommonFileDialogFilter("All files", "*") { ShowExtensions = true });

		if (dialog.ShowDialog() != CommonFileDialogResult.Ok) {
			return;
		}

		foreach (var filename in dialog.FileNames) {
			AddFile(filename);
		}

		UpdateFileLists();
	}

	#endregion
	#region info tab

	private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e) {
		if (_initializing) return;
		_modName = NameTextBox.Text;
		RefreshButton();
	}

	private void AuthorTextBox_TextChanged(object sender, TextChangedEventArgs e) {
		if (_initializing) return;
		_author = AuthorTextBox.Text;
		RefreshButton();
	}

	private void GameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (_initializing) return;
		_gameId = ((Game)GameComboBox.SelectedItem).Id;
	}

	private void SaveModularButton_Click(object sender, RoutedEventArgs e) {
		// TODO: dialog (modular)
		// TODO: save file with stuff set in all the tabs
	}

	#endregion

	#endregion
}
