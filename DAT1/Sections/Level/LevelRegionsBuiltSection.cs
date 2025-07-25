// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.IO;

namespace DAT1.Sections.Level {
	public class LevelRegionsBuiltSection: ArraySection<LevelRegionsBuiltSection.Region> {
		public const uint TAG = 0x396F9418; // Level Regions Built

		public class Region {
			public ulong NameHash;
			public uint Index;
			public short Unk1;
			public short Unk2;
			public int ParentRegionIndex, FirstChildRegionIndex;
			public uint ChildRegionsCount;
			public int Index1;
			public uint Count1;
			public int Index2; // same space as Index1, usually Count1 also equals Count2
			public uint Count2, Index3, Count3;
			public short Unk3;
			public short Unk4;
		}

		protected override uint GetValueByteSize() { return 56; }

		protected override Region Read(BinaryReader r) {
			var result = new Region();

			result.NameHash = r.ReadUInt64();
			result.Index = r.ReadUInt32();
			result.Unk1 = r.ReadInt16();
			result.Unk2 = r.ReadInt16();
			result.ParentRegionIndex = r.ReadInt32();
			result.FirstChildRegionIndex = r.ReadInt32();
			result.ChildRegionsCount = r.ReadUInt32();
			result.Index1 = r.ReadInt32();
			result.Count1 = r.ReadUInt32();
			result.Index2 = r.ReadInt32();
			result.Count2 = r.ReadUInt32();
			result.Index3 = r.ReadUInt32();
			result.Count3 = r.ReadUInt32();
			result.Unk3 = r.ReadInt16();
			result.Unk4 = r.ReadInt16();

			return result;
		}

		protected override void Write(BinaryWriter w, Region v) {
			w.Write(v.NameHash);
			w.Write(v.Index);
			w.Write(v.Unk1);
			w.Write(v.Unk2);
			w.Write(v.ParentRegionIndex);
			w.Write(v.FirstChildRegionIndex);
			w.Write(v.ChildRegionsCount);
			w.Write(v.Index1);
			w.Write(v.Count1);
			w.Write(v.Index2);
			w.Write(v.Count2);
			w.Write(v.Index3);
			w.Write(v.Count3);
			w.Write(v.Unk3);
			w.Write(v.Unk4);
		}
	}
}
