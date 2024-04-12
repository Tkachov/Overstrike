// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;

namespace DAT1.Sections.Localization {
	public class UnknownSection: UInt8ArraySection {
		public const uint TAG = 0xB0653243; // Localization Flags Built

		public void Pad(int n = -1) {
			if (n == -1)
				n = Values.Count;

			// there should be 4 times more values than there are entries
			// this is kinda uint32 per entry, but in a sparse manner and the 2nd/3rd/4th bytes are always 0
			for (var i = Values.Count; i < 4*n; ++i)
				Values.Add(0);
		}
	}
}
