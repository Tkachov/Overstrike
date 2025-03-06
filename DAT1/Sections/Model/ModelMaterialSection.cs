// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.Model {
	public class ModelMaterialSection: Section {
		public const uint TAG = 0x3250BB80; // Model Material

		public class Material {
			public string Path;
			public string SlotName;
			
			public ulong PathHash;
			public uint SlotHash;
			
			public void SetPath(string s) {
				Path = s;
				PathHash = CRC64.Hash(s);
			}

			public void SetSlotName(string s) {
				SlotName = s;
				SlotHash = CRC32.Hash(s, false);
			}
		}

		public List<Material> Materials;

		public override void Load(byte[] bytes, DAT1 container) {
			using var r = new BinaryReader(new MemoryStream(bytes));
			var size = bytes.Length;
			var count = size / 32;

			Materials = new();
			for (var i = 0; i < count; ++i) {
				var pathOffset = r.ReadUInt64();
				var slotOffset = r.ReadUInt64();

				Materials.Add(new Material {
					Path = container.GetStringByOffset((uint)pathOffset),
					SlotName = container.GetStringByOffset((uint)slotOffset),
				});
			}

			for (var i = 0; i < count; ++i) {
				var pathHash = r.ReadUInt64();
				var slotHash = r.ReadUInt32();
				r.ReadUInt32();

				/*
				Utils.Assert(pathHash == CRC64.Hash(Materials[i].Path));
				Utils.Assert(slotHash == CRC32.Hash(Materials[i].SlotName));
				*/
				Materials[i].PathHash = pathHash;
				Materials[i].SlotHash = slotHash;
			}
		}

		override public byte[] Save() {
			return null; // TODO
		}
	}
}
