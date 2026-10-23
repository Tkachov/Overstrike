// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Windows;

namespace Overstrike.Windows {
    public partial class TestThemeWindow : Window {
		public TestThemeWindow() {
            InitializeComponent();

			var app = (App)(Application.Current);
			var res = app.Resources.MergedDictionaries;
			if (res.Count > 1) {
				var theme = res[1];
				res.RemoveAt(1);

				var res2 = RightColumn.Resources.MergedDictionaries;
				res2.Clear();
				res2.Add(theme);
			}
		}
    }
}
