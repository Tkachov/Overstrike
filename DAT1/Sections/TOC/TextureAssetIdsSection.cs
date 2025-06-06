﻿// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.Collections.Generic;

namespace DAT1.Sections.TOC {
	public class TextureAssetIdsSection: UInt64ArraySection {
		public const uint TAG = 0x36A6C8CC; // Archive TOC Texture Asset Ids

		public List<ulong> Ids => Values;
	}
}
