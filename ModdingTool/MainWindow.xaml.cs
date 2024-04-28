// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using Microsoft.WindowsAPICodePack.Dialogs;
using ModdingTool.Structs;
using ModdingTool.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ModdingTool {
	public partial class MainWindow: Window {
		// tick
		private Thread _tickThread;
		private List<Thread> _taskThreads = new();

		// settings
		private List<string> _recentPaths = new();

		// loaded data
		private TOCBase? _toc = null;
		private List<Asset> _assets = new();
		private Dictionary<string, List<int>> _assetsByPath = new();
		private ObservableCollection<Asset> _displayedAssetList = new();

		// replaced data
		private Dictionary<Asset, string> _replacedAssets = new();
		private Dictionary<Asset, string> _addedAssets = new();

		// ui
		private SearchWindow _searchWindow = null;
		private HashToolWindow _hashToolWindow = null;

		public MainWindow() {
			InitializeComponent();
			CommandBindings.Add(new CommandBinding(AssetsListContextMenu.ExtractAssetCommand, ContextMenu_ExtractAsset));
			CommandBindings.Add(new CommandBinding(AssetsListContextMenu.ExtractAssetToStageCommand, ContextMenu_ExtractAssetToStage));
			CommandBindings.Add(new CommandBinding(AssetsListContextMenu.ReplaceAssetCommand, ContextMenu_ReplaceAsset));
			CommandBindings.Add(new CommandBinding(AssetsListContextMenu.CopyPathCommand, ContextMenu_CopyPath));
			CommandBindings.Add(new CommandBinding(AssetsListContextMenu.CopyRefCommand, ContextMenu_CopyRef));

			StartTickThread();
			LoadSettings();

			if (_recentPaths.Count > 0 ) {
				StartLoadTOCThread(_recentPaths[0]);
			}
		}

		#region tick

		private void StartTickThread() {
			_tickThread = new Thread(TickThread);
			_tickThread.Start();
		}

		private void TickThread() {
			try {
				while (true) {
					Thread.Sleep(16);
					Tick();
				}
			} catch {}
		}

		private void Tick() {
			List<Thread> threadsToRemove = new();
			foreach (var thread in _taskThreads) {
				if (!thread.IsAlive) {
					threadsToRemove.Add(thread);
				}
			}
			foreach (Thread thread in threadsToRemove) {
				_taskThreads.Remove(thread);
			}

			bool hasTasks = _taskThreads.Count > 0;
			Dispatcher.Invoke(() => {
				Overlay.Visibility = (hasTasks ? Visibility.Visible : Visibility.Collapsed);
			});
		}

		#endregion
		#region settings

		private void LoadSettings() {
			LoadRecentTxt();
		}

		private void LoadRecentTxt() {
			_recentPaths.Clear();

			var fn = "recent.txt";
			if (File.Exists(fn)) {
				foreach (var line in File.ReadLines(fn)) {
					if (line == null) continue;

					var l = line.Trim();
					if (l != "") _recentPaths.Add(l);
				}
			}
		}

		private void SaveRecentTxt() {
			using var f = File.OpenWrite("recent.txt");
			using var w = new StreamWriter(f);
			foreach (var l in _recentPaths) {
				w.WriteLine(l);
			}
		}

		#endregion
		#region load toc

		private void StartLoadTOCThread(string path) {
			if (File.Exists(path)) {
				path = Path.GetDirectoryName(path);
			}
			if (!Directory.Exists(path)) {
				return;
			}

			var tocPath = Path.Combine(path, "toc.BAK");
			if (!File.Exists(tocPath)) {
				tocPath = Path.Combine(path, "toc");
			}
			if (!File.Exists(tocPath)) {
				return;
			}

			_recentPaths.Remove(path);
			_recentPaths.Insert(0, path);
			SaveRecentTxt();

			//

			Thread thread = new(() => LoadTOC(tocPath));
			_taskThreads.Add(thread);
			thread.Start();
		}

		class TreeNode {
			public Dictionary<string, TreeNode> Children = new();
			public TreeNode() {}
		}

		private void LoadTOC(string path) {
			Dispatcher.Invoke(() => {
				OverlayHeaderLabel.Text = "Loading 'toc'...";
				OverlayOperationLabel.Text = "-";
			});

			// toc

			_toc = LoadTOCFile(path);
			if (_toc == null) {
				return;
			}

			var archiveNames = new List<string>();
			for (uint i = 0; i < _toc.GetArchivesCount(); ++i) {
				var fn = _toc.GetArchiveFilename(i);

				if (_toc is TOC_I29 && fn.StartsWith("d\\")) { // for RCRA to look a bit better
					fn = fn.Substring(2);
				}

				archiveNames.Add(fn);
			}

			_assets.Clear();
			_replacedAssets.Clear();

			var progress = 0;
			var progressTotal = _toc.AssetIdsSection.Values.Count;
			byte spanIndex = 0;
			foreach (var span in _toc.SpansSection.Values) {
				for (int i = (int)span.AssetIndex; i < span.AssetIndex + span.Count; ++i) {
					var hasHeader = (spanIndex % 8 == 0);
					if (hasHeader && _toc is TOC_I29) {
						hasHeader = (((TOC_I29)_toc).SizesSection.Values[i].HeaderOffset != -1);
					}

					_assets.Add(new Asset {
						Span = spanIndex,
						Id = _toc.AssetIdsSection.Values[i],
						Size = (uint)_toc.GetSizeInArchiveByAssetIndex(i),
						HasHeader = hasHeader,
						Name = "",
						Archive = archiveNames[(int)_toc.GetArchiveIndexByAssetIndex(i)]
					});

					++progress;
					if (progress % 1000 == 0) {
						Dispatcher.Invoke(() => {
							OverlayHeaderLabel.Text = "Loading 'toc'...";
							OverlayOperationLabel.Text = $"{progress}/{progressTotal} assets";
						});
					}
				}
				++spanIndex;
			}

			Dispatcher.Invoke(() => {
				OverlayOperationLabel.Text = $"-";
			});

			// hashes

			var appdir = AppDomain.CurrentDomain.BaseDirectory;
			var hashes_fn = Path.Combine(appdir, "hashes.txt");
			var knownHashes = new Dictionary<ulong, string>();
			if (File.Exists(hashes_fn)) {
				var lines = File.ReadLines(hashes_fn);
				progress = 0;
				progressTotal = lines.Count();
				foreach (var line in lines) {
					try {
						var firstComma = line.IndexOf(',');
						if (firstComma == -1) continue;

						var lastComma = line.LastIndexOf(',');
						var assetPath = (lastComma == -1 ? line.Substring(firstComma + 1) : line.Substring(firstComma + 1, lastComma - firstComma - 1));
						var assetId = ulong.Parse(line.Substring(0, firstComma), NumberStyles.HexNumber);

						if (assetPath.Trim().Length > 0) {
							knownHashes.Add(assetId, assetPath);
						}
					} catch { }

					++progress;
					if (progress % 1000 == 0) {
						Dispatcher.Invoke(() => {
							OverlayHeaderLabel.Text = "Loading 'hashes.txt'...";
							OverlayOperationLabel.Text = $"{progress}/{progressTotal} hashes";
						});
					}
				}
			}

			Dispatcher.Invoke(() => {
				OverlayOperationLabel.Text = $"-";
			});

			// tree

			_assetsByPath.Clear();
			TreeNode root = new();
			root.Children["[UNKNOWN]"] = new();
			root.Children["[WEM]"] = new();

			void AddPath(string dir, int assetIndex, bool makeFullPath = false) {
				if (dir == null) dir = "";
				if (makeFullPath)
					_assets[assetIndex].FullPath = Path.Combine(dir, _assets[assetIndex].Name);

				if (dir == "") dir = "/";
				var parts = dir.Split("\\");
				var currentNode = root;
				foreach (var part in parts) {
					if (!currentNode.Children.ContainsKey(part)) {
						currentNode.Children.Add(part, new());
					}
					currentNode = currentNode.Children[part];
				}

				if (!_assetsByPath.ContainsKey(dir)) {
					_assetsByPath[dir] = new();
				}
				_assetsByPath[dir].Add(assetIndex);
			};

			// tree: named assets

			progress = 0;
			progressTotal = _assets.Count;

			var usedHashes = new Dictionary<ulong, string>();
			for (var i = 0; i < _assets.Count; ++i) {
				var asset = _assets[i];
				var assetId = asset.Id;
				if (knownHashes.ContainsKey(assetId)) {
					var assetPath = DAT1.Utils.Normalize(knownHashes[assetId]);
					usedHashes[assetId] = assetPath;
					asset.Name = Path.GetFileName(assetPath);
					AddPath(Path.GetDirectoryName(assetPath), i, true);
				}

				++progress;
				if (progress % 1000 == 0) {
					Dispatcher.Invoke(() => {
						OverlayHeaderLabel.Text = "Building tree...";
						OverlayOperationLabel.Text = $"{progress}/{progressTotal} assets";
					});
				}
			}

			Dispatcher.Invoke(() => {				
				OverlayOperationLabel.Text = $"-";
			});

			// tree: other assets

			var unknown = root.Children["[UNKNOWN]"];
			var wems = root.Children["[WEM]"];
			
			for (var i = 0; i < _assets.Count; ++i) {
				var asset = _assets[i];
				if (asset.Name != "") continue;

				var assetId = asset.Id;
				var isWem = ((assetId & 0xFFFFFFFF00000000) == 0xE000000000000000);

				if (isWem) {
					var wemNumber = assetId & 0xFFFFFFFF;
					asset.Name = $"{wemNumber}.wem";
					AddPath($"[WEM]\\{asset.Archive}", i);
				} else {
					asset.Name = $"{assetId:X016}";
					AddPath($"[UNKNOWN]\\{asset.Archive}", i);
				}
			}

			// build the UI

			Dispatcher.Invoke(() => {
				OverlayHeaderLabel.Text = "Building tree...";
				OverlayOperationLabel.Text = $"-";

				void Traverse(TreeNode n, ItemCollection i) {
					var keysSorted = n.Children.Keys.ToList();
					keysSorted.Sort((x, y) => {
						if (x == y) return 0;
						if (x == null || x == "") return 1;
						if (y == null || y == "") return -1;
						
						if (x[0] == '/' || x[0] == '[') {
							if (y[0] != '/' && y[0] != '[') return 1;
							if (x[0] == y[0])
								return x.CompareTo(y);
							return x[0] - y[0];
						}

						if (y[0] == '/' || y[0] == '[') return -1;

						return x.CompareTo(y);
					});
					foreach (var k in keysSorted) {
						var i2 = new TreeViewItem() {
							Header = k
						};

						Traverse(n.Children[k], i2.Items);

						i.Add(i2);
					}
				};

				Folders.Items.Clear();
				Traverse(root, Folders.Items);

				ShowAssetsFromFolder("", Folders.Items.Count);
			});
		}

		private static TOCBase? LoadTOCFile(string tocPath) {
			TOC_I29 toc_i29 = new();
			if (toc_i29.Load(tocPath)) {
				return toc_i29;
			}

			TOC_I20 toc_i20 = new();
			if (toc_i20.Load(tocPath)) {
				return toc_i20;
			}

			return null;
		}

		#endregion
		#region common

		private void ShowAssetsFromFolder(string path, int dirs) {
			_displayedAssetList.Clear();
			List<Asset> assetList = new();

			if (_assetsByPath.ContainsKey(path)) {
				foreach (var index in _assetsByPath[path]) {
					assetList.Add(_assets[index]);
				}

				assetList.Sort((x, y) => {
					if (x.Name == y.Name) {
						return x.Span - y.Span;
					}
					return x.Name.CompareTo(y.Name);
				});
			}

			foreach (var asset in assetList) {
				_displayedAssetList.Add(asset);
			}

			AssetsList.ItemsSource = _displayedAssetList;

			// update status bar

			CurrentPath.Text = $"Selected directory: {path}";
			var hint = "";
			if (dirs > 0) {
				hint = $"{dirs} director" + (dirs > 1 ? "ies" : "y");
			}

			if (hint == "" || assetList.Count > 0) {
				if (hint != "") hint += ", ";
				hint += $"{assetList.Count} asset" + (assetList.Count == 1 ? "" : "s");
			}
			DirectoryDetails.Text = hint;
		}

		private void ShowAssetsFromFolder(string path) {
			var parts = path.Split('\\');
			TreeViewItem currentNode = null;
			var currentItems = Folders.Items;

			var actualPath = "";
			foreach (var part in parts) {
				var found = false;
				foreach (TreeViewItem item in currentItems) {
					if ((string)(item.Header) == part) {
						currentNode = item;
						currentItems = item.Items;
						found = true;
						break;
					}
				}

				if (found) { actualPath = Path.Combine(actualPath, part); } else break;
			}

			if (path != "/" && actualPath == "") {
				ShowAssetsFromFolder("/");
				return;
			}

			if (currentNode != null) {
				currentNode.IsSelected = true;
				currentNode.BringIntoView();
			}
			ShowAssetsFromFolder(actualPath, currentItems.Count);
		}

		private void JumpTo(string path) {
			string folderToOpen = null;
			bool openAssetById = false;
			byte assetSpanToOpen = 0;
			ulong assetIdToOpen = 0;
			bool openAssetByName = false;
			string assetNameToOpen = null;

			if (Regex.IsMatch(path, "^[0-9]+/[0-9a-fA-F]{16}$")) { // ref
				var i = path.IndexOf('/');
				var span = path.Substring(0, i);
				var assetId = path.Substring(++i);

				try {
					var spanIndex = byte.Parse(span);
					var id = ulong.Parse(assetId, NumberStyles.HexNumber);
					var assetIndex = _toc.FindAssetIndex(spanIndex, id);
					if (assetIndex != -1) {
						var asset = _assets[assetIndex];

						folderToOpen = Path.GetDirectoryName(asset.FullPath);
						openAssetById = true;
						assetSpanToOpen = spanIndex;
						assetIdToOpen = id;

						if (folderToOpen == null) {
							foreach (var dirname in _assetsByPath.Keys) {
								if (_assetsByPath[dirname].Contains(assetIndex)) {
									folderToOpen = dirname;
									break;
								}
							}
						}
					}
				} catch {}
			} else {
				if (path != "/") path = path.Replace('/', '\\');

				folderToOpen = path;
				openAssetByName = true;
				assetNameToOpen = Path.GetFileName(path);
			}

			if (folderToOpen != null) {
				ShowAssetsFromFolder(folderToOpen);

				if (openAssetById) {
					foreach (Asset assetItem in AssetsList.Items) {
						if (assetItem.Span == assetSpanToOpen && assetItem.Id == assetIdToOpen) {
							AssetsList.SelectedItem = assetItem;
							AssetsList.ScrollIntoView(assetItem);
							break;
						}
					}
				} else if (openAssetByName) {
					foreach (Asset assetItem in AssetsList.Items) {
						if (assetItem.Name == assetNameToOpen) {
							AssetsList.SelectedItem = assetItem;
							AssetsList.ScrollIntoView(assetItem);
							break;
						}
					}
				}
			}
		}

		private void ExtractOneAssetDialog(Asset asset) {
			CommonSaveFileDialog dialog = new();
			dialog.Title = "Extract asset...";
			dialog.RestoreDirectory = true;
			dialog.Filters.Add(new CommonFileDialogFilter("All files", "*") { ShowExtensions = true });
			dialog.DefaultFileName = asset.Name;

			if (dialog.ShowDialog() != CommonFileDialogResult.Ok) {
				return;
			}

			ExtractAsset(asset, dialog.FileName);
		}

		private void ExtractMultipleAssetsDialog(System.Collections.IList assets) {
			CommonOpenFileDialog dialog = new();
			dialog.Title = "Select directory to extract assets to...";
			dialog.IsFolderPicker = true;
			dialog.RestoreDirectory = true;

			var result = dialog.ShowDialog();
			Activate();

			if (result != CommonFileDialogResult.Ok) {
				return;
			}

			var path = dialog.FileName;
			if (!Directory.Exists(path)) {
				return;
			}

			foreach (var item in assets) {
				var asset = (Asset)item;
				ExtractAsset(asset, Path.Combine(path, asset.Name));
			}
		}

		private void ExtractAssetsToStageDialog(System.Collections.IList assets) {
			var window = new StageSelector();
			window.ShowDialog();

			if (window.Stage == null) return;

			var cwd = Directory.GetCurrentDirectory();
			var path = Path.Combine(cwd, "stages");
			var stagePath = Path.Combine(path, window.Stage);
			if (!Directory.Exists(stagePath)) Directory.CreateDirectory(stagePath);

			foreach (var item in assets) {
				var asset = (Asset)item;

				var dirname = Path.Combine(stagePath, $"{asset.Span}");
				var assetPath = Path.Combine(dirname, $"{asset.Id:X016}");
				if (asset.FullPath != null) {
					assetPath = Path.Combine(stagePath, $"{asset.Span}", asset.FullPath);
					dirname = Path.GetDirectoryName(assetPath);
				}

				if (!Directory.Exists(dirname)) Directory.CreateDirectory(dirname);
				ExtractAsset(asset, assetPath);
			}
		}

		private void ExtractAsset(Asset asset, string path) {
			try {
				var bytes = _toc.GetAssetBytes(asset.Span, asset.Id);
				File.WriteAllBytes(path, bytes);
			} catch {} // TODO: notify user of failure somehow
		}

		private void ExtractFolder(string folder, string path) {
			Dispatcher.Invoke(() => {
				OverlayHeaderLabel.Text = "Scanning tree...";
				OverlayOperationLabel.Text = "-";
			});

			Dictionary<string, List<int>> matchingPaths = new();
			var foundAssetsTotal = 0;
			foreach (var _path in _assetsByPath.Keys) {
				if (_path.StartsWith(folder)) {
					var assets = _assetsByPath[_path];
					matchingPaths.Add(Path.GetRelativePath(folder, _path), assets);
					foundAssetsTotal += assets.Count;

					Dispatcher.Invoke(() => {
						OverlayOperationLabel.Text = folder;
					});
				}
			}

			// remember which assets have the same name

			Dispatcher.Invoke(() => {
				OverlayHeaderLabel.Text = "Scanning tree...";
				OverlayOperationLabel.Text = "-";
			});

			Dictionary<ulong, int> countById = new();
			foreach (var suffix in matchingPaths.Keys) {
				foreach (var assetIndex in matchingPaths[suffix]) {
					var asset = _assets[assetIndex];
					countById.Update(asset.Id, 1, (int mapValue, int updateValue) => { return mapValue + updateValue; });
				}
			}

			// extract

			var progress = 0;
			var progressTotal = foundAssetsTotal;
			foreach (var suffix in matchingPaths.Keys) {
				var dirname = Path.Combine(path, suffix);
				if (!Directory.Exists(dirname)) Directory.CreateDirectory(dirname);

				foreach (var assetIndex in matchingPaths[suffix]) {
					var asset = _assets[assetIndex];
					Dispatcher.Invoke(() => {
						OverlayHeaderLabel.Text = $"Extracting assets ({progress}/{progressTotal} done)...";
						OverlayOperationLabel.Text = $"'{asset.Name}'";
					});

					var assetPath = Path.Combine(dirname, asset.Name);
					if (countById[asset.Id] > 1) {
						assetPath = Path.Combine(dirname, $"{asset.Name}.{asset.Span}");
					}
					ExtractAsset(asset, assetPath);
					++progress;
				}
			}
		}

		private void ExtractFolderToStage(string folder, string stage) {
			var cwd = Directory.GetCurrentDirectory();
			var path = Path.Combine(cwd, "stages");
			var stagePath = Path.Combine(path, stage);
			if (!Directory.Exists(stagePath)) Directory.CreateDirectory(stagePath);

			Dispatcher.Invoke(() => {
				OverlayHeaderLabel.Text = "Scanning tree...";
				OverlayOperationLabel.Text = "-";
			});

			Dictionary<string, List<int>> matchingPaths = new();
			var foundAssetsTotal = 0;
			foreach (var _path in _assetsByPath.Keys) {
				if (_path.StartsWith(folder)) {
					var assets = _assetsByPath[_path];
					matchingPaths.Add(_path, assets);
					foundAssetsTotal += assets.Count;

					Dispatcher.Invoke(() => {
						OverlayOperationLabel.Text = folder;
					});
				}
			}

			// extract

			var progress = 0;
			var progressTotal = foundAssetsTotal;
			foreach (var suffix in matchingPaths.Keys) {
				foreach (var assetIndex in matchingPaths[suffix]) {
					var asset = _assets[assetIndex];
					Dispatcher.Invoke(() => {
						OverlayHeaderLabel.Text = $"Extracting assets ({progress}/{progressTotal} done)...";
						OverlayOperationLabel.Text = $"'{asset.Name}'";
					});

					var dirname = Path.Combine(stagePath, $"{asset.Span}", suffix);
					var assetPath = Path.Combine(dirname, asset.Name);
					if (asset.FullPath == null) {
						dirname = Path.Combine(stagePath, $"{asset.Span}");
						assetPath = Path.Combine(dirname, $"{asset.Id:X016}");
					}

					if (!Directory.Exists(dirname)) Directory.CreateDirectory(dirname);

					ExtractAsset(asset, assetPath);
					++progress;
				}
			}
		}

		private void CloseSearchWindow() {
			if (_searchWindow != null) {
				_searchWindow.Close();
			}
		}

		private void CloseHashToolWindow() {
			if (_hashToolWindow != null) {
				_hashToolWindow.Close();
			}
		}

		#endregion
		#region event handlers

		#region menu

		private void File_LoadToc_Click(object sender, RoutedEventArgs e) {
			CommonOpenFileDialog dialog = new CommonOpenFileDialog();
			dialog.Title = "Select 'toc' to load...";
			dialog.Multiselect = false;
			dialog.RestoreDirectory = true;
			dialog.Filters.Add(new CommonFileDialogFilter("All files", "*") { ShowExtensions = true });

			if (dialog.ShowDialog() != CommonFileDialogResult.Ok) {
				return;
			}

			CloseSearchWindow();
			StartLoadTOCThread(dialog.FileName);
		}

		private void File_SubmenuOpened(object sender, RoutedEventArgs e) {
			File_LoadRecent.Visibility = (_recentPaths.Count > 0 ? Visibility.Visible : Visibility.Collapsed);

			void UpdateItem(MenuItem item, int index) {
				item.Visibility = (_recentPaths.Count > index ? Visibility.Visible : Visibility.Collapsed);
				item.Header = (_recentPaths.Count > index ? _recentPaths[index] : "").Replace("_", "__");
			};

			UpdateItem(File_LoadRecent1, 0);
			UpdateItem(File_LoadRecent2, 1);
			UpdateItem(File_LoadRecent3, 2);
			UpdateItem(File_LoadRecent4, 3);
			UpdateItem(File_LoadRecent5, 4);
		}

		private void File_LoadRecentItem_Click(object sender, RoutedEventArgs e) {
			bool CheckItem(MenuItem item, int index) {
				if (sender == item) {
					if (_recentPaths.Count > index) {
						CloseSearchWindow();
						StartLoadTOCThread(_recentPaths[index]);
					}
					return true;
				}
				return false;
			};

			if (CheckItem(File_LoadRecent1, 0)) {}
			else if (CheckItem(File_LoadRecent2, 1)) {}
			else if (CheckItem(File_LoadRecent3, 2)) {}
			else if (CheckItem(File_LoadRecent4, 3)) {}
			else if (CheckItem(File_LoadRecent5, 4)) {}
		}

		private void Search_Search_Click(object sender, RoutedEventArgs e) {
			if (_searchWindow == null) {
				_searchWindow = new SearchWindow(_assets, _assetsByPath, JumpTo, AssetsListContextMenuClicked);
				_searchWindow.Closed += (object? sender, EventArgs e) => {
					_searchWindow = null;
				};
				_searchWindow.Show();
			} else {
				_searchWindow.Focus();
			}
		}

		private void Search_JumpTo_Click(object sender, RoutedEventArgs e) {
			var window = new JumpToWindow();
			window.ShowDialog();

			if (!window.Jumped) return;
			JumpTo(window.Path.Trim());
		}

		private void Mod_SubmenuOpened(object sender, RoutedEventArgs e) {
			Mod_ReplacedItemsCount.Header = $"{_replacedAssets.Count} replaced, {_addedAssets.Count} new";

			Mod_ClearReplaced.IsEnabled = (_replacedAssets.Count + _addedAssets.Count > 0);
			Mod_ReplaceAssetsFromStage.IsEnabled = StagesExist();
		}

		private bool StagesExist() {
			var cwd = Directory.GetCurrentDirectory();
			var path = Path.Combine(cwd, "stages");
			if (Directory.Exists(path)) {
				var dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
				return (dirs.Length > 0);
			}
			return false;
		}


		private void Mod_ClearReplaced_Click(object sender, RoutedEventArgs e) {
			var result = MessageBox.Show("Are you sure?", "Clear all replaced and added files", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes) {
				_replacedAssets.Clear();
				_addedAssets.Clear();
			}
		}

		private void Mod_ReplaceAssetsFromStage_Click(object sender, RoutedEventArgs e) {
			var window = new StageSelector();
			window.OnlyExisting = true;
			window.ShowDialog();

			if (window.Stage == null) return;

			var cwd = Directory.GetCurrentDirectory();
			var path = Path.Combine(cwd, "stages");
			var stagePath = Path.Combine(path, window.Stage);

			for (var spanIndex = 0; spanIndex < 256; ++spanIndex) {
				var spanDir = Path.Combine(stagePath, $"{spanIndex}");
				if (!Directory.Exists(spanDir)) continue;

				var files = Directory.GetFiles(spanDir, "*", SearchOption.AllDirectories);
				foreach (var file in files) {
					var relpath = Path.GetRelativePath(spanDir, file);
					string fullpath = null;
					ulong assetId;
					if (Regex.IsMatch(relpath, "^[0-9A-Fa-f]{16}$")) {
						assetId = ulong.Parse(relpath, NumberStyles.HexNumber);
					} else {
						assetId = CRC64.Hash(relpath);
						fullpath = relpath;
					}

					var assetIndex = _toc.FindAssetIndex((byte)spanIndex, assetId);
					if (assetIndex != -1) {
						var asset = _assets[assetIndex];
						_replacedAssets.Set(asset, file);
						continue;
					}

					// record to _addedAssets, updating the record if it's already present
					Asset newAsset = null;

					foreach (var addedAsset in _addedAssets.Keys) {
						if (addedAsset.Span == spanIndex && addedAsset.Id == assetId) {
							newAsset = addedAsset;
							break;
						}
					}

					var adding = (newAsset == null);
					if (adding) newAsset = new Asset();

					newAsset.Span = (byte)spanIndex;
					newAsset.Id = assetId;
					newAsset.Size = 0; // TODO?
					newAsset.HasHeader = true;
					newAsset.Name = Path.GetFileName(relpath);
					newAsset.Archive = "-";
					newAsset.FullPath = fullpath;
													
					if (adding) {
						_addedAssets.Add(newAsset, file);
					} else {
						_addedAssets.Set(newAsset, file);
					}
				}
			}
		}

		private void Mod_CreateFromReplaced_Click(object sender, RoutedEventArgs e) {
			var window = new PackStageWindow(_replacedAssets, _addedAssets, _toc);
			window.ShowDialog();
		}

		private void Tools_CalculateHash_Click(object sender, RoutedEventArgs e) {
			if (_hashToolWindow == null) {
				_hashToolWindow = new HashToolWindow();
				_hashToolWindow.Closed += (object? sender, EventArgs e) => {
					_hashToolWindow = null;
				};
				_hashToolWindow.Show();
			} else {
				_hashToolWindow.Focus();
			}
		}

		#endregion
		#region folders view

		private void Folders_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			if (Folders.SelectedItem == null) return;

			var path = GetSelectedFolderPath();
			ShowAssetsFromFolder(path, ((TreeViewItem)Folders.SelectedItem).Items.Count);
		}

		private void Folders_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
			var element = (DependencyObject)e.OriginalSource;
			while (element != null && !(element is TreeViewItem))
				element = VisualTreeHelper.GetParent(element);

			if (element != null && element is TreeViewItem) {
				var treeItem = (TreeViewItem)element;
				treeItem.Focus();
				treeItem.IsSelected = true;
			} else {
				e.Handled = true; // don't show the menu if it wasn't tree item clicked
			}
		}

		private void FoldersMenu_ExtractAssets_Click(object sender, RoutedEventArgs e) {
			CommonOpenFileDialog dialog = new();
			dialog.Title = "Select directory to extract assets to...";
			dialog.IsFolderPicker = true;
			dialog.RestoreDirectory = true;

			var result = dialog.ShowDialog();
			Activate();

			if (result != CommonFileDialogResult.Ok) {
				return;
			}

			var path = dialog.FileName;
			if (!Directory.Exists(path)) {
				return;
			}

			//

			var folder = GetSelectedFolderPath();

			Thread thread = new(() => ExtractFolder(folder, path));
			_taskThreads.Add(thread);
			thread.Start();
		}

		private void FoldersMenu_ExtractAssetsToStage_Click(object sender, RoutedEventArgs e) {
			var window = new StageSelector();
			window.ShowDialog();

			if (window.Stage == null) return;

			//

			var folder = GetSelectedFolderPath();

			Thread thread = new(() => ExtractFolderToStage(folder, window.Stage));
			_taskThreads.Add(thread);
			thread.Start();
		}

		private void FoldersMenu_CopyPath_Click(object sender, RoutedEventArgs e) {
			var path = GetSelectedFolderPath();
			Clipboard.SetText(path);
		}

		private string GetSelectedFolderPath() {
			string path = "";
			var selection = Folders.SelectedItem;
			while (selection != null) {
				string name = (string)((TreeViewItem)selection).Header;

				if (path != "")
					path = name + "\\" + path;
				else
					path = name;

				selection = ((TreeViewItem)selection).Parent;
				if (selection is TreeView) break;
			}
			return path;
		}

		#endregion
		#region assets list

		private void AssetsList_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
			var selected = AssetsList.SelectedItems.Count;
			AssetsListContextMenu.HandleContextMenuOpening(sender, e, selected);
		}

		// command handlers

		private void ContextMenu_ExtractAsset(object sender, ExecutedRoutedEventArgs e) {
			AssetsListContextMenuClicked("ExtractAsset", AssetsList.SelectedItems);
		}

		private void ContextMenu_ExtractAssetToStage(object sender, ExecutedRoutedEventArgs e) {
			AssetsListContextMenuClicked("ExtractAssetToStage", AssetsList.SelectedItems);
		}

		private void ContextMenu_ReplaceAsset(object sender, ExecutedRoutedEventArgs e) {
			AssetsListContextMenuClicked("ReplaceAsset", AssetsList.SelectedItems);
		}

		private void ContextMenu_CopyPath(object sender, ExecutedRoutedEventArgs e) {
			AssetsListContextMenuClicked("CopyPath", AssetsList.SelectedItems);
		}

		private void ContextMenu_CopyRef(object sender, ExecutedRoutedEventArgs e) {
			AssetsListContextMenuClicked("CopyRef", AssetsList.SelectedItems);
		}

		// common handler (also used by SearchWindow)

		private void AssetsListContextMenuClicked(string item, System.Collections.IList selectedAssets) {
			switch (item) {
				case "ExtractAsset": ExtractAssets(selectedAssets); break;
				case "ExtractAssetToStage": ExtractAssetsToStage(selectedAssets); break;
				case "ReplaceAsset": ReplaceAsset(selectedAssets); break;
				case "CopyPath": CopyPath(selectedAssets); break;
				case "CopyRef": CopyRef(selectedAssets); break;
			}
		}

		// actual logic

		private void ExtractAssets(System.Collections.IList assets) {
			var selected = assets.Count;
			if (selected < 1) return;

			if (selected == 1) ExtractOneAssetDialog((Asset)assets[0]);
			else ExtractMultipleAssetsDialog(assets);
		}

		private void ExtractAssetsToStage(System.Collections.IList assets) {
			var selected = assets.Count;
			if (selected < 1) return;

			ExtractAssetsToStageDialog(assets);
		}

		private void ReplaceAsset(System.Collections.IList assets) {
			var selected = assets.Count;
			if (selected != 1) return;

			CommonOpenFileDialog dialog = new CommonOpenFileDialog();
			dialog.Title = "Select file to replace asset with...";
			dialog.Multiselect = false;
			dialog.RestoreDirectory = true;
			dialog.Filters.Add(new CommonFileDialogFilter("All files", "*") { ShowExtensions = true });

			if (dialog.ShowDialog() != CommonFileDialogResult.Ok) {
				return;
			}

			var asset = (Asset)assets[0];
			var path = dialog.FileName;
			_replacedAssets.Set(asset, path);
		}

		private static void CopyPath(System.Collections.IList assets) {
			var selected = assets.Count;
			if (selected < 1) return;

			var paths = "";
			foreach (var item in assets) {
				var asset = (Asset)item;
				var path = asset.FullPath ?? asset.RefPath;
				paths += $"{path}\n";
			}
			Clipboard.SetText(paths);
		}

		private static void CopyRef(System.Collections.IList assets) {
			var selected = assets.Count;
			if (selected < 1) return;

			var refs = "";
			foreach (var asset in assets) {
				refs += $"{(asset as Asset).RefPath}\n";
			}
			Clipboard.SetText(refs);
		}

		#endregion

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			CloseSearchWindow();
			CloseHashToolWindow();
		}

		#endregion
	}
}
