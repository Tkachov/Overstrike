// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Data;
using Overstrike.MetaInstallers;
using System.IO;
using System.Windows.Media.Imaging;

namespace Overstrike.Games {
	internal class GameMSMR: GameBase {
		public const string ID = "MSMR";
		public static GameMSMR Instance = new();

		public override string UserFriendlyName => "Marvel's Spider-Man Remastered";

		public override string GetExecutablePath(string gamePath) {
			return Path.Combine(gamePath, "Spider-Man.exe");
		}

		public override string GetTocPath(string gamePath) {
			return Path.Combine(gamePath, "asset_archive", "toc");
		}

		public override bool IsGameInstallation(string gamePath) {
			if (!Directory.Exists(Path.Combine(gamePath, "asset_archive"))) return false;
			if (!File.Exists(Path.Combine(gamePath, "asset_archive", "toc"))) return false;

			return (File.Exists(Path.Combine(gamePath, "Spider-Man.exe")));
		}

		public override bool IsCompatible(ModEntry mod) {
			return (mod.Type == ModEntry.ModType.SMPC || mod.Type == ModEntry.ModType.SUIT_MSMR || mod.Type == ModEntry.ModType.STAGE_MSMR || mod.Type == ModEntry.ModType.SUITS_MENU || mod.Type == ModEntry.ModType.MODULAR_MSMR);
		}

		public override MetaInstallerBase GetMetaInstaller(string gamePath, AppSettings settings, Profile profile) {
			return new MetaInstaller_I20(gamePath, settings, profile);
		}

		public override string GetTocHashFriendlyName(string sha1) {
			return null;
		}

		//

		private BitmapImage _back;
		private BitmapImage _logo;
		private BitmapImage _logo2;

		public override BitmapImage BannerBackground {
			get {
				_back ??= Utils.Imaging.ConvertToBitmapImage(Properties.Resources.banner_msmr_back);
				return _back;
			}
		}

		public override BitmapImage BannerLogoLeft {
			get {
				_logo ??= Utils.Imaging.ConvertToBitmapImage(Properties.Resources.banner_msmr_logo);
				return _logo;
			}
		}

		public override BitmapImage BannerLogoRight {
			get {
				_logo2 ??= Utils.Imaging.ConvertToBitmapImage(Properties.Resources.banner_msmr_logo2);
				return _logo2;
			}
		}

		public override bool HasSuitsSettingsSection => true;
		public override bool HasScriptsSettingsSection => false;
	}
}
