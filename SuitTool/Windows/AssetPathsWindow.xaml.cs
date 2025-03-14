// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using SuitTool.Data;
using System.Windows;
using System.Windows.Controls;

namespace SuitTool.Windows {
	public partial class AssetPathsWindow: Window {
		public AssetPathsWindow(Project project) {
			InitializeComponent();

			Contents.Children.Clear();

			if (project.Assets.Count == 0) {
				var tb = new TextBlock {
					Text = "No assets found",
					FontSize = 18,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(15, 15, 0, 20)
				};
				Contents.Children.Add(tb);
				return;
			}

			CreateAssetsExpander("Models", project.Models);
			CreateAssetsExpander("Materials", project.Materials);
			CreateAssetsExpander("Textures", project.Textures);
			CreateAssetsExpander("Other", project.OtherAssets);
		}

		private void CreateAssetsExpander(string header, List<string> assets) {
			if (assets.Count == 0) return;

			assets.Sort();

			var expander = new Expander {
				Header = " " + header,
				IsExpanded = true,

				HorizontalAlignment = HorizontalAlignment.Stretch,
				Margin = new Thickness(6, 3, 6, 5),
				Padding = new Thickness(0, 2, 0, 2)
			};

			var panel = new StackPanel();
			var first = true;
			foreach (var asset in assets) {
				panel.Children.Add(new TextBox {
					Text = asset,
					IsReadOnly = true,
					IsReadOnlyCaretVisible = true,

					Height = 22,
					Margin = new Thickness(1, first ? 4 : 0, 1, 6),
					Padding = new Thickness(1, 1, 1, 1)
				});
				first = false;
			}
			expander.Content = panel;

			Contents.Children.Add(expander);
		}
	}
}
