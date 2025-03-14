// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DAT1;
using DAT1.Sections.Material;
using DAT1.Sections.Model;
using Newtonsoft.Json.Linq;
using OverstrikeShared.STG;
using OverstrikeShared.STG.Files;
using OverstrikeShared.Utils;
using SuitTool.Data;
using SuitTool.Windows;

namespace SuitTool {
	public partial class MainWindow: Window {
		private string _projectPath = null;
		private Project _project = null;
		private bool _modified = false;
		private bool _loadingValues = false;
		private bool _alreadySuggestedSaving = false;

		private AssetPathsWindow _assetPathsWindow = null;
		private LogWindow _logWindow = null;

		public class FilenameOption {
			public string Filename { get; set; }
			public string DisplayName { get; set; }
		}

		private List<FilenameOption> _iconsList = new();
		private List<FilenameOption> _modelsList = new();
		private List<FilenameOption> _materialsList = new();

		private string ProjectName {
			get {
				var result = (_project == null ? "" : _project.ModName);

				if (result == "") {
					result = Path.GetFileName(_projectPath);
				}

				return result;
			}
		}

		//

		public MainWindow() {
			InitializeComponent();

			_loadingValues = true;
			HeroComboBox.Items.Clear();
			HeroComboBox.Items.Add(Project.HERO_PETER);
			HeroComboBox.Items.Add(Project.HERO_MILES);
			HeroComboBox.SelectedIndex = 0;

			LegsComboBox.Items.Clear();
			LegsComboBox.Items.Add(Project.LEGS_UNSPECIFIED);
			LegsComboBox.Items.Add("hero_spiderman_advanced_legs");
			LegsComboBox.Items.Add("hero_spiderman_ironspider_legs");
			LegsComboBox.Items.Add("hero_spiderman_itsvnoir_legs");
			LegsComboBox.Items.Add("hero_spiderman_iw_legs");
			LegsComboBox.Items.Add("hero_spiderman_miles_ironspider_legs");
			LegsComboBox.Items.Add("hero_spiderman_momoko_legs");
			LegsComboBox.Items.Add("hero_spiderman_superior_legs");
			LegsComboBox.SelectedIndex = 0;
			_loadingValues = false;
		}

		public void OpenProject(string filename) {
			SuggestSavingChanges();

			try {
				var project = new Project(filename);
				_project = project;
				_projectPath = filename;
				_modified = false;

				((App)Application.Current).UpdateRecentProjects(filename);

				RefreshAllControls();
				UpdateTitle();
			} catch {}
		}

		private void SaveProject() {
			if (_project == null) return;
			if (_projectPath == null) return;

			try {
				_project.Save(_projectPath);
				_modified = false;
				UpdateTitle();

				((App)Application.Current).UpdateRecentProjects(_projectPath);
			} catch {}
		}

		private void SuggestSavingChanges() {
			if (_alreadySuggestedSaving) return;

			if (_projectPath != null) {
				if (_modified) {
					MessageBoxResult result = MessageBox.Show($"There are unsaved changes in '{ProjectName}'. Do you want to save them?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Question);
					if (result == MessageBoxResult.Yes) {
						SaveProject();
					}
				}
			}
		}

		private void UpdateTitle() {
			Title = (_modified ? "*" : "") + $"{ProjectName} — Suit Tool";
		}

		//

		#region data

		#region combobox lists

		private static List<string> RemoveCommonPrefix(List<string> paths) {
			var result = new List<string>();
			if (paths.Count == 0) return result;

			static string Normalize(string p) {
				return p.Replace("\\", "/");
			}

			var firstPath = Normalize(Path.GetDirectoryName(paths[0]));

			while (true) {
				var matchesAll = true;
				foreach (var path in paths) {
					if (!path.StartsWith(firstPath)) {
						matchesAll = false;
						break;
					}
				}

				if (matchesAll) {
					foreach (var path in paths) {
						result.Add(Normalize(Path.GetRelativePath(firstPath, path)));
					}
					return result;
				}

				firstPath = Normalize(Path.GetDirectoryName(firstPath));
				if (firstPath == null || firstPath == "") {
					return paths;
				}
			}
		}

		private void MakeComboBoxLists() {
			_iconsList.Clear();

			var icons = new List<string>();

			// filter out textures that have counterparts
			foreach (var texture in _project.Textures) {
				if (texture.StartsWith("0/")) {
					if (_project.Textures.Contains(texture.Replace("0/", "1/"))) continue;
				} else if (texture.StartsWith("1/")) {
					if (_project.Textures.Contains(texture.Replace("1/", "0/"))) continue;
				}

				icons.Add(texture);
			}

			// sort by whether contain "ui" or "icon" + sort by size
			static bool IsIconLike(string p) {
				return (p.Contains("ui/", StringComparison.OrdinalIgnoreCase) || p.Contains("icon", StringComparison.OrdinalIgnoreCase));
			}

			icons.Sort((x, y) => {
				var xIsIconLike = IsIconLike(x);
				var yIsIconLike = IsIconLike(y);

				if (xIsIconLike != yIsIconLike) {
					return (xIsIconLike ? -1 : 1);
				}

				try {
					var xSize = new FileInfo(Path.Combine(Path.GetDirectoryName(_projectPath), x)).Length;
					var ySize = new FileInfo(Path.Combine(Path.GetDirectoryName(_projectPath), y)).Length;
					if (xSize != ySize) {
						return (xSize < ySize ? -1 : 1);
					}
				} catch {}

				return string.Compare(x, y, StringComparison.Ordinal);
			});

			var shortIcons = RemoveCommonPrefix(icons);
			foreach (var (path, name) in icons.Zip(shortIcons)) {
				_iconsList.Add(new FilenameOption { Filename = path, DisplayName = name });
			}

			MainIconComboBox.ItemsSource = new CompositeCollection {
				new CollectionContainer() { Collection = _iconsList }
			};

			//

			_modelsList.Clear();
			var shortModels = RemoveCommonPrefix(_project.Models);
			foreach (var (path, name) in _project.Models.Zip(shortModels)) {
				_modelsList.Add(new FilenameOption { Filename = path, DisplayName = name });
			}

			ModelComboBox.ItemsSource = new CompositeCollection {
				new CollectionContainer() { Collection = _modelsList }
			};
			MaskModelComboBox.ItemsSource = new CompositeCollection {
				new CollectionContainer() { Collection = _modelsList }
			};

			//

			_materialsList.Clear();

			_materialsList.Add(new FilenameOption { Filename = "", DisplayName = "" }); // DEFAULT

			var shortMaterials = RemoveCommonPrefix(_project.Materials);
			foreach (var (path, name) in _project.Materials.Zip(shortMaterials)) {
				_materialsList.Add(new FilenameOption { Filename = path, DisplayName = name });
			}
		}

		#endregion

		#region icons

		private Dictionary<string, BitmapSource> _loadedIcons = new();

		private BitmapSource GetBitmapSourceForTexture(string filename) {
			var fullname = Path.Combine(Path.GetDirectoryName(_projectPath), filename);
			if (_loadedIcons.ContainsKey(fullname)) {
				return _loadedIcons[fullname];
			}

			try {
				var texture = new Texture();
				texture.Load(fullname);

				var dds = texture.GetDDS();
				var bitmap = Utils.Imaging.DdsToBitmap(dds);
				_loadedIcons[fullname] = Utils.Imaging.ConvertToBitmapImage(bitmap);

				return _loadedIcons[fullname];
			} catch {}

			return null;
		}

		#endregion

		private Dictionary<string, string> GetDefaultModelMaterials() {
			var result = new Dictionary<string, string>();

			void ReadModel(string modelPath) {
				try {
					var model = new STG();
					model.Load(modelPath);

					var materialsSection = model.Dat1.Section<ModelMaterialSection>(ModelMaterialSection.TAG);
					foreach (var material in materialsSection.Materials) {
						var slot = material.SlotName;
						var path = material.Path;
						result[slot] = path;
					}
				} catch {}
			}

			var cwd = Path.GetDirectoryName(_projectPath);
			ReadModel(Path.Combine(cwd, _project.MainModel));
			ReadModel(Path.Combine(cwd, _project.MaskModel));

			return result;
		}

		#endregion

		#region ui

		private void RefreshAllControls() {
			_loadingValues = true;

			ModNameTextBox.Text = _project.ModName;
			AuthorTextBox.Text = _project.Author;
			SuitNameTextBox.Text = _project.SuitName;
			SuitIdTextBox.Text = _project.Id;
			HeroComboBox.SelectedItem = _project.Hero;

			MakeComboBoxLists();

			MainIconComboBox.SelectedValue = _project.MainIcon;
			MainIcon.Source = GetBitmapSourceForTexture(_project.MainIcon);
			ModelComboBox.SelectedValue = _project.MainModel;
			MaskModelComboBox.SelectedValue = _project.MaskModel;

			LegsComboBox.SelectedItem = _project.IronLegs;
			BlackWebsCheckbox.IsChecked = _project.BlackWebs;
			TentacleTraversalCheckbox.IsChecked = _project.TentacleTraversal;

			MakeStylesControls();

			_loadingValues = false;
		}

		private void MakeStylesControls(Dictionary<string, string> defaultMaterials = null) {
			var wasExpanded = new Dictionary<Project.Style, bool>();
			foreach (var child in StylesContainer.Children) {
				if (child is Expander expander) {
					if (expander.DataContext is Project.Style style) {
						wasExpanded[style] = expander.IsExpanded;
					}
				}
			}

			StylesContainer.Children.Clear();

			if (_project.Styles.Count == 0) {
				MinHeight = 436;
				Height = MinHeight;
				var tb = new TextBlock {
					Text = "No styles added",
					HorizontalAlignment = HorizontalAlignment.Left,
					Padding = new Thickness(1, 1, 1, 1),
					Foreground = Brushes.Gray
				};
				StylesContainer.Children.Add(tb);
				return;
			}

			MinHeight = 654;

			if (defaultMaterials == null) {
				defaultMaterials = GetDefaultModelMaterials();
			}

			var i = 0;
			foreach (var style in _project.Styles) {
				++i;
				var expander = new Expander {
					Header = $" Style #{i}",
					IsExpanded = wasExpanded.GetValueOrDefault(style, true),

					HorizontalAlignment = HorizontalAlignment.Stretch,
					Margin = new Thickness(-1, 3, -1, 5),
					Padding = new Thickness(0, 8, 0, 2)
				};
				expander.DataContext = style;

				var grid = new Grid();
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
				grid.ColumnDefinitions.Add(new ColumnDefinition());

				// column 0
				{
					var panel = new StackPanel();
					Grid.SetColumn(panel, 0);
					grid.Children.Add(panel);

					var border = new Border {
						BorderThickness = new Thickness(1),
						BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
						Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)),
					};
					panel.Children.Add(border);

					var image = new Image {
						Stretch = Stretch.Fill,
						Width = 160,
						Height = 160
					};
					image.Source = GetBitmapSourceForTexture(style.Icon);
					border.Child = image;

					var cb = new ComboBox {
						Margin = new Thickness(0, 6, 0, 0),
						SelectedValuePath = "Filename",
						DisplayMemberPath = "DisplayName",
					};
					cb.ItemsSource = new CompositeCollection {
						new CollectionContainer() { Collection = _iconsList }
					};
					cb.SelectedValue = style.Icon;
					cb.ToolTip = style.Icon;
					cb.SelectionChanged += StyleIcon_SelectionChanged;
					cb.DataContext = new Tuple<Project.Style, Image>(style, image);
					panel.Children.Add(cb);

					var button = new Button {
						Content = "Auto-Refill",

						Margin = new Thickness(0, 6, 0, 0),
						Height = 22,
					};
					button.Click += StyleRefill_Click;
					button.DataContext = style;
					panel.Children.Add(button);
				}

				// column 1
				{
					var panel = new StackPanel() {
						Margin = new Thickness(10, 0, 0, 0)
					};
					Grid.SetColumn(panel, 1);
					grid.Children.Add(panel);

					{
						var gridGroup = new Grid {
							Height = 22,
							Margin = new Thickness(0, 0, 0, 6)
						};
						gridGroup.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
						gridGroup.ColumnDefinitions.Add(new ColumnDefinition());
						gridGroup.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });

						var label = new Label {
							Content = "ID",
							HorizontalAlignment = HorizontalAlignment.Left,
							Margin = new Thickness(0, -3, 0, 0),
						};
						Grid.SetColumn(label, 0);
						gridGroup.Children.Add(label);

						var tb = new TextBox {
							Text = style.Id,

							Height = 22,
							Margin = new Thickness(0, 0, 0, 0),
							Padding = new Thickness(1, 1, 1, 1)
						};
						tb.TextChanged += StyleId_TextChanged;
						tb.DataContext = style;
						Grid.SetColumn(tb, 1);
						gridGroup.Children.Add(tb);

						var button = new Button {
							Content = "x",

							Margin = new Thickness(10, 0, 0, 0),
							Width = 22,
							Height = 22,
						};
						button.Click += StyleDelete_Click;
						button.DataContext = style;
						Grid.SetColumn(button, 2);
						gridGroup.Children.Add(button);

						panel.Children.Add(gridGroup);
					}

					{
						var gridGroup = new Grid {
							Height = 22,
							Margin = new Thickness(0, 0, 0, 6)
						};
						gridGroup.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
						gridGroup.ColumnDefinitions.Add(new ColumnDefinition());

						var label = new Label {
							Content = "Name",
							HorizontalAlignment = HorizontalAlignment.Left,
							Margin = new Thickness(0, -3, 0, 0),
						};
						Grid.SetColumn(label, 0);
						gridGroup.Children.Add(label);

						var tb = new TextBox {
							Text = style.Name,

							Height = 22,
							Margin = new Thickness(0, 0, 0, 0),
							Padding = new Thickness(1, 1, 1, 1)
						};
						tb.TextChanged += StyleName_TextChanged;
						tb.DataContext = style;
						Grid.SetColumn(tb, 1);
						gridGroup.Children.Add(tb);

						panel.Children.Add(gridGroup);
					}

					var label2 = new Label {
						Content = "Material overrides:",
						HorizontalAlignment = HorizontalAlignment.Left,
						Margin = new Thickness(0, 8, 0, 4),
					};
					Grid.SetColumn(label2, 0);
					panel.Children.Add(label2);

					foreach (var pair in defaultMaterials) {
						var slot = pair.Key;
						var path = pair.Value;

						var gridGroup = new Grid {
							Height = 22,
							Margin = new Thickness(0, 0, 0, 6)
						};
						gridGroup.ColumnDefinitions.Add(new ColumnDefinition());
						gridGroup.ColumnDefinitions.Add(new ColumnDefinition());

						var tb = new TextBox {
							Text = slot,
							IsReadOnly = true,
							IsReadOnlyCaretVisible = true,

							Height = 22,
							Margin = new Thickness(5, 0, 0, 0),
							Padding = new Thickness(1, 1, 1, 1)
						};
						Grid.SetColumn(tb, 0);
						gridGroup.Children.Add(tb);

						var cb = new ComboBox {
							Height = 22,
							Margin = new Thickness(10, 0, 0, 0),
							SelectedValuePath = "Filename",
							DisplayMemberPath = "DisplayName",
						};
						cb.ItemsSource = new CompositeCollection {
							new CollectionContainer() { Collection = _materialsList }
						};
						if (style.Overrides.ContainsKey(slot)) {
							cb.SelectedValue = style.Overrides[slot];
							cb.ToolTip = style.Overrides[slot];
						} else {
							cb.SelectedValue = "";
						}
						cb.SelectionChanged += StyleMaterialOverride_SelectionChanged;
						cb.DataContext = new Tuple<Project.Style, string>(style, slot);
						Grid.SetColumn(cb, 1);
						gridGroup.Children.Add(cb);

						panel.Children.Add(gridGroup);
					}
				}

				expander.Content = grid;

				StylesContainer.Children.Add(expander);
			}
		}

		#endregion

		#region event handlers

		#region menu

		private void File_SubmenuOpened(object sender, RoutedEventArgs e) {
			var recentProjects = ((App)Application.Current).SortedRecentProjects;
			File_OpenRecent.Visibility = (recentProjects.Count > 0 ? Visibility.Visible : Visibility.Collapsed);

			void UpdateItem(MenuItem item, int index) {
				item.Visibility = (recentProjects.Count > index ? Visibility.Visible : Visibility.Collapsed);
				item.Header = (recentProjects.Count > index ? recentProjects[index].ProjectPath : "").Replace("_", "__");
			};

			UpdateItem(File_OpenRecent1, 0);
			UpdateItem(File_OpenRecent2, 1);
			UpdateItem(File_OpenRecent3, 2);
			UpdateItem(File_OpenRecent4, 3);
			UpdateItem(File_OpenRecent5, 4);
		}

		private void NewProjectCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) {
			SuggestSavingChanges();
			_alreadySuggestedSaving = true;
			((App)Application.Current).ShowNewProjectDialog();
			_alreadySuggestedSaving = false;
		}

		private void OpenProjectCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) {
			SuggestSavingChanges();
			_alreadySuggestedSaving = true;
			((App)Application.Current).ShowOpenProjectDialog();
			_alreadySuggestedSaving = false;
		}

		private void File_OpenRecentItem_Click(object sender, RoutedEventArgs e) {
			var recentProjects = ((App)Application.Current).SortedRecentProjects;

			bool CheckItem(MenuItem item, int index) {
				if (sender == item) {
					if (recentProjects.Count > index) {
						OpenProject(recentProjects[index].ProjectPath);
					}
					return true;
				}
				return false;
			};

			if (CheckItem(File_OpenRecent1, 0)) return;
			if (CheckItem(File_OpenRecent2, 1)) return;
			if (CheckItem(File_OpenRecent3, 2)) return;
			if (CheckItem(File_OpenRecent4, 3)) return;
			if (CheckItem(File_OpenRecent5, 4)) return;
		}

		private void SaveProjectCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) {
			SaveProject();
		}

		private void RefreshAssetsCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) {
			if (_project != null && _projectPath != null) {
				_project.RefreshAssets(_projectPath);
			}

			RefreshAllControls();
		}

		private void Assets_ViewList_Click(object sender, RoutedEventArgs e) {
			if (_project == null) return;

			if (_assetPathsWindow != null) {
				_assetPathsWindow.Close();
			}

			_assetPathsWindow = new AssetPathsWindow(_project);
			PositionChildWindow(_assetPathsWindow);
			_assetPathsWindow.Show();
		}

		private void PositionChildWindow(Window window) {
			var screenWidth = SystemParameters.FullPrimaryScreenWidth;
			var screenHeight = SystemParameters.FullPrimaryScreenHeight;

			var left = WindowState == WindowState.Maximized ? 0 : Left;
			var top = WindowState == WindowState.Maximized ? 0 : Top;
			var width = WindowState == WindowState.Maximized ? screenWidth : Width;
			var height = WindowState == WindowState.Maximized ? screenHeight : Height;

			window.Left = Math.Min(left + width + 16, screenWidth - window.Width - 16);
			window.Top = Math.Max(top + height / 2 - window.Height / 2, 0);
		}

		#endregion

		#region main

		private void ModNameTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			if (_project == null) return;
			if (_loadingValues) return;
			
			_project.ModName = ModNameTextBox.Text;
			_modified = true;
			UpdateTitle();
		}

		private void AuthorTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			if (_project == null) return;
			if (_loadingValues) return;

			_project.Author = AuthorTextBox.Text;
			_modified = true;
			UpdateTitle();
		}

		private void SuitNameTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			if (_project == null) return;
			if (_loadingValues) return;

			_project.SuitName = SuitNameTextBox.Text;
			_modified = true;
			UpdateTitle();
		}

		private void SuitIdTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			if (_project == null) return;
			if (_loadingValues) return;

			_project.Id = SuitIdTextBox.Text;
			_modified = true;
			UpdateTitle();
		}

		private void HeroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_project == null) return;
			if (_loadingValues) return;

			_project.Hero = (string)HeroComboBox.SelectedItem;
			_modified = true;
			UpdateTitle();
		}

		private void IconComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				MainIconComboBox.ToolTip = (string)MainIconComboBox.SelectedValue;
			} catch {}

			if (_project == null) return;
			if (_loadingValues) return;

			_project.MainIcon = (string)MainIconComboBox.SelectedValue;
			MainIcon.Source = GetBitmapSourceForTexture(_project.MainIcon);
			_modified = true;
			UpdateTitle();
		}

		private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				ModelComboBox.ToolTip = (string)ModelComboBox.SelectedValue;
			} catch {}

			if (_project == null) return;
			if (_loadingValues) return;

			_project.MainModel = (string)ModelComboBox.SelectedValue;
			_modified = true;
			MakeStylesControls();
			UpdateTitle();
		}

		private void MaskModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				MaskModelComboBox.ToolTip = (string)MaskModelComboBox.SelectedValue;
			} catch {}

			if (_project == null) return;
			if (_loadingValues) return;

			_project.MaskModel = (string)MaskModelComboBox.SelectedValue;
			_modified = true;
			MakeStylesControls();
			UpdateTitle();
		}

		private void LegsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_project == null) return;
			if (_loadingValues) return;

			_project.IronLegs = (string)LegsComboBox.SelectedItem;
			_modified = true;
			UpdateTitle();
		}

		private void BlackWebsCheckbox_Changed(object sender, RoutedEventArgs e) {
			if (_project == null) return;
			if (_loadingValues) return;

			_project.BlackWebs = (bool)BlackWebsCheckbox.IsChecked;
			_modified = true;
			UpdateTitle();
		}

		private void TentacleTraversalCheckbox_Changed(object sender, RoutedEventArgs e) {
			if (_project == null) return;
			if (_loadingValues) return;

			_project.TentacleTraversal = (bool)TentacleTraversalCheckbox.IsChecked;
			_modified = true;
			UpdateTitle();
		}

		#endregion

		private void AddStyleButton_Click(object sender, RoutedEventArgs e) {
			var materials = GetDefaultModelMaterials();
			_project.AddStyle(materials);
			_modified = true;

			MakeStylesControls(materials);
			UpdateTitle();

			if (_project.Styles.Count > 1) {
				ContentScrollViewer.ScrollToBottom();
			}
		}

		#region styles

		private void StyleIcon_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var cb = e.Source as ComboBox;
			if (cb == null) return;

			try {
				cb.ToolTip = (string)cb.SelectedValue;
			} catch {}

			var styleAndImage = cb.DataContext as Tuple<Project.Style, Image>;
			if (styleAndImage == null) return;

			var style = styleAndImage.Item1;
			var image = styleAndImage.Item2;

			style.Icon = (string)cb.SelectedValue;
			image.Source = GetBitmapSourceForTexture(style.Icon);
			_modified = true;
			UpdateTitle();
		}

		private void StyleRefill_Click(object sender, RoutedEventArgs e) {
			var button = e.Source as Button;
			if (button == null) return;

			var style = button.DataContext as Project.Style;
			if (style == null) return;

			var result = MessageBox.Show($"This will clear any of your choices in style '{style.Name}' (if there were any). Do you want to this style be refilled automatically?", "Auto-Refill", MessageBoxButton.OKCancel, MessageBoxImage.Question);
			if (result != MessageBoxResult.OK) {
				return;
			}

			var materials = GetDefaultModelMaterials();
			_project.RefillStyle(style, materials);
			_modified = true;
			MakeStylesControls(materials);
			UpdateTitle();
		}

		private void StyleId_TextChanged(object sender, TextChangedEventArgs e) {
			var tb = e.Source as TextBox;
			if (tb == null) return;

			var style = tb.DataContext as Project.Style;
			if (style == null) return;

			style.Id = tb.Text;
			_modified = true;
			UpdateTitle();
		}

		private void StyleDelete_Click(object sender, RoutedEventArgs e) {
			var button = e.Source as Button;
			if (button == null) return;

			var style = button.DataContext as Project.Style;
			if (style == null) return;

			_project.Styles.Remove(style);
			_modified = true;
			MakeStylesControls();
			UpdateTitle();
		}

		private void StyleName_TextChanged(object sender, TextChangedEventArgs e) {
			var tb = e.Source as TextBox;
			if (tb == null) return;

			var style = tb.DataContext as Project.Style;
			if (style == null) return;

			style.Name = tb.Text;
			_modified = true;
			UpdateTitle();
		}

		private void StyleMaterialOverride_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var cb = e.Source as ComboBox;
			if (cb == null) return;

			try {
				cb.ToolTip = (string)cb.SelectedValue;
			} catch {}

			var styleAndSlot = cb.DataContext as Tuple<Project.Style, string>;
			if (styleAndSlot == null) return;

			var style = styleAndSlot.Item1;
			var slot = styleAndSlot.Item2;
			style.Overrides[slot] = (string)cb.SelectedValue;
			_modified = true;
			UpdateTitle();
		}

		#endregion

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			if (_projectPath != null) {
				if (_modified) {
					MessageBoxResult result = MessageBox.Show($"There are unsaved changes in '{ProjectName}'. Do you want to save them?", "Unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
					if (result == MessageBoxResult.Yes) {
						SaveProject();
					} else if (result == MessageBoxResult.Cancel) {
						e.Cancel = true;
					}
				}
			}

			if (!e.Cancel) {
				_assetPathsWindow?.Close();
				_logWindow?.Close();
			}
		}

		#region pack

		#region dependencies

		class AssetDependency {
			public string Path;
			public string Reason;
		}

		private void CollectDependencies(ref List<AssetDependency> assetDependencies) {
			var oldCount = assetDependencies.Count;
			var index = 0;

			while (index < oldCount) {
				for (; index < oldCount; ++index) {
					var dependency = assetDependencies[index];
					var path = dependency.Path;
					CollectDependenciesFrom(path, ref assetDependencies);
				}

				var newCount = assetDependencies.Count;
				if (oldCount == newCount) break;

				oldCount = newCount;
			}
		}

		private void AddDependency(ref List<AssetDependency> assetDependencies, string path, string reason) {
			path = DAT1.Utils.Normalize(path);

			var cwd = Path.GetDirectoryName(_projectPath);
			var localPath = Path.Combine(cwd, path);
			if (!File.Exists(localPath)) return;

			foreach (var dependency in assetDependencies) {
				if (dependency.Path == path) return;
			}

			assetDependencies.Add(new AssetDependency { Path = path, Reason = reason });
		}

		private void AddDependencyAllSpans(ref List<AssetDependency> assetDependencies, string path, string reason) {
			path = DAT1.Utils.Normalize(path);

			for (int i = 0; i < 256; ++i) {
				AddDependency(ref assetDependencies, $"{i}/{path}", reason);
			}
		}

		private bool AddConfigOverrideDependency(ref List<AssetDependency> assetDependencies, string path) {
			var before = assetDependencies.Count;
			AddDependency(ref assetDependencies, $"0/{path}", "config override");
			var after = assetDependencies.Count;
			return (before < after);
		}

		private void CollectDependenciesFrom(string path, ref List<AssetDependency> assetDependencies) {
			if (path.EndsWith(".model")) {
				CollectDependenciesFromModel(path, ref assetDependencies);
			} else if (path.EndsWith(".texture")) {
				CollectDependenciesFromTexture(path, ref assetDependencies);
			} else if (path.EndsWith(".material")) {
				CollectDependenciesFromMaterial(path, ref assetDependencies);
			}
		}

		private void CollectDependenciesFromModel(string path, ref List<AssetDependency> assetDependencies) {
			try {
				var cwd = Path.GetDirectoryName(_projectPath);
				var modelName = Path.GetFileName(path);

				var model = new STG();
				model.Load(Path.Combine(cwd, path));

				var materialsSection = model.Dat1.Section<ModelMaterialSection>(ModelMaterialSection.TAG);
				foreach (var material in materialsSection.Materials) {
					AddDependencyAllSpans(ref assetDependencies, material.Path, $"referenced in '{modelName}'");
				}
			} catch {}
		}

		private void CollectDependenciesFromTexture(string path, ref List<AssetDependency> assetDependencies) {
			try {
				if (path.StartsWith("0/")) {
					AddDependency(ref assetDependencies, string.Concat("1/", path.AsSpan(2)), "HD counterpart");
				}
			} catch {}
		}

		private void CollectDependenciesFromMaterial(string path, ref List<AssetDependency> assetDependencies) {
			try {
				var cwd = Path.GetDirectoryName(_projectPath);
				var materialName = Path.GetFileName(path);

				var material = new STG();
				material.Load(Path.Combine(cwd, path));

				var materialGraphPath = material.Dat1.GetStringByOffset((uint)(material.Dat1.FirstStringOffset + 20));
				if (materialGraphPath != null) {
					AddDependencyAllSpans(ref assetDependencies, materialGraphPath, $"referenced in '{materialName}'");
				}

				var parametersSection = material.Dat1.Section<MaterialSerializedDataSection>(MaterialSerializedDataSection.TAG);
				foreach (var texture in parametersSection.Textures) {
					AddDependencyAllSpans(ref assetDependencies, texture, $"referenced in '{materialName}'");
				}
			} catch {}
		}

		#endregion

		class AssetDescription {
			public byte Span;
			public ulong Id;

			public STG Stg;

			public uint Size => (uint)Stg.Raw.Length;
			public byte HeaderSize => (byte)(Stg.RawHeader == null ? 0 : Stg.RawHeader.Length);
			public bool IsTexture => (Stg.TextureMeta != null && Stg.TextureMeta.Length > 0);
			public byte TextureMetaSize => (byte)(IsTexture ? Stg.TextureMeta.Length : 0);
		}

		private void PackButton_Click(object sender, RoutedEventArgs e) {
			var cwd = Path.GetDirectoryName(_projectPath);
			var log = "";

			string RemoveSpanPrefix(string path) {
				path = path.Replace('\\', '/');
				var i = path.IndexOf('/');
				return path.Substring(i + 1);
			}

			try {
				var outDir = Path.Join(cwd, "out");
				if (!Directory.Exists(outDir)) {
					log += $"Creating \"out\"...\n";
					Directory.CreateDirectory(outDir);
					log += "Done.\n\n";
					log += "----\n\n";
				}

				//

				var outConfigsDir = Path.Join(outDir, "configs");
				if (!Directory.Exists(outConfigsDir)) {
					log += $"Creating \"out/configs\"...\n";
					Directory.CreateDirectory(outConfigsDir);
					log += "Done.\n\n";
					log += "----\n\n";
				}

				void SaveConfig(Config config, string path) {
					var basename = Path.GetFileName(path);
					log += $"- '{basename}'...\n";

					using var f = new FileStream(Path.Join(outConfigsDir, basename), FileMode.Create, FileAccess.Write, FileShare.None);
					using var w = new BinaryWriter(f);
					w.Write(config.Save());
				}

				log += $"Generating configs...\n\n";

				var id = _project.Id;
				var configsDir = $"suits/{id}/configs/";

				var icon = RemoveSpanPrefix(_project.MainIcon);
				var rewardConfigPath = configsDir + $"inv_reward_loadout_{id}.config";
				var suitName = $"SUIT_{id}";
				var varGroupConfigPath = configsDir + $"inv_{id}_variant_group.config";
				var varGroupName = $"suit_{id}_var_group";
				var loadoutConfigPath = configsDir + $"itemloadout_{id}.config";
				var varGroupLoadoutConfigPath = configsDir + $"itemloadout_{id}_variant_group.config";

				var rewardConfig = Config.Make("VanityLoadoutItemConfig");

				var defaultMaskModelObject = new JObject();
				if (_project.MaskModel != null && _project.MaskModel != "") {
					defaultMaskModelObject["AssetPath"] = RemoveSpanPrefix(_project.MaskModel);
				}
				defaultMaskModelObject["Autoload"] = false;

				var validCharactersArray = new JArray {
					_project.Hero == Project.HERO_PETER ? "kSpiderManPeter" : "kSpiderManMiles" // TODO: add a better way to translate one to another (support other constants?)
				};

				rewardConfig.ContentSection.Data = new JObject {
					["DamagedLoadoutConfig"] = new JObject {
						["Autoload"] = false
					},
					["DamagedMasklessLoadoutConfig"] = new JObject {
						["Autoload"] = false
					},
					["DefaultLoadoutConfig"] = new JObject {
						["AssetPath"] = loadoutConfigPath,
						["Autoload"] = false
					},
					["DefaultMaskModel"] = defaultMaskModelObject,
					["MasklessLoadoutConfig"] = new JObject {
						["Autoload"] = false
					},
					["Name"] = id,
					["PhotoModeSuitHealthEnabled"] = true,
					["Stackable"] = false,
					["ValidCharacters"] = validCharactersArray
				};

				if (_project.IronLegs != Project.LEGS_UNSPECIFIED) {
					var o = new JObject {
						["Dynamic_Enum_Value_Type"] = new JObject {
							["EnumAsset"] = "enums/hero_ironarmsmodel.dynamicenum",
							["EnumValue"] = _project.IronLegs
						}
					};

					rewardConfig.ContentSection.Data["DamagedIronArmsModel"] = o;
					rewardConfig.ContentSection.Data["DefaultIronArmsModel"] = o;
				}

				SaveConfig(rewardConfig, rewardConfigPath);

				//

				var vanityConfigPath = configsDir + $"vanity_{id}.config";
				Config loadoutConfig = null;

				if (_project.Hero == Project.HERO_PETER) {
					var vanityBodySM = "configs/VanityBodyType/VanityBody_SpiderMan.config";
					var vanityHED = "configs/VanityHED/VanityHEDSpiderMan1.config";

					loadoutConfig = Config.Make(
						"ItemLoadoutConfig",
						new List<string> { vanityBodySM, vanityHED, vanityConfigPath }
					);

					loadoutConfig.ContentSection.Data = new JObject {
						["Loadout"] = new JObject {
							["ItemLoadoutLists"] = new JArray {
								new JObject {
									["Items"] = new JArray {
										new JObject {
											["Item"] = vanityBodySM,
										},
										new JObject {
											["Item"] = vanityHED,
										},
										new JObject {
											["Item"] = vanityConfigPath,
										}
									}
								}
							},
							["Name"] = id
						},
					};
				} else {
					var vanityBodyType = "configs/VanityBodyType/VanityBody_MilesMorales.config";
					var vanityVenom = "configs/vanitytor1/vanity_i30_miles_venomxray.config";

					loadoutConfig = Config.Make(
						"ItemLoadoutConfig",
						new List<string> { vanityBodyType, vanityConfigPath, vanityVenom }
					);

					loadoutConfig.ContentSection.Data = new JObject {
						["Loadout"] = new JObject {
							["ItemLoadoutLists"] = new JArray {
								new JObject {
									["Items"] = new JArray {
										new JObject {
											["Item"] = vanityBodyType,
										},
										new JObject {
											["Item"] = vanityConfigPath,
										},
										new JObject {
											["Item"] = vanityVenom,
										},
									}
								}
							},
							["Name"] = id
						},
					};
				}

				SaveConfig(loadoutConfig, loadoutConfigPath);

				//

				var vanityConfig = Config.Make("VanityItemConfig");

				var conduitOverride = "conduit/characters/hero/hero_aud_vanity_spim_default/hero_aud_vanity_spip_default.conduit";
				if (_project.Hero != Project.HERO_PETER) {
					conduitOverride = "conduit/characters/hero/hero_aud_vanity_spim_default/hero_aud_vanity_spim_default.conduit";
				}

				vanityConfig.ContentSection.Data = new JObject {
					["Available"] = "kDefault",
					["Category"] = "kDefaultItem",
					["ConduitOverrideList"] = new JArray {
						new JObject {
							["ConduitOverride"] = new JObject {
								["Conduit"] = new JObject {
									["AssetPath"] = conduitOverride,
									["Autoload"] = false
								}
							}
						}
					},
					["ModelList"] = new JArray {
						new JObject {
							["BodyType"] = "kAll",
							["Model"] = new JObject {
								["AssetPath"] = RemoveSpanPrefix(_project.MainModel),
								["Autoload"] = false
							},
							["ModelInnerLayer"] = new JObject {
								["Autoload"] = false
							}
						}
					},
					["Name"] = id,
					["PartType"] = "kTypeBareTorsoAndArms",
					["ShaderUpdater"] = new JObject {
						["Type"] = "SkinShaderUpdaterPrius"
					},
					["SwitchGroupList"] = new JArray {
						new JObject {
							["SwitchGroup"] = new JObject {
								["SwitchGroupName"] = "SpiderHero_Vanity",
								["SwitchGroupValue"] = "Default"
							}
						}
					}
				};

				if (_project.BlackWebs) {
					vanityConfig.ContentSection.Data["ForceBlackWebs"] = true;
				}
				if (_project.TentacleTraversal) {
					vanityConfig.ContentSection.Data["ForceTentacleTraversal"] = true;
				}

				SaveConfig(vanityConfig, vanityConfigPath);

				//

				var varGroupConfig = Config.Make("VanityVariantGroupItemConfig");

				varGroupConfig.ContentSection.Data = new JObject {
					["ItemLoadoutConfig"] = new JObject {
						["AssetPath"] = varGroupLoadoutConfigPath,
						["Autoload"] = false
					},
					["Name"] = varGroupName,
				};

				SaveConfig(varGroupConfig, varGroupConfigPath);

				//

				log += "\nDone.\n\n----\n\n";

				log += "Scanning dependencies...\n";

				var assetDependencies = new List<AssetDependency>();

				AddDependency(ref assetDependencies, _project.MainModel, "main model");
				AddDependency(ref assetDependencies, _project.MainIcon, "main icon");
				if (_project.MaskModel != null && _project.MaskModel != "") {
					AddDependency(ref assetDependencies, _project.MaskModel, "mask model");
				}

				var hasRewardConfigOverride = AddConfigOverrideDependency(ref assetDependencies, rewardConfigPath);
				var hasLoadoutConfigOverride = AddConfigOverrideDependency(ref assetDependencies, loadoutConfigPath);
				var hasVanityConfigOverride = AddConfigOverrideDependency(ref assetDependencies, vanityConfigPath);
				var hasVarGroupConfigOverride = AddConfigOverrideDependency(ref assetDependencies, varGroupConfigPath);

				CollectDependencies(ref assetDependencies);

				var mainDepsFrom = 0;
				var mainDepsCount = assetDependencies.Count;

				var styleDepsFrom = new Dictionary<Project.Style, int>();
				var styleDepsCount = new Dictionary<Project.Style, int>();

				var pathsByAddTime = new List<string>();
				var pathToStyles = new Dictionary<string, List<Project.Style>>();
				var pathToReasons = new Dictionary<string, List<string>>();
				foreach (var style in _project.Styles) {
					var styleDependencies = new List<AssetDependency>();
					foreach (var dep in assetDependencies) {
						styleDependencies.Add(dep);
					}

					var from = styleDependencies.Count;
					AddDependency(ref styleDependencies, style.Icon, "style icon");

					foreach (var pair in style.Overrides) {
						var slot = pair.Key;
						var value = pair.Value;
						if (value == null || value.Trim() == "") continue;
						AddDependency(ref styleDependencies, value, "style override");
					}

					CollectDependencies(ref styleDependencies);
					var to = styleDependencies.Count;

					for (var i = from; i < to; ++i) {
						var dep = styleDependencies[i];
						var path = dep.Path;
						
						if (!pathToStyles.ContainsKey(path)) {
							pathsByAddTime.Add(path);
							pathToStyles[path] = new();
							pathToReasons[path] = new();
						}

						pathToStyles[path].Add(style);
						pathToReasons[path].Add(dep.Reason);
					}
				}

				foreach (var path in pathsByAddTime) {
					if (pathToStyles[path].Count > 1) {
						AddDependency(ref assetDependencies, path, $"used in {pathToStyles[path].Count} styles");
					}
				}

				mainDepsCount = assetDependencies.Count;

				foreach (var style in _project.Styles) {
					styleDepsFrom[style] = assetDependencies.Count;

					foreach (var path in pathsByAddTime) {
						if (pathToStyles[path].Count != 1) continue;

						var pathStyle = pathToStyles[path][0];
						if (style != pathStyle) continue;

						AddDependency(ref assetDependencies, path, pathToReasons[path][0]);
					}
					
					styleDepsCount[style] = assetDependencies.Count - styleDepsFrom[style];
				}

				var projAssets = _project.Assets;
				var verbose = true;

				void PrintDependencies(int from, int count) {
					if (verbose) {
						for (int i = from; i < from + count; ++i) {
							var dep = assetDependencies[i];
							log += $"- \"{dep.Path}\" -- {dep.Reason}\n";
						}
					}
				}

				log += "Done.\n\n";

				log += $"Used {assetDependencies.Count} assets out of {projAssets.Count} available\n";
				log += "\n";

				log += $"{mainDepsCount} in main .suit\n";
				PrintDependencies(mainDepsFrom, mainDepsCount);
				log += "\n";

				foreach (var style in _project.Styles) {
					log += $"{styleDepsCount[style]} in .suit_style \"{style.Name}\"\n";
					PrintDependencies(styleDepsFrom[style], styleDepsCount[style]);
					log += "\n";
				}

				bool IsInDependencies(string asset) {
					asset = DAT1.Utils.Normalize(asset);

					foreach (var dep in assetDependencies) {
						if (dep.Path == asset) return true;
					}

					return false;
				}

				const bool PACK_UNUSED = true;
				var unusedFrom = assetDependencies.Count;
				foreach (var asset in projAssets) {
					AddDependency(ref assetDependencies, asset, "unused");
				}
				var unusedCount = assetDependencies.Count - unusedFrom;

				if (unusedCount > 0) {
					log += $"{unusedCount} unused\n";
					PrintDependencies(unusedFrom, unusedCount);
					log += "\n";

					
					if (PACK_UNUSED) {
						log += "Default behavior: unused are packed into main .suit.\n\n";
					} else {
						log += "Default behavior: unused are not packed.\n\n";
					}
				}

				log += "----\n\n";
				log += "Packing...\n\n";

				var assetDescriptions = new List<AssetDescription>();

				AssetDescription MakeDescription(byte span, string filename, STG stg) {
					stg.Save();
					return new AssetDescription {
						Span = span,
						Id = CRC64.Hash(filename),
						Stg = stg,
					};
				}

				if (!hasRewardConfigOverride)
					assetDescriptions.Add(MakeDescription(0, rewardConfigPath, rewardConfig));
				if (!hasLoadoutConfigOverride)
					assetDescriptions.Add(MakeDescription(0, loadoutConfigPath, loadoutConfig));
				if (!hasVanityConfigOverride)
					assetDescriptions.Add(MakeDescription(0, vanityConfigPath, vanityConfig));
				if (!hasVarGroupConfigOverride)
					assetDescriptions.Add(MakeDescription(0, varGroupConfigPath, varGroupConfig));

				void MakeDescriptionForFile(ref List<AssetDescription> listToAddTo, string asset) {
					var path = asset.Replace('\\', '/');
					var i = path.IndexOf('/');
					var span = byte.Parse(path.Substring(0, i));
					var relpath = path.Substring(i + 1);

					string fullpath = null;
					ulong assetId;
					if (Regex.IsMatch(relpath, "^[0-9A-Fa-f]{16}$")) {
						assetId = ulong.Parse(relpath, NumberStyles.HexNumber);
					} else {
						assetId = CRC64.Hash(relpath);
						fullpath = relpath;
					}

					var assetStg = new STG();
					assetStg.Load(Path.Combine(cwd, path));

					listToAddTo.Add(new AssetDescription {
						Span = span,
						Id = assetId,

						Stg = assetStg,
					});
				}

				void MakeDescriptionsByDependencies(ref List<AssetDescription> listToAddTo, int from, int count) {
					for (int i = from; i < from + count; ++i) {
						var dep = assetDependencies[i];
						MakeDescriptionForFile(ref listToAddTo, dep.Path);
					}
				}

				MakeDescriptionsByDependencies(ref assetDescriptions, mainDepsFrom, mainDepsCount);
				if (PACK_UNUSED) {
					MakeDescriptionsByDependencies(ref assetDescriptions, unusedFrom, unusedCount);
				}

				//

				void WriteString(BinaryWriter w, string s) {
					var buf = Encoding.UTF8.GetBytes(s);
					w.Write((byte)buf.Length);
					w.Write(buf);
				}

				{
					log += $"- '{id}.suit'...\n";
					using var f = new FileStream(Path.Join(outDir, $"{id}.suit"), FileMode.Create, FileAccess.Write, FileShare.None);
					using var w = new BinaryWriter(f);

					// sort

					assetDescriptions.Sort((x, y) => {
						if (x.Span != y.Span)
							return x.Span - y.Span;

						if (x.Id == y.Id)
							return 0;

						return (x.Id < y.Id ? -1 : 1);
					});

					//

					using var ms = new MemoryStream();
					using var bw = new BinaryWriter(ms);

					var offsets = new List<uint>();
					foreach (var asset in assetDescriptions) {
						offsets.Add((uint)ms.Position);
						bw.Write(asset.Stg.Raw);
						BinaryStreams.Align16(bw);
					}

					var plain = ms.ToArray();
					var compressed = DSAR.Compress(plain);

					w.Write(compressed);
					BinaryStreams.Align16(w);

					//

					w.Write(new byte[1024]);
					BinaryStreams.Align16(w);

					var payloadStart = f.Position;

					WriteString(w, id);
					WriteString(w, _project.ModName);
					WriteString(w, _project.Author);
					WriteString(w, _project.SuitName);
					WriteString(w, icon);
					BinaryStreams.Align16(w);

					//

					var spans = new List<(byte, uint)>(); // (span, count)
					byte previousSpan = 0;
					uint spanCount = 0;

					foreach (var asset in assetDescriptions) {
						if (previousSpan == asset.Span) {
							++spanCount;
						} else {
							if (spanCount > 0) {
								spans.Add((previousSpan, spanCount));
							}

							previousSpan = asset.Span;
							spanCount = 1;
						}
					}

					if (spanCount > 0) {
						spans.Add((previousSpan, spanCount));
					}

					w.Write((byte)spans.Count);
					foreach (var (span, count) in spans) {
						w.Write((byte)span);
						w.Write((uint)count);
					}

					BinaryStreams.Align16(w);

					//

					{
						var i = 0;
						foreach (var asset in assetDescriptions) {
							w.Write(asset.Id);
							w.Write(offsets[i]);
							w.Write(asset.Size);
							++i;
						}

						foreach (var asset in assetDescriptions) {
							w.Write(asset.HeaderSize);
							w.Write(asset.TextureMetaSize);
						}

						BinaryStreams.Align16(w);
					}

					//

					foreach (var asset in assetDescriptions) {
						if (asset.HeaderSize > 0) {
							w.Write(asset.Stg.RawHeader);
						}
					}

					foreach (var asset in assetDescriptions) {
						if (asset.TextureMetaSize > 0) {
							w.Write(asset.Stg.TextureMeta);
						}
					}

					BinaryStreams.Align16(w);

					//

					var payloadEnd = f.Position;

					using var ms2 = new MemoryStream();
					using var bw2 = new BinaryWriter(ms2);
					bw2.Write((uint)0x54495553); // SUIT
					bw2.Write((uint)(payloadEnd - payloadStart));
					bw2.Write((byte)1); // version
					bw2.Write((byte)1); // game
					bw2.Write((byte)0);
					bw2.Write((byte)0);
					bw2.Write((uint)assetDescriptions.Count);

					var header = ms2.ToArray();
					var actual = new byte[16];
					for (var i = 0; i < 16; ++i) {
						actual[i] = (byte)(header[i] ^ compressed[i]);
					}

					w.Write(actual);
				}
				
				////
				
				foreach (var style in _project.Styles) {
					var styleAssetDescriptions = new List<AssetDescription>();
					MakeDescriptionsByDependencies(ref styleAssetDescriptions, styleDepsFrom[style], styleDepsCount[style]);

					var v = style.Id;

					log += $"- '{id}_{v}.suit_style'...\n";
					using var f = new FileStream(Path.Join(outDir, $"{id}_{v}.suit_style"), FileMode.Create, FileAccess.Write, FileShare.None);
					using var w = new BinaryWriter(f);

					// sort

					styleAssetDescriptions.Sort((x, y) => {
						if (x.Span != y.Span)
							return x.Span - y.Span;

						if (x.Id == y.Id)
							return 0;

						return (x.Id < y.Id ? -1 : 1);
					});

					//

					using var ms = new MemoryStream();
					using var bw = new BinaryWriter(ms);

					var offsets = new List<uint>();
					foreach (var asset in styleAssetDescriptions) {
						offsets.Add((uint)ms.Position);
						bw.Write(asset.Stg.Raw);
						BinaryStreams.Align16(bw);
					}

					var plain = ms.ToArray();
					var compressed = DSAR.Compress(plain);

					w.Write(compressed);
					BinaryStreams.Align16(w);

					//

					w.Write(new byte[1024]);
					BinaryStreams.Align16(w);

					var payloadStart = f.Position;

					WriteString(w, id);
					WriteString(w, style.Id);
					WriteString(w, style.Name);
					WriteString(w, _project.Author);
					WriteString(w, RemoveSpanPrefix(style.Icon));
					BinaryStreams.Align16(w);

					var overrides = new Dictionary<string, string>();
					foreach (var pair in style.Overrides) {
						var slot = pair.Key;
						var value = pair.Value;
						if (value == null || value.Trim() == "") continue;
						overrides[slot] = RemoveSpanPrefix(value);
					}

					foreach (var pair in overrides) {
						WriteString(w, pair.Key);
						WriteString(w, pair.Value);
					}
					BinaryStreams.Align16(w);

					//

					var spans = new List<(byte, uint)>(); // (span, count)
					byte previousSpan = 0;
					uint spanCount = 0;

					foreach (var asset in styleAssetDescriptions) {
						if (previousSpan == asset.Span) {
							++spanCount;
						} else {
							if (spanCount > 0) {
								spans.Add((previousSpan, spanCount));
							}

							previousSpan = asset.Span;
							spanCount = 1;
						}
					}

					if (spanCount > 0) {
						spans.Add((previousSpan, spanCount));
					}

					w.Write((byte)spans.Count);
					foreach (var (span, count) in spans) {
						w.Write((byte)span);
						w.Write((uint)count);
					}

					BinaryStreams.Align16(w);

					//

					{
						var i = 0;
						foreach (var asset in styleAssetDescriptions) {
							w.Write(asset.Id);
							w.Write(offsets[i]);
							w.Write(asset.Size);
							++i;
						}

						foreach (var asset in styleAssetDescriptions) {
							w.Write(asset.HeaderSize);
							w.Write(asset.TextureMetaSize);
						}

						BinaryStreams.Align16(w);
					}

					//

					foreach (var asset in styleAssetDescriptions) {
						if (asset.HeaderSize > 0) {
							w.Write(asset.Stg.RawHeader);
						}
					}

					foreach (var asset in styleAssetDescriptions) {
						if (asset.TextureMetaSize > 0) {
							w.Write(asset.Stg.TextureMeta);
						}
					}

					BinaryStreams.Align16(w);

					//

					var payloadEnd = f.Position;

					using var ms2 = new MemoryStream();
					using var bw2 = new BinaryWriter(ms2);
					bw2.Write((uint)0x4C595453); // STYL
					bw2.Write((uint)(payloadEnd - payloadStart));
					bw2.Write((byte)1); // version
					bw2.Write((byte)1); // game
					bw2.Write((byte)0);
					bw2.Write((byte)0);
					bw2.Write((uint)overrides.Count);

					var header = ms2.ToArray();
					var actual = new byte[16];
					for (var i = 0; i < 16; ++i) {
						actual[i] = (byte)(header[i] ^ compressed[i]);
					}

					w.Write(actual);
				}

				log += "\nDone!\n";
			} catch (Exception ex) {
				log += "Exception happened:\n";
				log += ex.ToString();
			}

			if (_logWindow != null) {
				_logWindow.Close();
			}

			_logWindow = new LogWindow(log);
			PositionChildWindow(_logWindow);
			_logWindow.Show();
			_logWindow.LogTextBox.ScrollToEnd();
		}

		#endregion

		#endregion
	}
}