using System;
using System.IO;

namespace DAT1.Sections.Localization {
	public class EntriesCountSection: Section {
		public const uint TAG = 0xD540A903;

		public uint Count;

		public EntriesCountSection(BinaryReader r, uint size) {
			Count = r.ReadUInt32();
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);
			w.Write((UInt32)Count);
			return s.ToArray();
		}
	}
}
