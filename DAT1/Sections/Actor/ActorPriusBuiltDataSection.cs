// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using Newtonsoft.Json.Linq;
using System;

namespace DAT1.Sections.Localization {
	public class ActorPriusBuiltDataSection: Sections.UnknownSection {
		public const uint TAG = 0x6D4301EF; // Actor Prius Built Data

		public JObject GetData(DAT1 dat1, int offset, int size) {
			try {
				var arr = new byte[size];
				Buffer.BlockCopy(Raw, offset - (int)dat1.OriginalSectionsOffsets[TAG], arr, 0, size);

				var s = new SerializedSection_I30();
				s.Load(arr, dat1);
				return s.Data;
			} catch {}

			return null;
		}
	}
}
