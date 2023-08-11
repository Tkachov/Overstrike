// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.MetaInstallers;
using System.IO;

namespace Overstrike.Games {
	internal class GameRCRA: GameBase {
		public const string ID = "RCRA";
		public static GameRCRA Instance = new();

		public override string UserFriendlyName => "Ratchet & Clank: Rift Apart";

		public override string GetExecutablePath(string gamePath) {
			return Path.Combine(gamePath, "RiftApart.exe");
		}

		public override bool IsGameInstallation(string gamePath) {
			return (File.Exists(Path.Combine(gamePath, "toc")) && File.Exists(Path.Combine(gamePath, "RiftApart.exe")));
		}

		public override bool IsCompatible(ModEntry mod) {
			return (mod.Type == ModEntry.ModType.STAGE_RCRA);
		}

		public override MetaInstallerBase GetMetaInstaller(string gamePath, AppSettings settings, Profile profile) {
			return new MetaInstaller_I29(gamePath, settings, profile);
		}
	}
}
