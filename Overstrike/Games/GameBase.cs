// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.MetaInstallers;
using System.IO;
using System;
using System.Windows.Media.Imaging;
using Overstrike.Data;

namespace Overstrike.Games {
	internal abstract class GameBase {
		public static GameBase GetGame(string id) {
			switch (id) {
				case GameMSMR.ID: return GameMSMR.Instance;
				case GameMM.ID: return GameMM.Instance;
				case GameRCRA.ID: return GameRCRA.Instance;
				case GameI30.ID: return GameI30.Instance;
				case GameI33.ID: return GameI33.Instance;
				case GameMSM2.ID: return GameMSM2.Instance;
				default: return null;
			}
		}

		public static string DetectGameInstallation(string gamePath) {
			try {
				if (!Directory.Exists(gamePath)) return null;

				if (GameMSMR.Instance.IsGameInstallation(gamePath)) return GameMSMR.ID;
				if (GameMM.Instance.IsGameInstallation(gamePath)) return GameMM.ID;
				if (GameRCRA.Instance.IsGameInstallation(gamePath)) return GameRCRA.ID;
				if (GameI30.Instance.IsGameInstallation(gamePath)) return GameI30.ID;
				if (GameI33.Instance.IsGameInstallation(gamePath)) return GameI33.ID;
				if (GameMSM2.Instance.IsGameInstallation(gamePath)) return GameMSM2.ID;
			} catch (Exception) { }

			return null;
		}

		//

		public abstract string UserFriendlyName { get; }

		public abstract string GetExecutablePath(string gamePath);

		public abstract string GetTocPath(string gamePath);

		public abstract bool IsGameInstallation(string gamePath);

		public abstract bool IsCompatible(ModEntry mod);

		public abstract MetaInstallerBase GetMetaInstaller(string gamePath, AppSettings settings, Profile profile);

		//

		public abstract BitmapImage BannerBackground { get; }
		public abstract BitmapImage BannerLogoLeft { get; }
		public abstract BitmapImage BannerLogoRight { get; }

		public abstract bool HasSuitsSettingsSection { get; }
		public abstract bool HasScriptsSettingsSection { get; }
	}
}
