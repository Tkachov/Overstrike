using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Localization {
	public class ValuesDataSection: StringsSection {
		public const uint TAG = 0x70A382B8;

		public ValuesDataSection(BinaryReader r, uint size) : base(r, size) { }
	}
}
