// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Data;
using System.Collections.Generic;
using System.IO;

namespace Overstrike.Detectors {
	internal abstract class DetectorBase {
		public DetectorBase() {}

		public abstract string[] GetExtensions();
		public abstract void Detect(Stream file, string path, List<ModEntry> mods, List<string> warnings);

		protected string GetShortPath(string path) {
			var index = path.LastIndexOf("||");
			if (index != -1) {
				return path.Substring(index + 2);
			}

			return path;
		}
	}
}
