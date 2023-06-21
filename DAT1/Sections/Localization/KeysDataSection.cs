using DAT1.Sections.Generic;
using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.Localization {
	public class KeysDataSection: StringsSection {
		public const uint TAG = 0x4D73CEBD;

		public KeysDataSection(BinaryReader r, uint size): base(r, size) {}
	}
}
