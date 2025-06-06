﻿// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;

namespace DAT1.Sections.Config {
	public class ConfigTypeSection: SerializedSection {
        public const uint TAG = 0x4A128222; // Config Type
	}

	public class ConfigTypeSection_I30: SerializedSection_I30 {
		public const uint TAG = 0x4A128222; // Config Type
	}
}
