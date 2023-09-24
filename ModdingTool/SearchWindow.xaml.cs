// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using ModdingTool.Structs;
using ModdingTool.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ModdingTool;

public partial class SearchWindow: Window {
	private List<Asset> _assets;
	private Dictionary<string, List<int>> _assetsByPath;
	private System.Action<string> _callback;
	private System.Action<string, System.Collections.IList> _contextMenuCallback;
	private ObservableCollection<SearchResult> _displayedResults = new();

	class SearchResult {
		public int AssetIndex { get; set; }
		public byte Span { get; set; }
		public ulong Id;
		public uint Size { get; set; }
		public string SizeFormatted { get => SizeFormat.FormatSize(Size); }

		public string Path { get; set; }
		public string Archive { get; set; }
		public string RefPath { get => $"{Span}/{Id:X016}"; }
	}

	public SearchWindow(List<Asset> assets, Dictionary<string, List<int>> assetsByPath, System.Action<string> callback, System.Action<string, System.Collections.IList> contextMenuCallback) {
		InitializeComponent();
		_assets = assets;
		_assetsByPath = assetsByPath;
		_callback = callback;
		_contextMenuCallback = contextMenuCallback;

		CommandBindings.Add(new CommandBinding(AssetsListContextMenu.ExtractAssetCommand, ContextMenu_ExtractAsset));
		CommandBindings.Add(new CommandBinding(AssetsListContextMenu.ExtractAssetToStageCommand, ContextMenu_ExtractAssetToStage));
		CommandBindings.Add(new CommandBinding(AssetsListContextMenu.ReplaceAssetCommand, ContextMenu_ReplaceAsset));
		CommandBindings.Add(new CommandBinding(AssetsListContextMenu.CopyPathCommand, ContextMenu_CopyPath));
		CommandBindings.Add(new CommandBinding(AssetsListContextMenu.CopyRefCommand, ContextMenu_CopyRef));

		SearchTextBox.Text = "";
		Search();
	}

	private void SearchTextBox_KeyUp(object sender, KeyEventArgs e) {
		if (e.Key == Key.Enter) {
			Search();
		}
	}

	private void SearchButton_Click(object sender, RoutedEventArgs e) {
		Search();
	}

	private void SearchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
		if (SearchResults.SelectedItems.Count != 1) return;
		if (SearchResults.SelectedItem == null) return;

		_callback((SearchResults.SelectedItem as SearchResult).RefPath);
	}

	private void Search() {
		_displayedResults.Clear();

		var search = Normalize(SearchTextBox.Text.Trim());
		var words = search.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);		
		
		if (words.Length > 0) {
			// search in fullpath
			var i = 0;
			foreach (var asset in _assets) {
				if (asset.FullPath != null && MatchesWords(Normalize(asset.FullPath), words)) {
					_displayedResults.Add(new SearchResult {
						AssetIndex = i,
						Span = asset.Span,
						Id = asset.Id,
						Size = asset.Size,
						Path = asset.FullPath,
						Archive = asset.Archive
					});
				}
				++i;
			}

			// search in fake paths (dirname + name)
			foreach (var path in _assetsByPath.Keys) {
				foreach (var assetIndex in _assetsByPath[path]) {
					var asset = _assets[assetIndex];
					if (asset.FullPath != null) continue;

					var fakepath = Path.Combine(path, asset.Name);
					if (MatchesWords(Normalize(fakepath), words)) {
						_displayedResults.Add(new SearchResult {
							AssetIndex = assetIndex,
							Span = asset.Span,
							Id = asset.Id,
							Size = asset.Size,
							Path = fakepath,
							Archive = asset.Archive
						});
					}
				}
			}
		}

		ResultsCount.Text = $"{_displayedResults.Count} results";
		SearchResults.ItemsSource = _displayedResults;
	}

	private static string Normalize(string text) {
		return text.Replace('\\', '/').ToLower();
	}

	private static bool MatchesWords(string path, IEnumerable<string> words) {
		foreach (var word in words) {
			if (!path.Contains(word)) return false;
		}
		return true;
	}

	#region context menu

	private void SearchResults_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
		var selected = SearchResults.SelectedItems.Count;
		AssetsListContextMenu.HandleContextMenuOpening(sender, e, selected);
	}

	private void ContextMenu_ExtractAsset(object sender, ExecutedRoutedEventArgs e) {
		_contextMenuCallback("ExtractAsset", GetSelectedAssets());
	}

	private void ContextMenu_ExtractAssetToStage(object sender, ExecutedRoutedEventArgs e) {
		_contextMenuCallback("ExtractAssetToStage", GetSelectedAssets());
	}

	private void ContextMenu_ReplaceAsset(object sender, ExecutedRoutedEventArgs e) {
		_contextMenuCallback("ReplaceAsset", GetSelectedAssets());
	}

	private void ContextMenu_CopyPath(object sender, ExecutedRoutedEventArgs e) {
		_contextMenuCallback("CopyPath", GetSelectedAssets());
	}

	private void ContextMenu_CopyRef(object sender, ExecutedRoutedEventArgs e) {
		_contextMenuCallback("CopyRef", GetSelectedAssets());
	}

	private List<Asset> GetSelectedAssets() {
		var result = new List<Asset>();
		foreach (var item in SearchResults.SelectedItems) {
			result.Add(_assets[(item as SearchResult).AssetIndex]);
		}
		return result;
	}

	#endregion
}
