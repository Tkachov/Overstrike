// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
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

	private Point _dragStartPosition;
	private Point _dragCurrentPosition;
	private static string DND_DATA_FORMAT = "ModularCreationWindowDragAndDropDataFormat";

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
			png = OverstrikeShared.Utils.Imaging.LoadImage(File.ReadAllBytes(filename));
		} catch {
			return; // bad icon => don't add to the list
		}

		var basename = Path.GetFileName(filename);
		_icons.Add(new IconPath() { Name = basename, Path = filename, Icon = png });
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
				// TODO: check if directory and recursively add all files from it?
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

	#region Drag and Drop

	// same handlers for LayoutEntriesList and options listboxes

	private void ListBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
		_dragStartPosition = e.GetPosition(null);
	}

	private void ListBox_MouseMove(object sender, MouseEventArgs e) {
		if (e.LeftButton == MouseButtonState.Pressed) {
			_dragCurrentPosition = e.GetPosition(null);

			if (Math.Abs(_dragCurrentPosition.X - _dragStartPosition.X) > SystemParameters.MinimumHorizontalDragDistance ||
				Math.Abs(_dragCurrentPosition.Y - _dragStartPosition.Y) > SystemParameters.MinimumVerticalDragDistance) {
				BeginDrag(sender, e);
			}
		}
	}

	private void BeginDrag(object sender, MouseEventArgs e) {
		if (sender is not ListBox listBox) {
			return;
		}

		var focusedElement = FocusManager.GetFocusedElement(this);
		if (focusedElement is not ListBoxItem listBoxItem) {
			return;
		}

		if (!listBoxItem.IsDescendantOf(listBox)) {
			return;
		}

		//

		var draggedItem = listBox.ItemContainerGenerator.ItemFromContainer(listBoxItem);

		var data = new DataObject(DND_DATA_FORMAT, listBoxItem);
		DragDrop.DoDragDrop(listBox, data, DragDropEffects.Move);

		listBox.SelectedItem = draggedItem;
	}

	private void ListBox_DragEnter(object sender, DragEventArgs e) {
		if (!e.Data.GetDataPresent(DND_DATA_FORMAT) || sender == e.Source) {
			e.Effects = DragDropEffects.None;
		}
	}

	private void ListBox_Drop(object sender, DragEventArgs e) {
		if (sender is not ListBox listBox) {
			return;
		}

		if (!e.Data.GetDataPresent(DND_DATA_FORMAT)) {
			return;
		}

		var dndData = e.Data.GetData(DND_DATA_FORMAT);
		if (dndData is not ListBoxItem listBoxItem) {
			return;
		}

		if (!listBoxItem.IsDescendantOf(listBox)) {
			return;
		}

		//

		var dropAtElement = e.OriginalSource as FrameworkElement;
		if (dropAtElement?.TemplatedParent is not ListBoxItem listBoxItemToDropAt) {
			return;
		}

		if (!listBoxItemToDropAt.IsDescendantOf(listBox)) {
			return;
		}

		// non-generic part
		
		if (listBoxItem.DataContext is LayoutEntry draggedEntry && listBoxItemToDropAt.DataContext is LayoutEntry entryToDropAt) {
			var index = _entries.IndexOf(entryToDropAt);
			_entries.Remove(draggedEntry);
			_entries.Insert(index, draggedEntry);
			UpdateEntriesList();
		}

		if (listBoxItem.DataContext is ModuleOption draggedOption && listBoxItemToDropAt.DataContext is ModuleOption optionToDropAt) {
			if (listBox.DataContext is ModuleEntry module) {
				var index = module.Options.IndexOf(optionToDropAt);
				module.Options.Remove(draggedOption);
				module.Options.Insert(index, draggedOption);
				module.UpdateOptions();
				UpdateEntriesList();
			}
		}
	}

	#endregion Drag and Drop

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
								_path = (optionPath == "" ? optionPath : Path.Combine(jsonDir, optionPath)),
								_iconPath = (icon == "" ? icon : Path.Combine(jsonDir, icon)),
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
		var dialog = new CommonSaveFileDialog();
		dialog.Title = "Save .modular...";
		dialog.RestoreDirectory = true;
		dialog.Filters.Add(new CommonFileDialogFilter("Modular", "*.modular") { ShowExtensions = true });
		dialog.Filters.Add(new CommonFileDialogFilter("All files", "*") { ShowExtensions = true });
		dialog.DefaultFileName = "*.modular";

		if (dialog.ShowDialog() != CommonFileDialogResult.Ok) {
			return;
		}

		// TODO: do this in a separate thread so app doesn't hang?
		// TODO: maybe provide an option to save just info.json (with non-generated filenames? so have to assume all files are in subdirectories relative to it) without packing .modular?

		// save file with stuff set in all the tabs
		var filename = dialog.FileName;
		var log = "";

		try {
			log += $"Saving into \"{filename}\"...\n\n";

			// some checks & warnings + filenames generation
			var headerIndex = -1;
			var moduleIndex = -1;
			var usedFiles = new Dictionary<string, int>();
			var usedIcons = new Dictionary<string, int>();
			var generatedNames = new Dictionary<string, string> {
				{ "", "" }
			};

			static bool isEmpty(string s) => (s == null || s == "");
			static string cleanName(string s) {
				var result = "";
				foreach (var c in s) {
					if (char.IsAsciiLetterOrDigit(c)) {
						result += c;
					} else if (c == ' ') {
						result += '_';
					}
				}
				return result;
			}

			foreach (var entry in _entries) {
				if (entry is HeaderEntry header) {
					++headerIndex;
					if (isEmpty(header.Text)) {
						log += $"Header #{headerIndex + 1} is empty!\n\n";
					}
				} else if (entry is ModuleEntry module) {
					++moduleIndex;

					var moduleMessages = new List<string>();
					var moduleName = "";

					if (isEmpty(module.Name)) {
						if (module.Options.Count > 1) {
							moduleMessages.Add("has no name!");
						}
					} else {
						moduleName = $" (\"{module.Name}\")";
					}

					if (module.Options.Count == 0) {
						moduleMessages.Add("skipped (contains no options).");
					} else if (module.Options.Count == 1) {
						moduleMessages.Add("internal. Will not be shown to user.");

						if (!isEmpty(module.Options[0]._iconPath) && module.Options[0].SelectedIconItem != null) {
							moduleMessages.Add($"has icon \"{module.Options[0].SelectedIconItem.Name}\" set. It will be ignored.");
						}
					}

					var emptyFiles = 0;
					var emptyIcons = 0;
					var nonEmptyIcons = 0;
					var optionIndex = -1;
					foreach (var option in module.Options) {
						++optionIndex;

						if (isEmpty(option.Name) && module.Options.Count > 1) {
							moduleMessages.Add($"option #{optionIndex + 1} has no name!");
						}

						if (isEmpty(option._iconPath)) {
							++emptyIcons;
						} else {
							if (option.SelectedIconItem != null) ++nonEmptyIcons;

							if (module.Options.Count > 1) {
								if (usedIcons.ContainsKey(option._iconPath)) {
									var warning = $"option #{optionIndex + 1}";
									if (option.Name != "") warning += $" (\"{option.Name}\")";

									warning += " uses the same icon";
									if (option.SelectedIconItem != null) {
										warning += $" \"{option.SelectedIconItem.Name}\"";
									}

									warning += $" as was used previously in option of module #{usedIcons[option._iconPath] + 1}. Could this be a mistake?";
									moduleMessages.Add(warning);
									usedIcons[option._iconPath] = moduleIndex;
								} else {
									usedIcons.Add(option._iconPath, moduleIndex);

									var iconItem = option.SelectedIconItem;
									var icon = option.SelectedIconItem?.Icon;
									if (iconItem != null && icon != null) {
										if (_selectedStyle == "small" && (icon.PixelWidth != 32 || icon.PixelHeight != 32)) {
											var warning = $"option #{optionIndex + 1}";
											if (option.Name != "") warning += $" (\"{option.Name}\")";

											warning += $" uses icon";
											if (option.SelectedIconItem != null) {
												warning += $" \"{option.SelectedIconItem.Name}\"";
											}

											warning += $" with size {icon.PixelWidth}x{icon.PixelHeight} (expected 32x32). It might appear stretched in case of aspect ratio mismatch or cause higher file size in case it's bigger than necessary.";
											moduleMessages.Add(warning);
										}
									}
								}

								if (!generatedNames.ContainsKey(option._iconPath)) {
									generatedNames[option._iconPath] = $"icons/{moduleIndex:D2}_{cleanName(moduleName)}/{optionIndex:D2}_{cleanName(option.Name)}{Path.GetExtension(option._iconPath)}";
								}
							}
						}

						if (isEmpty(option._path)) {
							++emptyFiles;
						} else {
							if (usedFiles.ContainsKey(option._path)) {
								var warning = $"option #{optionIndex + 1}";
								if (option.Name != "") warning += $" (\"{option.Name}\")";

								warning += " uses the same file";
								if (option.SelectedPathItem != null) {
									warning += $" \"{option.SelectedPathItem.Name}\"";
								}

								warning += $" as was used previously in option of module #{usedFiles[option._path] + 1}!";
								moduleMessages.Add(warning);
								usedFiles[option._path] = moduleIndex;
							} else {
								usedFiles.Add(option._path, moduleIndex);
							}

							if (!generatedNames.ContainsKey(option._path)) {
								generatedNames[option._path] = $"modules/{moduleIndex:D2}_{cleanName(moduleName)}/{optionIndex:D2}_{cleanName(option.Name)}{Path.GetExtension(option._path)}";
							}
						}
					}

					if (nonEmptyIcons > 0 && _selectedStyle == "none") {
						moduleMessages.Add($"has options with icons, but style is \"No icons\"!");
					}
					if (emptyIcons > 1 && _selectedStyle != "none") {
						moduleMessages.Add($"has more than one option without icon. Could this be a mistake?");
					}

					if (moduleMessages.Count > 0) {
						if (moduleMessages.Count == 1) {
							log += $"Module #{moduleIndex + 1}{moduleName}: {moduleMessages[0]}\n\n";
						} else {
							log += $"Module #{moduleIndex + 1}{moduleName}:\n";
							foreach (var message in moduleMessages) {
								log += "- " + message + "\n";
							}
							log += $"\n";
						}
					}
				}
			}

			if (moduleIndex == -1) {
				log += "Layout contains no modules!\n\n";
			}

			var first = true;
			foreach (var file in _modules) {
				if (!usedFiles.ContainsKey(file.Path)) {
					if (first) {
						log += "Unused files:\n";
						first = false;
					}

					log += $"- \"{file.Name}\"\n";
				}
			}
			if (!first) log += "\n";

			if (_selectedStyle != "none") {
				first = true;
				foreach (var icon in _icons) {
					if (!usedIcons.ContainsKey(icon.Path)) {
						if (first) {
							log += "Unused icons:\n";
							first = false;
						}

						log += $"- \"{icon.Name}\"\n";
					}
				}
				if (!first) log += "\n";
			}

			// form .json with names generated for the archive
			var layout = new JArray();

			foreach (var entry in _entries) {
				if (entry is HeaderEntry header) {
					layout.Add(new JArray() { "header", header.Text });
					continue;
				}
				
				if (entry is ModuleEntry module) {
					// 1 option -- internal module -- no icon
					if (module.Options.Count == 1) {
						var options = new JArray();
						foreach (var option in module.Options) {
							options.Add(new JArray() { "", option.Name, generatedNames[option._path] });
						}
						layout.Add(new JArray() { "module", module.Name, options });
						continue;
					}
					
					if (module.Options.Count > 1) {
						var options = new JArray();
						foreach (var option in module.Options) {
							options.Add(new JArray() { generatedNames[option._iconPath], option.Name, generatedNames[option._path] });
						}
						layout.Add(new JArray() { "module", module.Name, options });
						continue;
					}

					continue;
				}
				
				if (entry is SeparatorEntry) {
					layout.Add(new JArray() { "separator" });
					continue;
				}
			}

			var info = new JObject {
				["game"] = _gameId,
				["name"] = _modName,
				["author"] = _author,

				["format_version"] = 1,
				["icons_style"] = _selectedStyle,

				["layout"] = layout
			};

			// form archive with files and generated names
			using var f = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
			using var zip = new ZipArchive(f, ZipArchiveMode.Create);

			{
				var text = info.ToString();
				var data = Encoding.UTF8.GetBytes(text);

				var entry = zip.CreateEntry("info.json");
				using var ef = entry.Open();
				ef.Write(data, 0, data.Length);
			}

			foreach (var realPath in generatedNames.Keys) {
				if (realPath == "") continue;

				var bytes = File.ReadAllBytes(realPath);

				var path = generatedNames[realPath];
				var entry = zip.CreateEntry(path);
				using var ef = entry.Open();
				ef.Write(bytes, 0, bytes.Length);
			}

			log += "Done!\n";
		} catch (Exception ex) {
			log += "Exception happened:\n";
			log += ex.ToString();
		}

		LogTextBox.Text = log;
		Focus();
	}

	#endregion

	#endregion
}
