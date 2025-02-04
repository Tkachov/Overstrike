// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ModdingTool {
	public partial class App: Application {		
	}

	public static class AssetsListContextMenu {
		public static readonly RoutedUICommand ExtractAssetCommand = new("Extract Asset", "ExtractAssetCommand", typeof(AssetsListContextMenu));
		public static readonly RoutedUICommand ExtractAssetToStageCommand = new("Extract Asset to Stage", "ExtractAssetToStageCommand", typeof(AssetsListContextMenu));
		public static readonly RoutedUICommand ReplaceAssetCommand = new("Replace Asset", "ReplaceAssetCommand", typeof(AssetsListContextMenu));
		public static readonly RoutedUICommand ReplaceAssetsCommand = new("Replace Assets", "ReplaceAssetsCommand", typeof(AssetsListContextMenu));
		public static readonly RoutedUICommand CopyPathCommand = new("Copy Path", "CopyPathCommand", typeof(AssetsListContextMenu));
		public static readonly RoutedUICommand CopyRefCommand = new("Copy Ref", "CopyRefCommand", typeof(AssetsListContextMenu));

		public static MenuItem SelectedItemsCount => GetMenuItem("AssetsListContextMenu", "SelectedItemsCount");
		public static MenuItem ExtractAsset => GetMenuItem("AssetsListContextMenu", "ExtractAsset");
		public static MenuItem ExtractAssetToStage => GetMenuItem("AssetsListContextMenu", "ExtractAssetToStage");
		public static MenuItem ReplaceAsset => GetMenuItem("AssetsListContextMenu", "ReplaceAsset");
		public static MenuItem ReplaceAssets => GetMenuItem("AssetsListContextMenu", "ReplaceAssets");
		public static MenuItem CopyPath => GetMenuItem("AssetsListContextMenu", "CopyPath");
		public static MenuItem CopyRef => GetMenuItem("AssetsListContextMenu", "CopyRef");

		public static void HandleContextMenuOpening(object sender, ContextMenuEventArgs e, int selectedCount) {
			if (selectedCount == 0) {
				e.Handled = true;
				return;
			}

			var suffix = (selectedCount == 1 ? "" : "s");
			SelectedItemsCount.Header = $"{selectedCount} asset{suffix} selected";

			ReplaceAsset.Visibility = (selectedCount == 1 ? Visibility.Visible : Visibility.Collapsed);
			ReplaceAssets.Visibility = (selectedCount > 1 ? Visibility.Visible : Visibility.Collapsed);

			CopyPath.Header = "Copy path" + (selectedCount > 1 ? "s" : "");
			CopyRef.Header = "Copy ref" + (selectedCount > 1 ? "s" : "");
		}

		private static MenuItem? GetMenuItem(string menuName, string menuItemName) {
			if (App.Current.FindResource(menuName) is ContextMenu contextMenu) {
				foreach (var Item in contextMenu.Items) {
					if (Item is MenuItem menuItem) {
						if (menuItem.Name == menuItemName) {
							return menuItem;
						}
					}
				}
			}

			return null;
		}
	}
}
