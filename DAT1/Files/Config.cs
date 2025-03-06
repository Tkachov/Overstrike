// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Config;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static DAT1.Sections.Generic.ReferencesSection;

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

	public class Config_I30: DAT1 {
		public const uint MAGIC = 0x35F7AFA5;

		public Config_I30(BinaryReader r): base() {
			StringsEncoding = Encoding.UTF8;
			Init(r);
			Utils.Assert(TypeMagic == MAGIC, "Config_I30(): bad magic");
		}

		public ConfigTypeSection_I30 TypeSection => Section<ConfigTypeSection_I30>(ConfigTypeSection_I30.TAG);
		public ConfigBuiltSection_I30 ContentSection => Section<ConfigBuiltSection_I30>(ConfigBuiltSection_I30.TAG);
		public ConfigReferencesSection ReferencesSection => Section<ConfigReferencesSection>(ConfigReferencesSection.TAG);

		public override byte[] Save() {
			// access all sections to make them load into Section objects

			var typeData = TypeSection.Data;
			var builtData = ContentSection.Data;

			var references = new List<string>();
			if (HasSection(ConfigReferencesSection.TAG)) {
				foreach (var refEntry in ReferencesSection.Entries) {
					references.Add(GetStringByOffset(refEntry.AssetPathStringOffset));
				}
			}

			// save them back into raw

			ResetStringsBlock();
			AddString("Config Built File", true);

			TypeSection.Data = typeData;
			IntoRawSection(ConfigTypeSection_I30.TAG);

			ContentSection.Data = builtData;
			IntoRawSection(ConfigBuiltSection_I30.TAG);

			if (HasSection(ConfigReferencesSection.TAG)) {
				ReferencesSection.Entries.Clear();
				foreach (var reference in references) {
					var i = reference.LastIndexOf('.');
					if (i != -1) {
						var extension = reference[i..];

						var hash = CRC64.Hash(reference);
						var offset = AddString(reference, true);
						var extensionHash = CRC32.Hash(extension);

						ReferencesSection.Entries.Add(new ReferenceEntry() { AssetId = hash, AssetPathStringOffset = offset, ExtensionHash = extensionHash });
						continue;
					}

					// no extension => assume localization tag / wem reference
					{
						var hash = (0xEul << 60) | FNV(reference);
						var offset = AddString(reference, true);
						var extensionHash = (uint)0x3F08A054;

						ReferencesSection.Entries.Add(new ReferenceEntry() { AssetId = hash, AssetPathStringOffset = offset, ExtensionHash = extensionHash });
					}
				}
				IntoRawSection(ConfigReferencesSection.TAG);
			}

			// get DAT1 made of raw sections

			return base.Save();
		}

		private static uint FNV(string s) {
			uint result = 2166136261;
			s = s.ToLower();
			foreach (var c in s) {
				result = result * 16777619 ^ ((byte)c);
			}
			return result;
		}

		private static readonly byte[] EMPTY_CONFIG_BYTES = {
			0x31, 0x54, 0x41, 0x44, // 1TAD
			0xA5, 0xAF, 0xF7, 0x35, // config magic
			0x10, 0x00, 0x00, 0x00, // size = 16
			0x00, 0x00, 0x00, 0x00, // sections count = 0
		};

		public static Config_I30 Make() {
			using var ms = new MemoryStream(EMPTY_CONFIG_BYTES);
			using var br = new BinaryReader(ms);
			return new Config_I30(br);
		}
	}
}
