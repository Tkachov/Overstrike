// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json.Linq;
using System;
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

	#endregion
	#region layout tab

	private string _selectedStyle;
	private List<IconsStyle> _styles = new();
	private List<LayoutEntry> _entries = new();
	private LayoutEntry _buttonsEntry = new AddingEntriesButtonsEntry();

	internal List<LayoutEntry> Entries { get => _entries; }
	internal CompositeCollection OptionPathCollection = new();
	internal CompositeCollection OptionIconCollection = new();

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

		// TODO: drag n drop -- both options of modules and entries
		// TODO: save button to show warnings and produce file

		UpdateEntriesList();
		MakeOptionPathSelector();
	}

	public string ModName => (_modName == null || _modName.Trim() == "" ? "Untitled" : _modName);
	public string SelectedIconsStyle => _selectedStyle;

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
		MakeOptionPathSelector();
	}

	#endregion
	#region layout tab

	private void MakeIconsStyleSelector() {
		_styles.Clear();
		_styles.Add(new IconsStyle() { Name = "No icons", Id = "none" });
		_styles.Add(new IconsStyle() { Name = "32x32 icons", Id = "small" });

		IconsStyleComboBox.ItemsSource = new CompositeCollection {
			new CollectionContainer() { Collection = _styles }
		};

		var selected = _styles[0];
		_selectedStyle = selected.Id;
		IconsStyleComboBox.SelectedItem = selected;
	}

	private void UpdateEntriesList() {
		var i = _entries.IndexOf(_buttonsEntry);
		if (i == -1) {
			_entries.Add(_buttonsEntry);
		} else if (i != _entries.Count - 1) {
			_entries.RemoveAt(i);
			_entries.Add(_buttonsEntry);
		}

		LayoutEntriesList.ItemsSource = new CompositeCollection {
			new CollectionContainer() { Collection = _entries }
		};
	}

	private void MakeOptionPathSelector() {
		var paths = new List<ModulePath> {
			new() { Name = "(no file)", Path = "" }
		};
		paths.AddRange(_modules);

		OptionPathCollection = new CompositeCollection {
			new CollectionContainer() { Collection = paths }
		};

		var icons = new List<IconPath> {
			new() { Name = "(no icon)", Path = "", Icon = null }
		};
		icons.AddRange(_icons);

		OptionIconCollection = new CompositeCollection {
			new CollectionContainer() { Collection = icons }
		};

		UpdateEntriesList();
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
		Focus();
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

	private void AddingEntriesButtonsEntry_AddHeader_Click(object sender, RoutedEventArgs e) {
		AddEntry(new HeaderEntry() { Text = "" });
	}

	private void AddingEntriesButtonsEntry_AddModule_Click(object sender, RoutedEventArgs e) {
		AddEntry(new ModuleEntry() { Name = "" });
	}

	private void AddingEntriesButtonsEntry_AddSeparator_Click(object sender, RoutedEventArgs e) {
		AddEntry(new SeparatorEntry());		
	}

	private void AddEntry(LayoutEntry entry) {
		_entries.Add(entry);
		UpdateEntriesList();

		// additionally, scroll to the bottom & select the latest added entry
		LayoutEntriesList.ScrollIntoView(_buttonsEntry);
		LayoutEntriesList.SelectedItem = entry;
	}

	private void Module_AddOptionButton_Click(object sender, RoutedEventArgs e) {
		var button = (Button)sender;
		var module = (ModuleEntry)button?.DataContext;
		module?.Options.Add(new ModuleOption() { Window = this });
		module?.UpdateOptions();

		UpdateEntriesList();
	}

	private void ModulesOrOptionsList_KeyUp(object sender, KeyEventArgs e) {
		if (e.Key == Key.Delete) {
			if (sender is ListBox listBox) {
				var focusedElement = FocusManager.GetFocusedElement(this);
				if (focusedElement is ListBoxItem listBoxItem) {
					if (listBoxItem.IsDescendantOf(listBox)) {
						// Delete pressed when focus in on list item, not some control inside of that item
						if (listBoxItem.DataContext is ModuleOption option) {
							if (listBox.DataContext is ModuleEntry module) {
								module.Options.Remove(option);
								module.UpdateOptions();
								UpdateEntriesList();
							}
						} else if (listBoxItem.DataContext is LayoutEntry entry) {
							_entries.Remove(entry);
							UpdateEntriesList();
						}

						// prevent bubbling so the outer list box doesn't handle Delete after inner did
						e.Handled = true;
						return;
					}
				}
			}
		}
	}

	private void IconsStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (_initializing) return;
		_selectedStyle = ((IconsStyle)IconsStyleComboBox.SelectedItem).Id;
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

	private void LoadInfoJsonButton_Click(object sender, RoutedEventArgs e) {
		var dialog = new CommonOpenFileDialog();
		dialog.Title = "Select 'info.json' to load...";
		dialog.RestoreDirectory = true;

		dialog.Filters.Add(new CommonFileDialogFilter("All supported files", "*.json") { ShowExtensions = true });
		dialog.Filters.Add(new CommonFileDialogFilter("All files", "*") { ShowExtensions = true });

		if (dialog.ShowDialog() != CommonFileDialogResult.Ok) {
			return;
		}

		try {
			var filename = dialog.FileName;
			var jsonDir = Path.GetDirectoryName(filename);
			var json = JObject.Parse(File.ReadAllText(filename));

			// general fields

			var gameId = (string)json["game"];
			var modName = (string)json["name"];
			var author = (string)json["author"];

			NameTextBox.Text = modName;
			AuthorTextBox.Text = author;
			foreach (var game in _games) {
				if (game.Id == gameId) {
					GameComboBox.SelectedItem = game;
					break;
				}
			}

			var iconsStyle = (string)json["icons_style"];
			foreach (var style in _styles) {
				if (style.Id == iconsStyle) {
					IconsStyleComboBox.SelectedItem = style;
					break;
				}
			}

			// files & layout

			_entries.Clear();
			_modules.Clear();
			_icons.Clear();

			var layout = json["layout"];
			foreach (var entry in layout) {
				var entryType = (string)entry[0];
				if (entryType == "header") {
					_entries.Add(new HeaderEntry() { Text = (string)entry[1] });
				} else if (entryType == "separator") {
					_entries.Add(new SeparatorEntry());
				} else if (entryType == "module") {
					var options = (JArray)entry[2];
					if (options.Count > 0) {
						var moduleEntry = new ModuleEntry() { Name = (string)entry[1] };

						foreach (var option in options) {
							var icon = (string)option[0];
							var optionName = (string)option[1];
							var optionPath = (string)option[2];

							if (icon != "")
								AddFile(Path.Combine(jsonDir, icon));

							if (optionPath != "")
								AddFile(Path.Combine(jsonDir, optionPath));

							moduleEntry.Options.Add(new ModuleOption() {
								Window = this,
								_path = Path.Combine(jsonDir, optionPath),
								_iconPath = Path.Combine(jsonDir, icon),
								Name = optionName
							});
						}

						moduleEntry.UpdateOptions();
						_entries.Add(moduleEntry);
					}
				}
			}

			UpdateFileLists();
			UpdateEntriesList();
		} catch (Exception) {}

		Focus();
	}

	private void SaveModularButton_Click(object sender, RoutedEventArgs e) {
		// TODO: dialog (modular)
		// TODO: save file with stuff set in all the tabs
	}

	#endregion

	#endregion
}
