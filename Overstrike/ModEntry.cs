using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace Overstrike {
	public class ModEntry: INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		public enum ModType {
			SMPC,
			MMPC,
			SUIT_MSMR,
			SUIT_MM,
			SUIT_MM_V2,

			UNKNOWN
		}

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

		// run-time
		public string Path { get; set; }
		public ModType Type { get; set; }

		// UI
		public int Order { get; set; }
		public BitmapImage Badge { get; set; }

		public ModEntry(string name, string path, ModType type) {
			Install = true;
			Name = name;
			Path = path;
			Type = type;
			Order = 0;
			Badge = null;
		}

		public ModEntry(string path, bool install) {
			Install = install;
			Name = null;
			Path = path;
			Type = ModType.UNKNOWN;
			Order = 0;
			Badge = null;
		}

		public ModEntry(ModEntry mod, bool install, int order) {
			Install = install;
			Name = mod.Name;
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

				default:
					return null;
			}
		}

		private static BitmapImage badge_smpc = null;
		private static BitmapImage badge_mmpc = null;
		private static BitmapImage badge_suit = null;

		private static void LoadBadges() {
			if (badge_smpc == null)
				badge_smpc = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.badge_smpc);

			if (badge_mmpc == null)
				badge_mmpc = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.badge_mmpc);

			if (badge_suit == null)
				badge_suit = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.badge_suit);
		}
	}
}
