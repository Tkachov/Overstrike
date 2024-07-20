// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using OverstrikeShared.Windows;
using System.Windows;
using System.Windows.Controls;

namespace ModdingTool.Windows;

public partial class ModularWizardPreview: ModularWizardBase {
	public ModularWizardPreview(Window mainWindow) { // TODO: pass something to get layout from
		InitializeComponent();
		Init(mainWindow);
	}

	//

	protected override Grid MainGrid { get => _MainGrid; }
	protected override Label NumberLabel { get => _NumberLabel; }
	protected override TextBox NumberBox { get => _NumberBox; }

	protected override string ModName { get => "Untitled"; } // TODO: take name from creation window's last tab

	protected override JArray LoadLayout() {
		return new JArray(); // TODO: get layout from creation window
	}

	protected override ulong LoadSelectedCombinationNumber() {
		return 1;
	}

	protected override void SaveSelection() {}
}
