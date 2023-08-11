// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

namespace Overstrike.MetaInstallers {
	internal abstract class MetaInstallerBase {
		protected string _gamePath;
		protected AppSettings _settings;
		protected Profile _profile;

		public MetaInstallerBase(string gamePath, AppSettings settings, Profile profile) {
			_gamePath = gamePath;
			_settings = settings;
			_profile = profile;
		}

		public abstract void Prepare();
		public abstract void Start();
		public abstract void Install(ModEntry mod, int index);
		public abstract void Finish();
	}
}
