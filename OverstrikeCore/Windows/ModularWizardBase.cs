﻿// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace OverstrikeShared.Windows {
	class ModuleOption {
		public string Name { get; set; }
		public BitmapSource Icon { get; set; }
	}

	public abstract class ModularWizardBase: Window {
		#region implementation-defined

		protected abstract Grid MainGrid { get; }
		protected abstract Label NumberLabel { get; }
		protected abstract TextBox NumberBox { get; }

		protected abstract string ModName { get; }
		protected abstract string IconsStyle { get; }

		protected abstract JArray LoadLayout();
		protected abstract ulong LoadSelectedCombinationNumber();
		protected abstract BitmapSource GetIconByPath(string path);

		protected abstract void SaveSelection();

		#endregion
		#region state

		protected JArray _layout;

		protected List<TextBlock> _moduleNamesLabels = new();
		protected List<ComboBox> _optionsSelectors = new();
		protected bool _ignoreChanges = true;

		#endregion

		protected void Init(Window mainWindow) {
			MainGrid.Children.Clear();

			try {
				Title = $"Editing '{ModName}'...";

				_layout = LoadLayout();
				MakeElements(mainWindow);

				// load user's selection
				_ignoreChanges = false;
				var current = LoadSelectedCombinationNumber();
				ApplyCombinationNumber(current);
				UpdateNumberLabel();
			} catch (Exception) {
				MessageBox.Show($"Failed to load '{ModName}'!", "Warning", MessageBoxButton.OK);
				Close();
			}
		}

		#region UI

		private void MakeElements(Window mainWindow) {
			var top = 15;
			foreach (var entry in _layout) {
				var entryType = (string)entry[0];
				if (entryType == "header") {
					MakeHeader(entry, ref top);
				} else if (entryType == "separator") {
					MakeSeparator(ref top);
				} else if (entryType == "module") {
					MakeModule(entry, ref top);
				}
			}

			// recalculate positions/window size once last label is loaded
			if (_moduleNamesLabels.Count > 0) {
				_moduleNamesLabels[^1].Loaded += (s, e) => {
					var left = 0;
					foreach (var item in _moduleNamesLabels) {
						left = Math.Max(left, 15 + (int)item.ActualWidth + 30);
					}

					foreach (var item in _optionsSelectors) {
						item.Margin = new Thickness(left, item.Margin.Top, item.Margin.Right, item.Margin.Bottom);
					}

					var width = left + 200 + 15 + 17 + 10; // combobox width, offset same as left, scrollbar width, some random unaccounted space
					var height = top + 32 + 18 + 32; // window title bar, unaccounted space, bottom bar with buttons
					Width = width;
					MinWidth = width;
					Height = Math.Min(height, mainWindow.Height); // can't be higher than main window

					Left = mainWindow.Left + mainWindow.Width / 2 - Width / 2;
					Top = mainWindow.Top + mainWindow.Height / 2 - Height / 2;
				};
			}
		}

		private void MakeHeader(JToken entry, ref int top) {
			var tb = new TextBlock {
				Text = (string)entry[1],
				FontSize = 18,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(15, top, 0, 20)
			};
			MainGrid.Children.Add(tb);

			top += 36;
		}

		private void MakeSeparator(ref int top) {
			top += 10;

			var s = new Separator {
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(15, top, 15, 25),
				Height = 4
			};
			MainGrid.Children.Add(s);

			top += 20;
		}

		private DataTemplate GetModuleSelectorTemplate() {
			return (DataTemplate)Resources["IconStyle_" + IconsStyle];
		}

		private int GetModuleLabelYOffset() {
			var style = IconsStyle;
			if (style == "small") return 8;
			return 0;
		}

		private int GetModuleLabelHeight() {
			var style = IconsStyle;
			if (style == "small") return 44;
			return 28;
		}

		private void MakeModule(JToken entry, ref int top) {
			var options = (JArray)entry[2];
			if (options.Count == 1) return;

			// module name

			var label = new TextBlock {
				Text = (string)entry[1],
				FontSize = 12,
				FontWeight = FontWeights.Bold,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(15, top + GetModuleLabelYOffset(), 0, 20)
			};
			MainGrid.Children.Add(label);
			_moduleNamesLabels.Add(label);

			// options

			var selector = new ComboBox {
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(300 + 15, top - 3, 0, 20),
				Width = 200,
				ItemTemplate = GetModuleSelectorTemplate()
			};

			var optionsItems = new List<ModuleOption>();
			foreach (var item in options) {
				optionsItems.Add(new ModuleOption() { Icon = GetIconByPath((string)item[0]), Name = (string)item[1] });
			}

			selector.ItemsSource = new CompositeCollection {
				new CollectionContainer() { Collection = optionsItems }
			};
			selector.SelectedIndex = 0;
			selector.SelectionChanged += ModuleSelectionChanged;

			MainGrid.Children.Add(selector);
			_optionsSelectors.Add(selector);

			top += GetModuleLabelHeight();
		}

		#endregion
		#region event handlers

		protected void ModuleSelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_ignoreChanges) return;

			UpdateNumberLabel();
		}

		protected void NumberLabel_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			if (NumberBox.Visibility == Visibility.Collapsed) {
				ShowNumberBox();
			} else {
				HideNumberBox();
			}
		}

		protected void NumberBox_LostFocus(object sender, RoutedEventArgs e) {
			HideNumberBox();
		}

		protected void NumberBox_TextChanged(object sender, TextChangedEventArgs e) {
			if (_ignoreChanges) return;

			ulong current = 0;
			try {
				current = ulong.Parse(NumberBox.Text);
			} catch { }

			var max = GetCombinationsNumber();
			if (current < 1) current = 1;
			if (current > max) current = max;
			var s = $"{current}";

			if (s != NumberBox.Text) {
				NumberBox.Text = s;
				return;
			}

			ApplyCombinationNumber(current);
		}

		protected void CancelButton_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		protected void SaveButton_Click(object sender, RoutedEventArgs e) {
			SaveSelection();
			Close();
		}

		#endregion
		#region logic

		private void UpdateNumberLabel() {
			var current = GetCurrentCombinationNumber();
			NumberLabel.Content = $"#{current}/{GetCombinationsNumber()}";
			NumberBox.Text = $"{current}";
		}

		private void ShowNumberBox() {
			NumberLabel.Content = "";

			NumberBox.Visibility = Visibility.Visible;
			NumberBox.Focus();
		}

		private void HideNumberBox() {
			UpdateNumberLabel();

			NumberBox.Visibility = Visibility.Collapsed;
		}

		private void ApplyCombinationNumber(ulong current) {
			_ignoreChanges = true;

			current -= 1;
			for (var i = 0; i < _optionsSelectors.Count; ++i) {
				var selector = _optionsSelectors[i];

				selector.SelectedIndex = (int)(current % (ulong)selector.Items.Count);
				current /= (ulong)selector.Items.Count;
			}

			_ignoreChanges = false;
		}

		protected ulong GetCurrentCombinationNumber() {
			ulong current = 0;

			for (var i = _optionsSelectors.Count - 1; i >= 0; --i) {
				var selector = _optionsSelectors[i];

				current *= (ulong)selector.Items.Count;
				current += (ulong)selector.SelectedIndex;
			}

			current += 1;
			return current;
		}

		protected string GetCurrentCombinationDescription() {
			var description = "";

			var selectorIndex = 0;
			foreach (var entry in _layout) {
				var entryType = (string)entry[0];
				if (entryType == "header") {
					description += "\n" + (string)entry[1];
				} else if (entryType == "separator") {
					description += "\n";
				} else if (entryType == "module") {
					var options = (JArray)entry[2];
					if (options.Count == 1) continue;

					description += "\n" + (string)entry[1] + ": " + ((ModuleOption)_optionsSelectors[selectorIndex].SelectedItem).Name;
					++selectorIndex;
				}
			}

			return description;
		}

		private ulong GetCombinationsNumber() {
			ulong totalCombinations = 1;

			for (var i = _optionsSelectors.Count - 1; i >= 0; --i) {
				var selector = _optionsSelectors[i];
				totalCombinations *= (ulong)selector.Items.Count;
			}

			return totalCombinations;
		}

		#endregion
	}
}
