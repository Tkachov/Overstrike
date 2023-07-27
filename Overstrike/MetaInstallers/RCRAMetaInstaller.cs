// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using System.IO;
using Overstrike.Installers;

namespace Overstrike.MetaInstallers {
	internal class RCRAMetaInstaller: MetaInstallerBase {
		public RCRAMetaInstaller(string gamePath) : base(gamePath) {
		}

		public override void Prepare() {
			var tocPath = Path.Combine(_gamePath, "toc");
			var tocBakPath = Path.Combine(_gamePath, "toc.BAK");

			if (!File.Exists(tocBakPath)) {
				File.Copy(tocPath, tocBakPath);
			} else {
				File.Copy(tocBakPath, tocPath, true);
			}

			var modsPath = Path.Combine(_gamePath, "d", "mods");
			if (!Directory.Exists(modsPath)) {
				Directory.CreateDirectory(modsPath);
			}
		}

		private TOC_I29 _toc;

		public override void Start() {
			var tocPath = Path.Combine(_gamePath, "toc");
			_toc = new TOC_I29();
			_toc.Load(tocPath);
		}

		public override void Install(ModEntry mod, int index) {
			var installer = GetInstaller(mod, _toc);
			installer.Install(mod, index);
		}

		private InstallerBase GetInstaller(ModEntry mod, TOC_I29 toc) {
			switch (mod.Type) {
				case ModEntry.ModType.STAGE_RCRA:
					return new RCRAStageInstaller(toc, _gamePath);

				default:
					return null;
			}
		}

		public override void Finish() {
			var tocPath = Path.Combine(_gamePath, "toc");
			_toc.Save(tocPath);
		}
	}
}
