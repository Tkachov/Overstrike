using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Localization {
	public class KeysOffsetsSection: UInt32ArraySection {
		public const uint TAG = 0xA4EA55B2;

		public KeysOffsetsSection(BinaryReader r, uint size) : base(r, size) { }
	}
}
