// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DAT1.Sections.Generic {
	public class StringsSection: Section {
		public List<string> Strings = new();
		protected Dictionary<string, uint> offsetByKey = new();
		protected Dictionary<uint, string> keyByOffset = new();

		override public void Load(byte[] data, DAT1 container) {
			var size = data.Length;
			int start_offset = 0;

			Strings.Clear();
			offsetByKey.Clear();
			keyByOffset.Clear();

			for (int i = 0; i < size; ++i) {
				if (i == size - 1 || data[i] == 0) {
					string s = Encoding.UTF8.GetString(data, start_offset, i - start_offset);
					offsetByKey[s] = (uint)start_offset;
					keyByOffset[(uint)start_offset] = s;
					Strings.Add(s);
					start_offset = i + 1;
				}
			}
		}

		override public byte[] Save() {
			return null; // TODO

			var s = new MemoryStream();
			var w = new BinaryWriter(s);
			/*
			foreach (var e in Entries) {
				w.Write((UInt64)e.AssetId);
				w.Write((uint)e.AssetPathStringOffset);
				w.Write((uint)e.ExtensionHash);
			}
			*/
			return s.ToArray();
		}

		public string GetStringByOffset(uint offset) {
			string result = null;
			keyByOffset.TryGetValue(offset, out result);
			return result;
		}

		public int GetOffsetByKey(string key) {
			if (!offsetByKey.ContainsKey(key))
				return -1;

			return (int)offsetByKey[key];
		}
	}
}
