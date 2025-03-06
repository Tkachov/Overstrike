// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DAT1.Sections.Material {
	public class MaterialSerializedDataSection: Section {
		public const uint TAG = 0xF5260180; // Material Serialized Data

		public List<string> Textures; // TODO: actually read all data, not just texture strings

		public override void Load(byte[] bytes, DAT1 container) {
			using var r = new BinaryReader(new MemoryStream(bytes));

			var sectionSize = r.ReadUInt32();
			var parametersCount = r.ReadUInt32();
			_ = r.ReadUInt32(); // unknown
			_ = r.ReadUInt32(); // unknown
			var parametersEndOffset = r.ReadUInt32();

			var textureParametersCount = r.ReadUInt32();
			var textureParametersOffset = r.ReadUInt32();
			var textureParametersEndOffset = r.ReadUInt32();

			// TODO: actually read parameters

			r.ReadBytes((int)(textureParametersOffset - r.BaseStream.Position));

			var offsets = new List<uint>();
			for (int i = 0; i < textureParametersCount; ++i) {
				var offset = r.ReadUInt32();
				_ = r.ReadUInt32(); // TODO: probably parameter crc32
				offsets.Add(offset);
			}

			if (r.BaseStream.Position < textureParametersEndOffset) {
				r.ReadBytes((int)(textureParametersEndOffset - r.BaseStream.Position));
			}

			var textureStrings = r.ReadBytes((int)(bytes.Length - r.BaseStream.Position));
			Textures = new();
			foreach (var offset in offsets) {
				var endOffset = offset;
				while (endOffset < textureStrings.Length && textureStrings[endOffset] != 0) {
					++endOffset;
				}
				if (endOffset == textureStrings.Length) {
					--endOffset;
				}

				Textures.Add(Encoding.UTF8.GetString(textureStrings, (int)offset, (int)(endOffset - offset)));
			}
		}

		override public byte[] Save() {
			return null; // TODO
		}
	}
}
