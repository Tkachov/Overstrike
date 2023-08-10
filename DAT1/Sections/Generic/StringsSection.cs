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
		protected uint currentOffset = 0;

		override public void Load(byte[] data, DAT1 container) {
			var size = data.Length;

			Clear();

			for (uint i = 0; i < size; ++i) {
				if (i == size - 1 || data[i] == 0) {
					string s = Encoding.UTF8.GetString(data, (int)currentOffset, (int)(i - currentOffset));
					offsetByKey[s] = (uint)currentOffset;
					keyByOffset[(uint)currentOffset] = s;
					Strings.Add(s);
					currentOffset = i + 1;
				}
			}
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);
			foreach (var key in Strings) {
				w.Write(Encoding.UTF8.GetBytes(key));
				w.Write((byte)0);
			}
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

		public void Clear() {
			Strings.Clear();
			offsetByKey.Clear();
			keyByOffset.Clear();
			currentOffset = 0;
		}

		public uint Add(string key) {
			if (offsetByKey.ContainsKey(key)) {
				return offsetByKey[key];
			}

			var result = currentOffset;
			
			Strings.Add(key);
			offsetByKey[key] = currentOffset;
			keyByOffset[currentOffset] = key;
			currentOffset += (uint)Encoding.UTF8.GetByteCount(key) + 1;

			return result;
		}
	}
}
