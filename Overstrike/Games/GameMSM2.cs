// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Data;
using Overstrike.MetaInstallers;
using System.Collections.Generic;
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

		private static Dictionary<string, string> _knownHashes = new () {
			{ "318F4345F3A1E8604F2ABDB4B3D266EE82CBCC32", "v1.130.0.0" },
			{ "7067DADB2153D0B3B7F3F1635CB7EB970E250F3D", "v1.202.0.0" },
			{ "5D9171696FA00566C3F4F9FC7563BD53F8896B03", "v1.205.0.0" },
			{ "E0537352A31E3CB3287CA348EB007E63030E4963", "v1.212.1.0" },
			{ "A5B63A78D4D043493233E2B5279CE63E7755A51D", "v1.218.0.0" },
			{ "9CEAC6760A571F9DC230D70014640910165D538E", "v1.305.0.0" }, // same as v1.307.0.0
			{ "EA88E03938A958DE6989DAE6FD7693F489B59162", "v1.312.0.0" },
			{ "CBDAE7CD5CF8D722207B0A749C6D6E3D70D5FA99", "v1.318.1.0" }
		};

		public override string GetTocHashFriendlyName(string sha1) {
			string result = null;
			_knownHashes.TryGetValue(sha1.ToUpper(), out result);
			return result;
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
