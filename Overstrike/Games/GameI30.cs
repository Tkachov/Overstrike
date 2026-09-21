// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Data;
using Overstrike.MetaInstallers;
using System.IO;
using System.Windows.Media.Imaging;

namespace Overstrike.Games {
	internal class GameI30: GameBase {
		public const string ID = "i30";
		public static GameI30 Instance = new();

		public override string UserFriendlyName => "i30";

		public override string GetExecutablePath(string gamePath) {
			return Path.Combine(gamePath, "i30.exe");
		}

		public override string GetTocPath(string gamePath) {
			return Path.Combine(gamePath, "toc");
		}

		public override bool IsGameInstallation(string gamePath) {
			return (File.Exists(Path.Combine(gamePath, "toc")) && File.Exists(Path.Combine(gamePath, "i30.exe")));
		}

		public override bool IsCompatible(ModEntry mod) {
			return (mod.Type == ModEntry.ModType.STAGE_I30 || mod.Type == ModEntry.ModType.MODULAR_I30);
		}

		public override MetaInstallerBase GetMetaInstaller(string gamePath, AppSettings settings, Profile profile) {
			return new MetaInstaller_I29(gamePath, settings, profile);
		}

		public override string GetTocHashFriendlyName(string sha1) {
			return null;
		}

		//

		public override BitmapImage BannerBackground => GetThemedImage("banner_i30_back");

		public override BitmapImage BannerLogoLeft => GetThemedImage("banner_i30_logo");

		public override BitmapImage BannerLogoRight => GetThemedImage("banner_i30_logo2");

		public override bool HasSuitsSettingsSection => false;
		public override bool HasScriptsSettingsSection => false;
	}
}
