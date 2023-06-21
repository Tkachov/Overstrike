using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Localization {
	public class ValuesOffsetsSection: UInt32ArraySection {
		public const uint TAG = 0xF80DEEB4;

		public ValuesOffsetsSection(BinaryReader r, uint size): base(r, size) {}
	}
}
