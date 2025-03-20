// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using System.IO;
using Overstrike.Installers;
using Overstrike.Utils;
using Overstrike.Data;
using Overstrike.Games;

namespace Overstrike.MetaInstallers {
	internal class MetaInstaller_I29: MetaInstallerBase {
		private string _outTocName = "toc";

		public MetaInstaller_I29(string gamePath, AppSettings settings, Profile profile): base(gamePath, settings, profile) {}

		public override void Prepare() {
			if (_profile.Settings_Scripts_Enabled && _profile.Settings_Scripts_ModToc) {
				_outTocName = "tocm";
			}

			var tocPath = Path.Combine(_gamePath, _outTocName);
			var origTocPath = Path.Combine(_gamePath, "toc");
			var tocBakPath = Path.Combine(_gamePath, "toc.BAK");

			if (!File.Exists(tocBakPath)) {
				ErrorLogger.WriteInfo("Creating 'toc.BAK'...");
				File.Copy(origTocPath, tocBakPath);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			ErrorLogger.WriteInfo($"Overwriting '{_outTocName}' with 'toc.BAK'...");
			RemoveReadOnlyAttribute(tocPath);
			File.Copy(tocBakPath, tocPath, true);
			ErrorLogger.WriteInfo(" OK!\n");

			var modsPath = Path.Combine(_gamePath, "d", "mods");
			if (!Directory.Exists(modsPath)) {
				ErrorLogger.WriteInfo("Creating 'mods' directory...");
				Directory.CreateDirectory(modsPath);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			SetupScripts();

			ErrorLogger.WriteInfo("\n");
		}

		private void SetupScripts() {
			var scriptsPath = Path.Combine(_gamePath, "scripts");
			var scriptsTxtPath = Path.Combine(_gamePath, "scripts.txt");
			var scriptsProxyPath = Path.Combine(_gamePath, "winmm.dll");

			// cleanup

			if (Directory.Exists(scriptsPath)) {
				ErrorLogger.WriteInfo("Deleting 'scripts' directory...");
				Directory.Delete(scriptsPath, true);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			if (File.Exists(scriptsTxtPath)) {
				ErrorLogger.WriteInfo("Deleting 'scripts.txt'...");
				File.Delete(scriptsTxtPath);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			if (File.Exists(scriptsProxyPath)) {
				ErrorLogger.WriteInfo("Deleting 'winmm.dll'...");
				File.Delete(scriptsProxyPath);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			//

			if (!_profile.Settings_Scripts_Enabled) return;

			if (!Directory.Exists(scriptsPath)) {
				ErrorLogger.WriteInfo("Creating 'scripts' directory...");
				Directory.CreateDirectory(scriptsPath);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			ErrorLogger.WriteInfo("Creating 'scripts.txt'...");
			File.WriteAllText(scriptsTxtPath, "");
			ErrorLogger.WriteInfo(" OK!\n");

			ErrorLogger.WriteInfo("Creating 'winmm.dll'...");
			File.Copy("scripts_proxy.dll", scriptsProxyPath);
			ErrorLogger.WriteInfo(" OK!\n");
		}

		private TOC_I29 _toc;

		public override void Start() {
			ErrorLogger.WriteInfo($"Loading '{_outTocName}'...");

			var tocPath = Path.Combine(_gamePath, _outTocName);
			_toc = new TOC_I29();
			_toc.Load(tocPath);

			ErrorLogger.WriteInfo(" OK!\n");
			LogTocSanityCheck();
			ErrorLogger.WriteInfo("\n");
		}

		private void LogTocSanityCheck() {
			var sha = Hashes.GetFileSha1(Path.Combine(_gamePath, _outTocName));
			var friendlyName = GameBase.GetGame(_profile.Game).GetTocHashFriendlyName(sha);
			var friendlyNameSuffix = (string.IsNullOrEmpty(friendlyName) ? "" : $" ({friendlyName})");
			ErrorLogger.WriteInfo($"[i] SHA-1: {sha[..7].ToUpper()}{friendlyNameSuffix}\n");

			ErrorLogger.WriteInfo($"[i] {_toc.ArchivesSection.Values.Count} archives\n");

			bool hasMods = false;
			foreach (var archive in _toc.ArchivesSection.Values) {
				var fn = archive.GetFilename();
				if (fn.Contains("mods", System.StringComparison.InvariantCultureIgnoreCase)) {
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
				case ModEntry.ModType.STAGE_RCRA:
				case ModEntry.ModType.STAGE_I30:
				case ModEntry.ModType.STAGE_I33:
				case ModEntry.ModType.STAGE_MSM2:
					return new StageInstaller_I29(_toc, _gamePath);

				case ModEntry.ModType.STAGE_RCRA_V2:
				case ModEntry.ModType.STAGE_MSM2_V2:
					return new StageInstaller_I29_V2(_toc, _gamePath);

				case ModEntry.ModType.SCRIPT_SUPPORT:
					return new ScriptSupportInstaller(_gamePath);

				case ModEntry.ModType.SCRIPT_MSM2:
					return new ScriptInstaller(_gamePath);
				
				case ModEntry.ModType.SUIT2_MSM2:
					return new MSM2Suit2Installer(_toc, _gamePath, _profile.Settings_Suit_Language);

				case ModEntry.ModType.SUIT_STYLE_MSM2:
					return new MSM2SuitStyleInstaller(_toc, _gamePath);

				default:
					return null;
			}
		}

		public override void Finish() {
			var tocPath = Path.Combine(_gamePath, _outTocName);
			RemoveReadOnlyAttribute(tocPath);
			_toc.Save(tocPath);
		}

		public override void Uninstall() {
			// even if installing to tocm, uninstall is cleaning the normal toc

			var tocPath = Path.Combine(_gamePath, "toc");
			var tocBakPath = Path.Combine(_gamePath, "toc.BAK");

			if (File.Exists(tocBakPath)) {
				ErrorLogger.WriteInfo($"Overwriting 'toc' with 'toc.BAK'...");
				RemoveReadOnlyAttribute(tocPath);
				File.Copy(tocBakPath, tocPath, true);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			// remove scripts proxy

			var scriptsProxyPath = Path.Combine(_gamePath, "winmm.dll");

			if (File.Exists(scriptsProxyPath)) {
				ErrorLogger.WriteInfo("Deleting 'winmm.dll'...");
				File.Delete(scriptsProxyPath);
				ErrorLogger.WriteInfo(" OK!\n");
			}
		}
	}
}
