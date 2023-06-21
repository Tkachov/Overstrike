using DAT1.Sections;
using DAT1.Sections.Config;
using DAT1.Sections.Localization;
using DAT1.Sections.Texture;
using DAT1.Sections.TOC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DAT1
{
    public class DAT1
	{
		// header
		uint magic, unk1, size, sectionsCount;
		List<uint> sectionsTags = new List<uint>();
		List<uint> sectionsSortedByOffset = new List<uint>();

		long stringsStartOffset;
		byte[] strings;

		protected Dictionary<uint, Section> sections = new Dictionary<uint, Section>();


		private const uint ASSET_IDS_SECTION_TAG = 0x506D7B8A;
		private const uint ARCHIVES_SECTION_TAG = 0x398ABFF0;
        private const uint SIZE_ENTRIES_SECTION_TAG = 0x65BCF461;
		private const uint OFFSETS_SECTION_TAG = 0xDCD720B5;

        public ArchivesMapSection ArchivesSection => (ArchivesMapSection)sections[ARCHIVES_SECTION_TAG];
        public AssetIdsSection AssetIdsSection => (AssetIdsSection)sections[ASSET_IDS_SECTION_TAG];
		public SizeEntriesSection SizesSection => (SizeEntriesSection)sections[SIZE_ENTRIES_SECTION_TAG];
		public OffsetsSection OffsetsSection => (OffsetsSection)sections[OFFSETS_SECTION_TAG];

		protected DAT1()
		{

		}

		protected void Init(BinaryReader r)
		{
			long dat1_start = r.BaseStream.Position;

			magic = r.ReadUInt32();
			unk1 = r.ReadUInt32();
			size = r.ReadUInt32();
			sectionsCount = r.ReadUInt32();

			// read sections table
			Dictionary<uint, uint> offsets = new Dictionary<uint, uint>();
			Dictionary<uint, uint> sizes = new Dictionary<uint, uint>();
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
				sections[tag] = ReadSection(tag, r, sizes[tag]);
			}
		}

		public DAT1(BinaryReader r)
		{
			Init(r);
		}

        private Section ReadSection(uint tag, BinaryReader r, uint size) {
			switch (tag) {
				// toc
				case SIZE_ENTRIES_SECTION_TAG: return new SizeEntriesSection(r, size);
				case ARCHIVES_SECTION_TAG: return new ArchivesMapSection(r, size);
				case ASSET_IDS_SECTION_TAG: return new AssetIdsSection(r, size);
				case 0x6D921D7B: return new KeyAssetsSection(r, size);
				case OFFSETS_SECTION_TAG: return new OffsetsSection(r, size);
				case 0xEDE8ADA9: return new SpansSection(r, size);

				// config
				case ConfigTypeSection.TAG: return new ConfigTypeSection(this, r, size);
				case ConfigBuiltSection.TAG: return new ConfigBuiltSection(this, r, size);
				case ConfigReferencesSection.TAG: return new ConfigReferencesSection(r, size);

				// localization
				case EntriesCountSection.TAG: return new EntriesCountSection(r, size);
				case KeysDataSection.TAG: return new KeysDataSection(r, size);
				case KeysOffsetsSection.TAG: return new KeysOffsetsSection(r, size);
				case ValuesDataSection.TAG: return new ValuesDataSection(r, size);
				case ValuesOffsetsSection.TAG: return new ValuesOffsetsSection(r, size);

				// texture
				case TextureHeaderSection.TAG: return new TextureHeaderSection(r, size);
			}

			return new UnknownSection(r.ReadBytes((int)size));
		}

		public uint AddNewArchive(string filename) {
			ArchivesMapSection section = (ArchivesMapSection)sections[ARCHIVES_SECTION_TAG];
			uint new_index = 0;
			foreach (var a in section.Entries) {
				if (a.InstallBucket != 0)
					break;
				++new_index;
			}

			int min(int a, int b) => a < b ? a : b;

			byte[] bytes = Encoding.ASCII.GetBytes(filename);
			byte[] byteFilename = new byte[64];
			System.Buffer.BlockCopy(bytes, 0, byteFilename, 0, min(bytes.Length, 64));

			section.Entries.Insert((int)new_index, new ArchivesMapSection.ArchiveFileEntry() { InstallBucket = 0, Chunkmap = 10000 + new_index, Filename = byteFilename });
			return new_index;
		}

		public Dictionary<UInt64, int> FindAssetsIndexes(HashSet<UInt64> assetsIds) {
			Dictionary<UInt64, int> result = new Dictionary<ulong, int>();
			AssetIdsSection section = (AssetIdsSection)sections[ASSET_IDS_SECTION_TAG];

			int i = 0;
			while (i < section.Ids.Count && assetsIds.Count > 0) {
				var id = section.Ids[i];
				if (assetsIds.Contains(id)) {
					result[id] = i;
					assetsIds.Remove(id);
				}

				++i;
			}

			return result;
		}

		public virtual byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);

			Dictionary<uint, byte[]> bytes = new Dictionary<uint, byte[]>();
			foreach (var tag in sectionsSortedByOffset) {
				bytes[tag] = sections[tag].Save();
			}
			
			// recalculate sections offsets
			var offset = 16 + 12 * sectionsCount + strings.Length;

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

        public string GetStringByOffset(uint offset)
        {
			if (offset < stringsStartOffset) return null;
			if (offset >= stringsStartOffset + strings.Length) return null;

			int i;
			for (i = (int)(offset - stringsStartOffset); i < strings.Length; ++i)
			{
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
    }
}
