// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using OverstrikeShared.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ModdingTool.Windows;

public partial class ModularWizardPreview: ModularWizardBase {
	private ModularCreationWindow _creationWindow;

	public ModularWizardPreview(Window mainWindow) {
		InitializeComponent();

		_creationWindow = (ModularCreationWindow)mainWindow;
		Init(mainWindow);
	}

	//

	protected override Grid MainGrid { get => _MainGrid; }
	protected override Label NumberLabel { get => _NumberLabel; }
	protected override TextBox NumberBox { get => _NumberBox; }

	protected override string ModName { get => _creationWindow.ModName; }
	protected override string IconsStyle { get => _creationWindow.SelectedIconsStyle; }

	protected override JArray LoadLayout() {
		var entries = _creationWindow.Entries;
		var layout = new JArray();

		foreach (var entry in entries) {
			if (entry is HeaderEntry header) {
				layout.Add(new JArray() { "header", header.Text });
			} else if (entry is ModuleEntry module) {
				if (module.Options.Count > 1) {
					var options = new JArray();
					foreach (var option in module.Options) {
						options.Add(new JArray() { option._iconPath, option.Name, option._path });
					}
					layout.Add(new JArray() { "module", module.Name, options });
				}
			} else if (entry is SeparatorEntry) {
				layout.Add(new JArray() { "separator" });
			}
		}

		return layout;
	}

	protected override ulong LoadSelectedCombinationNumber() { return 1; }

	protected override BitmapSource GetIconByPath(string path) {
		if (path == "") return null;

		foreach (CollectionContainer container in _creationWindow.OptionIconCollection) {
			foreach (IconPath icon in container.Collection) {
				if (icon.Path == path) {
					return icon.Icon;
				}
			}
		}

		return null;
	}

	protected override void SaveSelection() {}
}
