// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.MetaInstallers;
using System.IO;

namespace Overstrike.Games {
	internal class GameMM: GameBase {
		public const string ID = "MM";
		public static GameMM Instance = new();

		public override string UserFriendlyName => "Marvel's Spider-Man: Miles Morales";

		public override string GetExecutablePath(string gamePath) {
			return Path.Combine(gamePath, "MilesMorales.exe");
		}

		public override bool IsGameInstallation(string gamePath) {
			if (!Directory.Exists(Path.Combine(gamePath, "asset_archive"))) return false;
			if (!File.Exists(Path.Combine(gamePath, "asset_archive", "toc"))) return false;

			return (File.Exists(Path.Combine(gamePath, "MilesMorales.exe")));
		}

		public override bool IsCompatible(ModEntry mod) {
			return (mod.Type == ModEntry.ModType.MMPC || mod.Type == ModEntry.ModType.SUIT_MM || mod.Type == ModEntry.ModType.SUIT_MM_V2 || mod.Type == ModEntry.ModType.STAGE_MM);
		}

		public override MetaInstallerBase GetMetaInstaller(string gamePath) {
			return new MSMRMetaInstaller(gamePath);
		}
	}
}
