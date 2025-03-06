// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Data;
using Overstrike.MetaInstallers;
using System.IO;
using System.Windows.Media.Imaging;

namespace Overstrike.Games {
	internal class GameMSM2: GameBase {
		public const string ID = "MSM2";
		public static GameMSM2 Instance = new();

		public override string UserFriendlyName => "Marvel's Spider-Man 2";

		public override string GetExecutablePath(string gamePath) {
			return Path.Combine(gamePath, "Spider-Man2.exe");
		}

		public override string GetTocPath(string gamePath) {
			return Path.Combine(gamePath, "toc");
		}

		public override bool IsGameInstallation(string gamePath) {
			return (File.Exists(Path.Combine(gamePath, "toc")) && File.Exists(Path.Combine(gamePath, "Spider-Man2.exe")));
		}

		public override bool IsCompatible(ModEntry mod) {
			return (mod.Type == ModEntry.ModType.STAGE_MSM2 || mod.Type == ModEntry.ModType.STAGE_MSM2_V2 || mod.Type == ModEntry.ModType.MODULAR_MSM2 || mod.Type == ModEntry.ModType.SCRIPT_MSM2 || mod.Type == ModEntry.ModType.SUIT2_MSM2 || mod.Type == ModEntry.ModType.SUIT_STYLE_MSM2);
		}

		public override MetaInstallerBase GetMetaInstaller(string gamePath, AppSettings settings, Profile profile) {
			return new MetaInstaller_I29(gamePath, settings, profile);
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
				_logo ??= Utils.Imaging.ConvertToBitmapImage(Properties.Resources.banner_msm2_logo);
				return _logo;
			}
		}

		public override BitmapImage BannerLogoRight {
			get {
				_logo2 ??= Utils.Imaging.ConvertToBitmapImage(Properties.Resources.banner_msm2_logo2);
				return _logo2;
			}
		}

		public override bool HasSuitsSettingsSection => false;
		public override bool HasScriptsSettingsSection => true;
	}
}
