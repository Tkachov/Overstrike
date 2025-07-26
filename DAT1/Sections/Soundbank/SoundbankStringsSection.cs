// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.Text;

namespace DAT1.Sections.Soundbank {
	public class SoundbankStringsSection: StringsSection {
		public const uint TAG = 0x3E8490A3; // Sound Bank Strings

		// contains asset original filename, and all Wwise object names ordered by FNV hash value

		override public void Load(byte[] data, DAT1 container) {
			var size = data.Length;

			Clear();

			for (uint i = 0; i < size; ++i) {
				if (i == size - 1 || data[i] == 0) {
					string s = Encoding.UTF8.GetString(data, (int)currentOffset, (int)(i - currentOffset));
					offsetByKey[s] = (uint)currentOffset;
					keyByOffset[(uint)currentOffset] = s;
					Strings.Add(s);

					++i;
					var r = i % 4;
					if (r > 0) {
						i += 4 - r;
					}

					currentOffset = i;
					continue;
				}
			}
		}
	}
}
