// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Installers;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Overstrike.Tabs {
	public partial class MMSuitsMenu: SuitsMenuBase {
		public MMSuitsMenu() {
			InitializeComponent();
			SuitsSlots.ItemContainerGenerator.StatusChanged += SuitsSlots_ItemGeneratorStatusChanged;
		}

		protected override ListView SuitsSlots { get => _SuitsSlots; }
		protected override Grid Modified { get => _Modified; }
		protected override Grid NotModified { get => _NotModified; }
		protected override TextBlock SuitName { get => _SuitName; }
		protected override Grid SuitInfo { get => _SuitInfo; }
		protected override Image BigIcon { get => _BigIcon; }
		protected override ComboBox SuitLoadoutComboBox { get => _SuitLoadoutComboBox; }
		protected override ComboBox SuitIconComboBox { get => _SuitIconComboBox; }
		protected override ComboBox SuitBigIconComboBox { get => _SuitBigIconComboBox; }
		protected override Button ToggleSuitDeleteButton { get => _ToggleSuitDeleteButton; }
		protected override Label NotModifiedStatusLabel { get => _NotModifiedStatusLabel; }
		protected override Button ResetButton { get => _ResetButton; }

		protected override bool HasBigIcons { get => true; }
		protected override Dictionary<string, byte> LANGUAGES { get => MMSuit1Installer.LANGUAGES; }
	}
}
