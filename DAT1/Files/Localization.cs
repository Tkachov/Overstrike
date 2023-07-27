﻿// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Localization;
using System.Diagnostics;
using System.IO;

namespace DAT1.Files {
	public class Localization: DAT1 {
		public const uint MAGIC = 0x122BB0AB;

		uint magic, dat1_size;
		byte[] unk;

		public Localization(BinaryReader r, FormatVersion version) : base(version) {
			magic = r.ReadUInt32();
			dat1_size = r.ReadUInt32();
			unk = r.ReadBytes(28);

			Debug.Assert(magic == MAGIC, "Localization(): bad magic");

			Init(r);
		}

		public EntriesCountSection EntriesCountSection => (EntriesCountSection)sections[EntriesCountSection.TAG];
		public KeysDataSection KeysDataSection => (KeysDataSection)sections[KeysDataSection.TAG];
		public KeysOffsetsSection KeysOffsetsSection => (KeysOffsetsSection)sections[KeysOffsetsSection.TAG];
		public ValuesDataSection ValuesDataSection => (ValuesDataSection)sections[ValuesDataSection.TAG];
		public ValuesOffsetsSection ValuesOffsetsSection => (ValuesOffsetsSection)sections[ValuesOffsetsSection.TAG];

		public string GetValue(string key) {
			int key_offset = KeysDataSection.GetOffsetByKey(key);
			if (key_offset == -1) return null;

			int index = KeysOffsetsSection.Values.IndexOf((uint)key_offset);
			if (index == -1) return null;

			uint value_offset = ValuesOffsetsSection.Values[index];
			return ValuesDataSection.GetStringByOffset(value_offset);
		}
	}
}