// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;

namespace DAT1.Sections.Localization {
	public class KeyHashesSection: UInt32ArraySection {
		public const uint TAG = 0x06A58050;
	}

	public class SortedKeyHashesSection: UInt32ArraySection {
		public const uint TAG = 0xC43731B5; // Localization SortedHashes Built
	}

	public class SortedIndexesSection: UInt16ArraySection {
		public const uint TAG = 0x0CD2CFE9; // Localization SortedIndexes Built
	}
}
