// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DAT1 {

	public class DAT1 {
		// header
		uint magic, unk1, size, sectionsCount;
		List<uint> sectionsTags = new();
		List<uint> sectionsSortedByOffset = new();

		long stringsStartOffset;
		byte[] strings;

		private Dictionary<uint, byte[]> _rawSections = new();
		public Dictionary<uint, Section> Sections = new();

		public T Section<T>(uint tag) where T: Section, new() {
			if (Sections.ContainsKey(tag)) {
				return (T)Sections[tag];
			}

			if (_rawSections.ContainsKey(tag)) {
				Sections[tag] = new T();
				Sections[tag].Load(_rawSections[tag], this);
				return (T)Sections[tag];
			}

			return null;
		}

		protected DAT1() {}

		public DAT1(BinaryReader r) {
			Init(r);
		}

		protected void Init(BinaryReader r) {
			long dat1_start = r.BaseStream.Position;

			magic = r.ReadUInt32();
			unk1 = r.ReadUInt32();
			size = r.ReadUInt32();
			sectionsCount = r.ReadUInt32();

			// read sections table
			Dictionary<uint, uint> offsets = new();
			Dictionary<uint, uint> sizes = new();
			uint minOffset = size;
			for (uint i=0; i<sectionsCount; ++i) {
				var tag = r.ReadUInt32();
				var offset = r.ReadUInt32();
				var size = r.ReadUInt32();
				sectionsTags.Add(tag);
				offsets[tag] = offset;
				sizes[tag] = size;
				if (minOffset > offset)
					minOffset = offset;
			}

			foreach (var tag in sectionsTags) {
				sectionsSortedByOffset.Add(tag);
			}
			sectionsSortedByOffset.Sort((uint a, uint b) => (int)(offsets[a] - offsets[b]));

			stringsStartOffset = r.BaseStream.Position - dat1_start;
			strings = r.ReadBytes((int)(minOffset + dat1_start - r.BaseStream.Position));

			// read sections
			foreach (uint tag in sectionsTags) {
				r.BaseStream.Position = offsets[tag] + dat1_start;
				_rawSections[tag] = r.ReadBytes((int)sizes[tag]);
			}
		}

		public virtual byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);

			Dictionary<uint, byte[]> bytes = new Dictionary<uint, byte[]>();
			foreach (var tag in sectionsSortedByOffset) {
				if (Sections.ContainsKey(tag))
					bytes[tag] = Sections[tag].Save();
				else
					bytes[tag] = _rawSections[tag];
			}

			// recalculate sections offsets
			var offset = 16 + 12 * sectionsCount + strings.Length;
			long zeroesToWrite = 0;
			if (offset % 4 != 0) {
				zeroesToWrite = (4 - (offset % 4));
				offset += zeroesToWrite;
			}

			Dictionary<uint, uint> offsets = new Dictionary<uint, uint>();
			foreach (var tag in sectionsSortedByOffset) {
				offsets[tag] = (uint)offset;
				offset += bytes[tag].Length;
				if (offset % 16 != 0)
					offset += 16 - (offset % 16);
			}

			// write header
			w.Write((uint)magic);
			w.Write((uint)unk1);
			w.Write((uint)offset);
			w.Write((uint)sectionsCount);
			foreach (var tag in sectionsTags) {
				w.Write((uint)tag);
				w.Write((uint)offsets[tag]);
				w.Write((uint)bytes[tag].Length);
			}

			w.Write(strings);

			offset = 16 + 12 * sectionsCount + strings.Length;
			for (var z = 0; z < zeroesToWrite; ++z)
				w.Write((byte)0);
			offset += zeroesToWrite;
			
			foreach (var tag in sectionsSortedByOffset) {
				w.Write(bytes[tag]);
				offset += bytes[tag].Length;
				if (offset % 16 != 0) {
					var padding = 16 - (offset % 16);
					for (int i = 0; i<padding; ++i)
						w.Write((byte)0);
					offset += padding;
				}
			}

			return s.ToArray();
		}

		#region strings block

		public string GetStringByOffset(uint offset) {
			if (offset < stringsStartOffset) return null;
			if (offset >= stringsStartOffset + strings.Length) return null;

			int i;
			for (i = (int)(offset - stringsStartOffset); i < strings.Length; ++i) {
				if (strings[i] == 0) break;
			}

			int length = (int)(i - (offset - stringsStartOffset));
			byte[] bytes = new byte[length];
			for (i = 0; i < length; ++i)
				bytes[i] = strings[offset - stringsStartOffset + i];

			return Encoding.ASCII.GetString(bytes);
		}

		private Dictionary<uint, string> stringByOffset = null;
		private Dictionary<string, uint> offsetByString = null;

		private void MakeStringsMaps() {
			if (stringByOffset != null) return;

			stringByOffset = new Dictionary<uint, string>();
			offsetByString = new Dictionary<string, uint>();

			int i = 0;
			int start = 0;
			while (i < strings.Length) {
				if (strings[i] == 0 || i == strings.Length - 1) {
					if (start == i) {
						++i;
						start = i;
						continue;
					}

					string s = Encoding.ASCII.GetString(strings, start, i - start);
					stringByOffset[(uint)(stringsStartOffset + start)] = s;
					offsetByString[s] = (uint)(stringsStartOffset + start);
					start = i + 1;
				}

				++i;
			}
		}

		public uint AddString(string s) {
			MakeStringsMaps();
			
			if (offsetByString.ContainsKey(s)) {
				return offsetByString[s];
			}

			uint offset = (uint)(stringsStartOffset + strings.Length);
			stringByOffset[offset] = s;
			offsetByString[s] = offset;

			var stream = new MemoryStream();
			var w = new BinaryWriter(stream);
			w.Write(strings);
			w.Write(Encoding.ASCII.GetBytes(s));
			w.Write((byte)0);
			strings = stream.ToArray();

			return offset;
		}

		public bool HasString(string s) {
			MakeStringsMaps();
			return offsetByString.ContainsKey(s);
		}

		#endregion
	}
}
