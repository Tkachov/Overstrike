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
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ModdingTool.Windows;

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
	private List<LayoutEntry> _entries = new();

	class IconsStyle {
		public string Name { get; set; }
		public string Id;
	}

	abstract class LayoutEntry {
		public abstract string Description { get; }
	}

	class HeaderEntry: LayoutEntry {
		public string Text;

		public override string Description {
			get {
				return $"Header: {Text}";
			}
		}
	}

	class SeparatorEntry: LayoutEntry {
		public override string Description { get => "Separator"; }
	}

	class ModuleEntry: LayoutEntry {
		public string Name;
		public List<ModuleOption> Options = new();

		public override string Description {
			get {
				var suffix = "";
				if (Options.Count == 0) {
					suffix = "| WARNING: NO OPTIONS!";
				} else if (Options.Count == 1) {
					suffix = "(internal)";
				} else {
					suffix = $"({Options.Count} options)";
				}

				return $"Module: {Name} {suffix}";
			}
		}
	}

	class ModuleOption {
		public string _name = "";
		public string _path = "";

		public string Name {
			get {
				if (_name == "") return "(no name)";
				return _name;
			}
			set {
				_name = value;
			}
		}

		public string File {
			get {
				if (_path == "") return "(no file)";
				return Path.GetFileName(_path);
			}
			set {
				_path = value;
			}
		}

		public string IconPath;
		public BitmapSource Icon { get; set; }
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
		UpdateSelectedEntryElements();
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

		// update combobox in other tab if needed
		if (OptionsList.SelectedItems.Count > 0) {
			var option = OptionsList.SelectedItems[0];
			if (option is ModuleOption module) {
				MakeOptionPathSelector(module);
			}
		}
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

	private void UpdateEntriesList() {
		LayoutEntriesList.ItemsSource = new CompositeCollection {
			new CollectionContainer() { Collection = _entries }
		};
		UpdateMoveEntryUpDownButtons();
	}

	private void UpdateOptionsList(ModuleEntry selectedEntry) {
		if (selectedEntry == null) {
			OptionsList.ItemsSource = new CompositeCollection {};
			return;
		}

		OptionsList.ItemsSource = new CompositeCollection {
			new CollectionContainer() { Collection = selectedEntry.Options }
		};
	}

	private void UpdateSelectedEntryElements() {
		UpdateMoveEntryUpDownButtons();

		EditingHeader.Visibility = Visibility.Collapsed;
		EditingModule.Visibility = Visibility.Collapsed;		

		if (LayoutEntriesList.SelectedItems.Count == 0) {
			SelectedEntryLabel.Text = "Selected entry: none";
			MoveEntryUpButton.IsEnabled = MoveEntryDownButton.IsEnabled = false;
			return;
		}

		var selectedEntry = LayoutEntriesList.SelectedItems[0];
		if (selectedEntry is HeaderEntry header) {
			SelectedEntryLabel.Text = "Selected entry: header";
			HeaderTextBox.Text = header.Text;
			EditingHeader.Visibility = Visibility.Visible;
		} else if (selectedEntry is SeparatorEntry) {
			SelectedEntryLabel.Text = "Selected entry: separator";
			// nothing to edit
		} else if (selectedEntry is ModuleEntry module) {
			SelectedEntryLabel.Text = "Selected entry: module";
			ModuleNameTextBox.Text = module.Name;
			UpdateOptionsList(module);
			EditingModule.Visibility = Visibility.Visible;
			EditingOption.Visibility = Visibility.Collapsed;
			UpdateSelectedOptionElements();
		}
	}

	private void UpdateMoveEntryUpDownButtons() {
		if (LayoutEntriesList.SelectedItems.Count == 0) {
			MoveEntryUpButton.IsEnabled = MoveEntryDownButton.IsEnabled = false;
			return;
		}

		MoveEntryUpButton.IsEnabled = (LayoutEntriesList.SelectedIndex > 0);
		MoveEntryDownButton.IsEnabled = (LayoutEntriesList.SelectedIndex < _entries.Count - 1);
	}

	private void UpdateSelectedOptionElements() {
		UpdateMoveOptionUpDownButtons();

		if (OptionsList.SelectedItems.Count == 0) {
			EditingOption.Visibility = Visibility.Collapsed;
			return;
		}

		EditingOption.Visibility = Visibility.Visible;

		var option = (ModuleOption)OptionsList.SelectedItems[0];
		// TODO:
		OptionNameTextBox.Text = option._name;
		// selected in combobox 1
		MakeOptionPathSelector(option);

		// TODO: fill comboboxes
		// TODO: react to change and update Options[<index>] of selected module
		// TODO: move up and down buttons for options
	}

	private void MakeOptionPathSelector(ModuleOption option) {
		var paths = new List<ModulePath> {
			new() { Name = "(no file)", Path = "" }
		};
		paths.AddRange(_modules);

		OptionPathComboBox.ItemsSource = new CompositeCollection {
			new CollectionContainer() { Collection = paths }
		};

		var selected = paths[0];
		foreach (var path in paths) {
			if (option._path == path.Path) {
				selected = path;
				break;
			}
		}
		OptionPathComboBox.SelectedItem = selected;
	}

	private void UpdateMoveOptionUpDownButtons() {
		if (OptionsList.SelectedItems.Count == 0) {
			MoveOptionUpButton.IsEnabled = MoveOptionDownButton.IsEnabled = false;
			return;
		}

		MoveOptionUpButton.IsEnabled = (OptionsList.SelectedIndex > 0);
		MoveOptionDownButton.IsEnabled = (OptionsList.SelectedIndex < OptionsList.Items.Count - 1);
	}

	#endregion
	#region info tab

	private void MakeGamesSelector() {
		// should be in sync with Overstrike.Games
		// TODO: have these moved into OverstrikeShared
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

	private void ModulesList_KeyUp(object sender, KeyEventArgs e) {
		if (e.Key == Key.Delete) {
			foreach (var module in ModulesList.SelectedItems) {
				_modules.Remove((ModulePath)module);
			}

			UpdateFileLists();
		}
	}

	private void IconsList_KeyUp(object sender, KeyEventArgs e) {
		if (e.Key == Key.Delete) {
			foreach (var icon in IconsList.SelectedItems) {
				_icons.Remove((IconPath)icon);
			}

			UpdateFileLists();
		}
	}

	#endregion
	#region layout tab

	private void AddHeaderButton_Click(object sender, RoutedEventArgs e) {
		_entries.Add(new HeaderEntry() { Text = "" });
		UpdateEntriesList();
	}

	private void AddModuleButton_Click(object sender, RoutedEventArgs e) {
		_entries.Add(new ModuleEntry() { Name = "" });
		UpdateEntriesList();
	}

	private void AddSeparatorButton_Click(object sender, RoutedEventArgs e) {
		_entries.Add(new SeparatorEntry());
		UpdateEntriesList();
	}

	private void MoveEntryUpButton_Click(object sender, RoutedEventArgs e) {
		if (LayoutEntriesList.SelectedItem == null) return;

		var index = LayoutEntriesList.SelectedIndex;
		if (index < 1) return;

		(_entries[index - 1], _entries[index]) = (_entries[index], _entries[index - 1]);

		UpdateEntriesList();
		LayoutEntriesList.SelectedIndex = index - 1;
		UpdateMoveEntryUpDownButtons();
	}

	private void MoveEntryDownButton_Click(object sender, RoutedEventArgs e) {
		if (LayoutEntriesList.SelectedItem == null) return;

		var index = LayoutEntriesList.SelectedIndex;
		if (index + 1 >= _entries.Count) return;

		(_entries[index + 1], _entries[index]) = (_entries[index], _entries[index + 1]);

		UpdateEntriesList();
		LayoutEntriesList.SelectedIndex = index + 1;
		UpdateMoveEntryUpDownButtons();
	}

	private void LayoutEntriesList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		UpdateSelectedEntryElements();
	}

	private void HeaderTextBox_TextChanged(object sender, TextChangedEventArgs e) {
		if (LayoutEntriesList.SelectedItems.Count == 0) return;

		var selectedEntry = LayoutEntriesList.SelectedItems[0];
		if (selectedEntry is HeaderEntry header) {
			header.Text = HeaderTextBox.Text;
		}

		UpdateEntriesList();
	}

	private void ModuleNameTextBox_TextChanged(object sender, TextChangedEventArgs e) {
		if (LayoutEntriesList.SelectedItems.Count == 0) return;

		var selectedEntry = LayoutEntriesList.SelectedItems[0];
		if (selectedEntry is ModuleEntry module) {
			module.Name = ModuleNameTextBox.Text;
		}

		UpdateEntriesList();
	}

	private void OptionsList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		UpdateSelectedOptionElements();
	}

	private void AddOptionButton_Click(object sender, RoutedEventArgs e) {
		var selectedEntry = LayoutEntriesList.SelectedItems[0];
		if (selectedEntry is ModuleEntry module) {
			module.Options.Add(new ModuleOption());
			UpdateOptionsList(module);
			UpdateEntriesList();
		}
	}

	private void OptionNameTextBox_TextChanged(object sender, TextChangedEventArgs e) {
		// take selected entry from the left
		if (LayoutEntriesList.SelectedItems.Count == 0) return;

		var selectedEntry = LayoutEntriesList.SelectedItems[0];

		// and selected option from the right
		if (OptionsList.SelectedItems.Count == 0) return;

		var option = (ModuleOption)OptionsList.SelectedItems[0];
		option.Name = OptionNameTextBox.Text;

		if (selectedEntry is ModuleEntry module) {
			//module.Options[OptionsList.SelectedIndex].File = modulePath.Path;
			UpdateOptionsList(module);
		}
	}

	private void OptionPathComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (OptionPathComboBox.SelectedItem == null) return;

		var modulePath = (ModulePath)OptionPathComboBox.SelectedItem;

		// take selected entry from the left
		if (LayoutEntriesList.SelectedItems.Count == 0) return;

		var selectedEntry = LayoutEntriesList.SelectedItems[0];

		// and selected option from the right
		if (OptionsList.SelectedItems.Count == 0) return;

		var option = (ModuleOption)OptionsList.SelectedItems[0];
		option.File = modulePath.Path;

		if (selectedEntry is ModuleEntry module) {
			//module.Options[OptionsList.SelectedIndex].File = modulePath.Path;
			UpdateOptionsList(module);
		}
	}

	private void OpenPreviewButton_Click(object sender, RoutedEventArgs e) {
		new ModularWizardPreview(this).ShowDialog();
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
