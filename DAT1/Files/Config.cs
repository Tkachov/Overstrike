﻿// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Config;
using System.IO;

namespace DAT1.Files {
	public class Config: DAT1 {
		public const uint MAGIC = 0x21A56F68;

		uint magic, dat1_size;
		byte[] unk;

		public Config(BinaryReader r): base() {
			magic = r.ReadUInt32();
			dat1_size = r.ReadUInt32();
			unk = r.ReadBytes(28);
			Utils.Assert(magic == MAGIC, "Config(): bad magic");

			Init(r);
		}

		public ConfigTypeSection TypeSection => Section<ConfigTypeSection>(ConfigTypeSection.TAG);
		public ConfigBuiltSection ContentSection => Section<ConfigBuiltSection>(ConfigBuiltSection.TAG);
		public ConfigReferencesSection ReferencesSection => Section<ConfigReferencesSection>(ConfigReferencesSection.TAG);

		public override byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);

			byte[] data = base.Save();
			w.Write(magic);
			w.Write((uint)data.Length);
			w.Write(unk);
			w.Write(data);

			return s.ToArray();
		}
	}
}
