using ModdingTool.Structs;
using ModdingTool.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ModdingTool;

public partial class SearchWindow: Window {
	private List<Asset> _assets;
	private Dictionary<string, List<int>> _assetsByPath;
	private ObservableCollection<SearchResult> _displayedResults = new();

	class SearchResult {
		public byte Span { get; set; }
		public ulong Id;
		public uint Size { get; set; }
		public string SizeFormatted { get => SizeFormat.FormatSize(Size); }

		public string Path { get; set; }
		public string Archive { get; set; }
		public string RefPath { get => $"{Span}/{Id:X016}"; }
	}

	public SearchWindow(List<Asset> assets, Dictionary<string, List<int>> assetsByPath) {
		InitializeComponent();
		_assets = assets;
		_assetsByPath = assetsByPath;

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

	private void Search() {
		_displayedResults.Clear();

		var search = Normalize(SearchTextBox.Text.Trim());
		var words = search.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);		
		
		if (words.Length > 0) {
			// search in fullpath
			foreach (var asset in _assets) {
				if (asset.FullPath != null && MatchesWords(Normalize(asset.FullPath), words)) {
					_displayedResults.Add(new SearchResult {
						Span = asset.Span,
						Id = asset.Id,
						Size = asset.Size,
						Path = asset.FullPath,
						Archive = asset.Archive
					});
				}
			}

			// search in fake paths (dirname + name)
			foreach (var path in _assetsByPath.Keys) {
				foreach (var assetIndex in _assetsByPath[path]) {
					var asset = _assets[assetIndex];
					if (asset.FullPath != null) continue;

					var fakepath = Path.Combine(path, asset.Name);
					if (MatchesWords(Normalize(fakepath), words)) {
						_displayedResults.Add(new SearchResult {
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
}
