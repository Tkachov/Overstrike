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
		private const uint MAGIC = 0x44415431;

		// header
		uint magic, unk1, size;
		ushort sectionsCount, unknownsCount;
		List<uint> sectionsTags = new();
		List<uint> sectionsSortedByOffset = new();
		byte[] unknowns;

		long stringsStartOffset;
		byte[] strings;

		public uint Magic => magic;
		public uint TypeMagic => unk1;
		public Encoding StringsEncoding = Encoding.ASCII;

		private Dictionary<uint, byte[]> _rawSections = new();
		public Dictionary<uint, Section> Sections = new();

		public Dictionary<uint, uint> OriginalSectionsOffsets = new();

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

		public T AddSection<T>(uint tag, T section) where T: Section {
			Sections[tag] = section;

			if (!_rawSections.ContainsKey(tag)) {
				_rawSections[tag] = new byte[0];
				sectionsTags.Add(tag);
				sectionsSortedByOffset.Add(tag);

				sectionsCount = (ushort)_rawSections.Count;
				ResetStringsBlock(); // all offsets are no longer actual
			}

			return section;
		}

		public bool HasSection(uint tag) => _rawSections.ContainsKey(tag);

		public List<uint> GetSectionTags() => sectionsTags;

		public byte[] GetUnknowns() => unknowns;

		public byte[] GetRawSection(uint tag) => (_rawSections.ContainsKey(tag) ? _rawSections[tag] : null);

		protected DAT1() {}

		public DAT1(BinaryReader r) {
			Init(r);
		}

		protected void Init(BinaryReader r) {
			long dat1_start = r.BaseStream.Position;

			magic = r.ReadUInt32();
			unk1 = r.ReadUInt32();
			size = r.ReadUInt32();
			sectionsCount = r.ReadUInt16();
			unknownsCount = r.ReadUInt16();

			if (magic != MAGIC) {
				throw new System.Exception("Not DAT1");
			}

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

			unknowns = r.ReadBytes(unknownsCount * 8);

			stringsStartOffset = r.BaseStream.Position - dat1_start;
			strings = r.ReadBytes((int)(minOffset + dat1_start - r.BaseStream.Position));

			// read sections
			foreach (uint tag in sectionsTags) {
				r.BaseStream.Position = offsets[tag] + dat1_start;
				_rawSections[tag] = r.ReadBytes((int)sizes[tag]);
				OriginalSectionsOffsets[tag] = offsets[tag];
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
			long offset = 16 + 12 * sectionsCount + unknowns.Length + strings.Length;

			Dictionary<uint, uint> offsets = new Dictionary<uint, uint>();
			foreach (var tag in sectionsSortedByOffset) {
				if (offset % 16 != 0)
					offset += 16 - (offset % 16);

				offsets[tag] = (uint)offset;
				offset += bytes[tag].Length;
			}

			// write header
			w.Write((uint)magic);
			w.Write((uint)unk1);
			w.Write((uint)offset);
			w.Write((ushort)sectionsCount);
			w.Write((ushort)unknownsCount);
			sectionsTags.Sort();
			foreach (var tag in sectionsTags) {
				w.Write((uint)tag);
				w.Write((uint)offsets[tag]);
				w.Write((uint)bytes[tag].Length);
			}

			w.Write(unknowns);

			w.Write(strings);

			offset = 16 + 12 * sectionsCount + unknowns.Length + strings.Length;
			
			foreach (var tag in sectionsSortedByOffset) {
				if (offset % 16 != 0) {
					var padding = 16 - (offset % 16);
					for (int i = 0; i < padding; ++i)
						w.Write((byte)0);
					offset += padding;
				}

				w.Write(bytes[tag]);
				offset += bytes[tag].Length;				
			}

			return s.ToArray();
		}

		#region strings block

		public long FirstStringOffset { get => stringsStartOffset; }

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

			return StringsEncoding.GetString(bytes);
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

					string s = StringsEncoding.GetString(strings, start, i - start);
					stringByOffset[(uint)(stringsStartOffset + start)] = s;
					offsetByString[s] = (uint)(stringsStartOffset + start);
					start = i + 1;
				}

				++i;
			}
		}

		public uint AddString(string s, bool alwaysAdd = false) {
			MakeStringsMaps();

			if (!alwaysAdd) {
				if (offsetByString.ContainsKey(s)) {
					return offsetByString[s];
				}
			}

			uint offset = (uint)(stringsStartOffset + strings.Length);
			stringByOffset[offset] = s;
			offsetByString[s] = offset;

			var stream = new MemoryStream();
			var w = new BinaryWriter(stream);
			w.Write(strings);
			w.Write(StringsEncoding.GetBytes(s));
			w.Write((byte)0);
			strings = stream.ToArray();

			return offset;
		}

		public bool HasString(string s) {
			MakeStringsMaps();
			return offsetByString.ContainsKey(s);
		}

		public void ResetStringsBlock() {
			stringByOffset = null;
			offsetByString = null;
			strings = new byte[0];

			stringsStartOffset = 16 + 12 * sectionsCount + unknowns.Length;
		}

		public void IntoRawSection(uint tag) {
			Utils.Assert(Sections.ContainsKey(tag));

			_rawSections[tag] = Sections[tag].Save();
			Sections.Remove(tag);
		}

		#endregion
	}
}
