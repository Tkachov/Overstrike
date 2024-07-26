﻿// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using OverstrikeShared.Windows;
using System.Windows;
using System.Windows.Controls;

namespace ModdingTool.Windows;

public partial class ModularWizardPreview: ModularWizardBase {
	private ModularCreationWindow _creationWindow;

	public ModularWizardPreview(Window mainWindow) { // TODO: pass something to get layout from
		InitializeComponent();

		_creationWindow = (ModularCreationWindow)mainWindow;
		Init(mainWindow);
	}

	//

	protected override Grid MainGrid { get => _MainGrid; }
	protected override Label NumberLabel { get => _NumberLabel; }
	protected override TextBox NumberBox { get => _NumberBox; }

	protected override string ModName { get => "Untitled"; } // TODO: take name from creation window's last tab

	protected override JArray LoadLayout() {
		var entries = _creationWindow.Entries;
		var layout = new JArray();

		foreach (var entry in entries) {
			if (entry is HeaderEntry header) {
				layout.Add(new JArray() { "header", header.Text });
			} else if (entry is ModuleEntry module) {
				// TODO: add a module with 0 options causes a crash
				layout.Add(new JArray() { "module", module.Name, new JArray() {
					new JArray() { "(icon)", "(name)", "(file)" },
					new JArray() { "(icon)", "(name)", "(file)" }
				} });
			} else if (entry is SeparatorEntry) {
				layout.Add(new JArray() { "separator" });
			}
		}

		return layout;
	}

	protected override ulong LoadSelectedCombinationNumber() { return 1; }

	protected override void SaveSelection() {}
}