// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using System.IO;
using Overstrike.Installers;
using Overstrike.Utils;

namespace Overstrike.MetaInstallers {
	internal class MetaInstaller_I20: MetaInstallerBase {
		public MetaInstaller_I20(string gamePath, AppSettings settings, Profile profile) : base(gamePath, settings, profile) {}

		public override void Prepare() {
			var tocPath = Path.Combine(_gamePath, "asset_archive", "toc");
			var tocBakPath = Path.Combine(_gamePath, "asset_archive", "toc.BAK");

			if (!File.Exists(tocBakPath)) {
				ErrorLogger.WriteInfo("Creating 'toc.BAK'...");
				File.Copy(tocPath, tocBakPath);
				ErrorLogger.WriteInfo(" OK!\n");
			} else {
				ErrorLogger.WriteInfo("Overwriting 'toc' with 'toc.BAK'...");
				File.Copy(tocBakPath, tocPath, true);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			var modsPath = Path.Combine(_gamePath, "asset_archive", "mods");
			if (!Directory.Exists(modsPath)) {
				ErrorLogger.WriteInfo("Creating 'mods' directory...");
				Directory.CreateDirectory(modsPath);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			var suitsPath = Path.Combine(_gamePath, "asset_archive", "Suits");
			if (!Directory.Exists(suitsPath)) {
				ErrorLogger.WriteInfo("Creating 'Suits' directory...");
				Directory.CreateDirectory(suitsPath);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			ErrorLogger.WriteInfo("\n");
		}

		private TOC_I20 _toc;
		private TOC_I20 _unchangedToc;

		public override void Start() {
			ErrorLogger.WriteInfo("Loading 'toc'...");

			var tocPath = Path.Combine(_gamePath, "asset_archive", "toc");
			_toc = new TOC_I20();
			_toc.Load(tocPath);

			_unchangedToc = new TOC_I20(); // a special copy for .smpcmod installer to lookup indexes in
			_unchangedToc.Load(tocPath);

			ErrorLogger.WriteInfo(" OK!\n");
			LogTocSanityCheck();
			ErrorLogger.WriteInfo("\n");
		}

		private void LogTocSanityCheck() {
			ErrorLogger.WriteInfo($"[i] {_toc.ArchivesSection.Values.Count} archives\n");
			
			bool hasMods = false;
			foreach (var archive in _toc.ArchivesSection.Values) {
				var fn = archive.GetFilename();
				if (fn.Contains("mods", System.StringComparison.InvariantCultureIgnoreCase) || fn.Contains("Suits", System.StringComparison.InvariantCultureIgnoreCase)) {
					hasMods = true;
					break;
				}
			}
			if (hasMods) {
				ErrorLogger.WriteInfo($"[!] might be modified\n");
			}
		}

		public override void Install(ModEntry mod, int index) {
			var installer = GetInstaller(mod);
			installer.Install(mod, index);
		}

		private InstallerBase GetInstaller(ModEntry mod) {
			switch (mod.Type) {
				case ModEntry.ModType.SMPC:
				case ModEntry.ModType.MMPC:
					return new SMPCModInstaller(_toc, _unchangedToc, _gamePath);

				case ModEntry.ModType.SUIT_MSMR:
					return new MSMRSuitInstaller(_toc, _gamePath, _profile.Settings_Suit_Language);

				case ModEntry.ModType.SUIT_MM:
					return new MMSuit1Installer(_toc, _gamePath, _profile.Settings_Suit_Language);

				case ModEntry.ModType.SUIT_MM_V2:
					return new MMSuit2Installer(_toc, _gamePath);

				case ModEntry.ModType.STAGE_MSMR:
				case ModEntry.ModType.STAGE_MM:
					return new StageInstaller_I20(_toc, _gamePath);

				default:
					return null;
			}
		}

		public override void Finish() {
			var tocPath = Path.Combine(_gamePath, "asset_archive", "toc");
			_toc.Save(tocPath);
		}
	}
}
