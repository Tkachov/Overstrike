// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Localization;
using System.IO;

namespace DAT1.Files {
	public class Localization_I30: DAT1 {
		public Localization_I30(BinaryReader r): base(r) {}

		public KeysDataSection KeysDataSection => Section<KeysDataSection>(KeysDataSection.TAG);
		public KeysOffsetsSection KeysOffsetsSection => Section<KeysOffsetsSection>(KeysOffsetsSection.TAG);
		public ValuesDataSection ValuesDataSection => Section<ValuesDataSection>(ValuesDataSection.TAG);
		public ValuesOffsetsSection ValuesOffsetsSection => Section<ValuesOffsetsSection>(ValuesOffsetsSection.TAG);

		public string? GetValue(string? key) {
			if (string.IsNullOrEmpty(key)) return null;

			int keyOffset = KeysDataSection.GetOffsetByKey(key);
			if (keyOffset == -1) return null;

			int index = KeysOffsetsSection.Values.IndexOf((uint)keyOffset);
			if (index == -1 || index >= ValuesOffsetsSection.Values.Count) return null;

			uint valueOffset = ValuesOffsetsSection.Values[index];
			return ValuesDataSection.GetStringByOffset(valueOffset);
		}

		public bool HasKey(string? key) {
			if (string.IsNullOrEmpty(key)) return false;
			return KeysDataSection.GetOffsetByKey(key) != -1;
		}
	}
}
