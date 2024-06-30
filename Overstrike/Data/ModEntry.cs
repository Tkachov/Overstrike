// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace Overstrike.Data {
	public class ModEntry: INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		[JsonConverter(typeof(StringEnumConverter))]
		public enum ModType {
			SMPC,
			MMPC,
			SUIT_MSMR,
			SUIT_MM,
			SUIT_MM_V2,
			STAGE_MSMR,
			STAGE_MM,
			STAGE_RCRA,
			STAGE_I30,
			STAGE_I33,
			SUITS_MENU,
			MODULAR_MSMR,
			MODULAR_MM,
			MODULAR_RCRA,
			MODULAR_I30,
			MODULAR_I33,

			UNKNOWN
		}

		public static string SUITS_MENU_PATH = "|Suits Menu|"; // must be impossible, so it doesn't clash with real files

		// stored
		private bool _install;
		public bool Install {
			get { return _install; }
			set {
				_install = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Install)));
			}
		}
		public string Name { get; set; }
		public JObject Extras { get; set; }

		// run-time
		public string Path { get; set; }
		public ModType Type { get; set; }
		public string ToolTip {
			get {
				if (Type == ModType.SUITS_MENU) return null;

				string result = Path;

				var depth = 0;
				var index = result.IndexOf("||");
				while (index != -1) {
					string left = result.Substring(0, index);
					string right = result.Substring(index + 2);
					string middle = "\n ";
					for (var i = 0; i < depth; ++i)
						middle += "   ";
					middle += "↳ ";
					result = left + middle + right;
					index = result.IndexOf("||");
					++depth;
				}

				if (IsTypeFamilyModular(Type)) {
					if (Extras != null && Extras.ContainsKey("description")) {
						result += "\n" + Extras["description"];
					}
				}

				return result;
			}
		}

		// UI
		public int Order { get; set; }
		public BitmapImage Badge { get; set; }

		public ModEntry(string name, string path, ModType type) {
			Install = true;
			Name = name;
			Path = path;
			Extras = null;
			Type = type;
			Order = 0;
			Badge = null;
		}

		public ModEntry(string path, bool install, JObject extras) {
			Install = install;
			Name = null;
			Path = path;
			Extras = extras;
			Type = ModType.UNKNOWN;
			Order = 0;
			Badge = null;
		}

		public ModEntry(ModEntry mod, bool install, int order, JObject extrasOverride) {
			Install = install;
			Name = mod.Name;
			Extras = (extrasOverride ?? mod.Extras);
			Path = mod.Path;
			Type = mod.Type;
			Order = order;
			Badge = GetBadge(Type);
		}

		private static BitmapImage GetBadge(ModType type) {
			LoadBadges();

			switch (type) {
				case ModType.SMPC: return badge_smpc;
				case ModType.MMPC: return badge_mmpc;

				case ModType.SUIT_MSMR:
				case ModType.SUIT_MM:
				case ModType.SUIT_MM_V2:
					return badge_suit;

				case ModType.STAGE_MSMR:
				case ModType.STAGE_MM:
				case ModType.STAGE_RCRA:
				case ModType.STAGE_I30:
				case ModType.STAGE_I33:
					return badge_stage;

				case ModType.SUITS_MENU:
					return badge_suit; // TODO: custom badge?

				case ModType.MODULAR_MSMR:
				case ModType.MODULAR_MM:
				case ModType.MODULAR_RCRA:
				case ModType.MODULAR_I30:
				case ModType.MODULAR_I33:
					return badge_modular;

				default:
					return null;
			}
		}

		private static BitmapImage badge_smpc = null;
		private static BitmapImage badge_mmpc = null;
		private static BitmapImage badge_suit = null;
		private static BitmapImage badge_stage = null;
		private static BitmapImage badge_modular = null;

		private static void LoadBadges() {
			if (badge_smpc == null)
				badge_smpc = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.badge_smpc);

			if (badge_mmpc == null)
				badge_mmpc = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.badge_mmpc);

			if (badge_suit == null)
				badge_suit = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.badge_suit);

			if (badge_stage == null)
				badge_stage = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.badge_stage);

			if (badge_modular == null)
				badge_modular = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.badge_modular);
		}

		public static bool IsTypeFamilyModular(ModType type) {
			return (type == ModType.MODULAR_MSMR || type == ModType.MODULAR_MM || type == ModType.MODULAR_RCRA || type == ModType.MODULAR_I30 || type == ModType.MODULAR_I33);
		}
	}
}
