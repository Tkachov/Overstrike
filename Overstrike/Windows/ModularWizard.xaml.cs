// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using Overstrike.Data;
using Overstrike.Installers;
using OverstrikeShared.Windows;
using System.Windows;
using System.Windows.Controls;

namespace Overstrike.Windows {
    public partial class ModularWizard: ModularWizardBase {
		ModEntry _mod;

		public ModularWizard(ModEntry mod, Window mainWindow) {
			_mod = mod;

			InitializeComponent();
			Init(mainWindow);
		}

		//

		protected override Grid MainGrid { get => _MainGrid; }
		protected override Label NumberLabel { get => _NumberLabel; }
		protected override TextBox NumberBox { get => _NumberBox; }

		protected override string ModName { get => _mod.Name; }

		private string _iconsStyle;
		protected override string IconsStyle { get => _iconsStyle; }

		protected override JArray LoadLayout() {
			using var modular = ModularInstaller.ReadModularFile(_mod);
			var info = ModularInstaller.GetInfo(modular);
			_iconsStyle = (string)info["icons_style"];
			return (JArray)info["layout"];
		}

		protected override ulong LoadSelectedCombinationNumber() {
			return ModularInstaller.GetSelectedCombinationNumber(_mod);
		}

		protected override void SaveSelection() {
			if (_mod.Extras == null) {
				_mod.Extras = new JObject();
			}

			_mod.Extras["selections"] = GetCurrentCombinationNumber();
			_mod.Extras["description"] = GetCurrentCombinationDescription();
		}
	}
}
