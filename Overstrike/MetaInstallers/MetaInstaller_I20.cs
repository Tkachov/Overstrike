// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using System.IO;
using Overstrike.Installers;

namespace Overstrike.MetaInstallers {
	internal class MetaInstaller_I20: MetaInstallerBase {
		public MetaInstaller_I20(string gamePath): base(gamePath) {}

		public override void Prepare() {
			var tocPath = Path.Combine(_gamePath, "asset_archive", "toc");
			var tocBakPath = Path.Combine(_gamePath, "asset_archive", "toc.BAK");

			if (!File.Exists(tocBakPath)) {
				File.Copy(tocPath, tocBakPath);
			} else {
				File.Copy(tocBakPath, tocPath, true);
			}

			var modsPath = Path.Combine(_gamePath, "asset_archive", "mods");
			if (!Directory.Exists(modsPath)) {
				Directory.CreateDirectory(modsPath);
			}

			var suitsPath = Path.Combine(_gamePath, "asset_archive", "Suits");
			if (!Directory.Exists(suitsPath)) {
				Directory.CreateDirectory(suitsPath);
			}
		}

		private TOC_I20 _toc;
		private TOC_I20 _unchangedToc;

		public override void Start() {
			var tocPath = Path.Combine(_gamePath, "asset_archive", "toc");
			_toc = new TOC_I20();
			_toc.Load(tocPath);

			_unchangedToc = new TOC_I20(); // a special copy for .smpcmod installer to lookup indexes in
			_unchangedToc.Load(tocPath);
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
					return new MSMRSuitInstaller(_toc, _gamePath);

				case ModEntry.ModType.SUIT_MM:
					return new MMSuit1Installer(_toc, _gamePath);

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
