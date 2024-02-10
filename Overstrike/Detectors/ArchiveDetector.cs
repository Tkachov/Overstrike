// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO;
using System;
using System.Collections.Generic;
using SharpCompress.Archives;
using Overstrike.Data;

namespace Overstrike.Detectors {
	internal class ArchiveDetector: DetectorBase {
		private ModsDetection _detection;

		public ArchiveDetector(ModsDetection detection) : base() {
			_detection = detection;
		}

		public override string[] GetExtensions() {
			return new string[] {"7z", "rar", "zip"};
		}

		public override void Detect(Stream file, string path, List<ModEntry> mods) {			
			try {
				using var archive = ArchiveFactory.Open(file);
				_detection.Detect(archive, path, mods);
			} catch (Exception) { }
		}
	}
}
