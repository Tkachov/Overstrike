// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using Microsoft.WindowsAPICodePack.Dialogs;
using ModdingTool.Structs;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ModdingTool {
	public partial class PackStageWindow: Window {
		private bool _initializing = true;
		private Dictionary<Asset, string> _mainWindowReplacedAssets;
		private Dictionary<Asset, string> _mainWindowAddedAssets;

		private string _modName;
		private string _author;
		private string _gameId;
		private List<Game> _games = new();
		private ObservableCollection<AssetReplace> _assets = new();

		class Game {
			public string Name { get; set; }
			public string Id;
		}

		class AssetReplace {
			public Asset Asset;
			public bool IsNew = false;

			public string OriginalAssetName { get => (IsNew ? $"new ({Asset.RefPath})" : Asset.Name); }
			public string OriginalAssetNameToolTip { get => (Asset.FullPath == null ? "" : $"Path: {Asset.FullPath}\n") + $"ID: {Asset.Id:X016}\nSpan: {Asset.Span}\nArchive: {Asset.Archive}"; }

			public string ReplacingFileName { get; set; }
			public string ReplacingFileNameToolTip { get; set; }
		}

		public PackStageWindow(Dictionary<Asset, string> replacedAssets, Dictionary<Asset, string> addedAssets, TOCBase toc) {
			InitializeComponent();
			_initializing = false;

			MakeGamesSelector(toc);

			_mainWindowReplacedAssets = replacedAssets;
			_mainWindowAddedAssets = addedAssets;
			UpdateAssetsList();
		}

		#region applying state

		private void MakeGamesSelector(TOCBase toc) {
			// should be in sync with Overstrike.Games
			_games.Clear();
			_games.Add(new Game() { Name = "Marvel's Spider-Man Remastered", Id = "MSMR" });
			_games.Add(new Game() { Name = "Marvel's Spider-Man: Miles Morales", Id = "MM" });
			_games.Add(new Game() { Name = "Ratchet & Clank: Rift Apart", Id = "RCRA" });
			_games.Add(new Game() { Name = "Marvel's Spider-Man 2", Id = "MSM2" });
			_games.Add(new Game() { Name = "i30", Id = "i30" });
			_games.Add(new Game() { Name = "i33", Id = "i33" });

			GameComboBox.ItemsSource = new CompositeCollection {
				new CollectionContainer() { Collection = _games }
			};

			var selected = _games[0];
			if (toc is TOC_I29) {
				selected = _games[3];
			}

			_gameId = selected.Id;
			GameComboBox.SelectedItem = selected;
		}

		private void UpdateAssetsList() {
			_assets.Clear();

			foreach (var asset in _mainWindowReplacedAssets.Keys) {
				var path = _mainWindowReplacedAssets[asset];
				_assets.Add(new AssetReplace {
					Asset = asset,
					ReplacingFileName = Path.GetFileName(path),
					ReplacingFileNameToolTip = path
				});
			}

			foreach (var asset in _mainWindowAddedAssets.Keys) {
				var path = _mainWindowAddedAssets[asset];
				_assets.Add(new AssetReplace {
					Asset = asset,
					IsNew = true,
					ReplacingFileName = Path.GetFileName(path),
					ReplacingFileNameToolTip = path
				});
			}

			AssetsList.ItemsSource = _assets;
		}

		private void RefreshButton() {
			var isEmpty = (string s) => { return (s == null || s == ""); };

			SaveStageButton.IsEnabled = (!isEmpty(_modName) && !isEmpty(_author));
		}

		#endregion
		#region event handlers

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

		private void AssetsList_KeyUp(object sender, KeyEventArgs e) {
			if (e.Key == Key.Delete) {
				foreach (var item in AssetsList.SelectedItems) {
					var assetReplace = (AssetReplace)item;

					if (assetReplace.IsNew) {
						_mainWindowAddedAssets.Remove(assetReplace.Asset);
					} else {
						_mainWindowReplacedAssets.Remove(assetReplace.Asset);
					}
				}

				UpdateAssetsList();
			}
		}

		private void SaveStageButton_Click(object sender, RoutedEventArgs e) {
			var dialog = new CommonSaveFileDialog();
			dialog.Title = "Save .stage...";
			dialog.RestoreDirectory = true;
			dialog.Filters.Add(new CommonFileDialogFilter("Stage", "*.stage") { ShowExtensions = true });
			dialog.Filters.Add(new CommonFileDialogFilter("All files", "*") { ShowExtensions = true });
			dialog.DefaultFileName = "*.stage";

			if (dialog.ShowDialog() != CommonFileDialogResult.Ok) {
				return;
			}

			var tocHasTextureSections = (_gameId != "MSMR" && _gameId != "MM");
			if (_mainWindowAddedAssets.Count > 0 && tocHasTextureSections) {
				MessageBox.Show($"Warning: adding new .texture assets is not implemented.\n\nThe game might work incorrectly with these or even crash because of them.", "Warning", MessageBoxButton.OK);
			}

			var stageFileName = dialog.FileName;
			try {
				using var f = new FileStream(stageFileName, FileMode.Create, FileAccess.Write, FileShare.None);
				using var zip = new ZipArchive(f, ZipArchiveMode.Create);

				void WriteAssets(Dictionary<Asset, string> assets) {
					foreach (var asset in assets.Keys) {
						var path = assets[asset];
						var bytes = File.ReadAllBytes(path);

						var assetPath = asset.RefPath;
						if (asset.FullPath != null)
							assetPath = $"{asset.Span}/" + DAT1.Utils.Normalize(asset.FullPath);

						var entry = zip.CreateEntry(assetPath);
						using var ef = entry.Open();
						ef.Write(bytes, 0, bytes.Length);
					}
				}

				WriteAssets(_mainWindowReplacedAssets);
				WriteAssets(_mainWindowAddedAssets);

				{					
					JObject j = new() {
						["game"] = _gameId,
						["name"] = _modName,
						["author"] = _author,
						["format_version"] = 2
					};

					var text = j.ToString();
					var data = Encoding.UTF8.GetBytes(text);

					var entry = zip.CreateEntry("info.json");
					using var ef = entry.Open();
					ef.Write(data, 0, data.Length);
				}
			} catch {
				MessageBox.Show($"Error: failed to write '{stageFileName}'!", "Error", MessageBoxButton.OK);
				return;
			}

			Close();
		}

		#endregion
	}
}
