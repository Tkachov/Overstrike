// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DAT1.Sections.HibernateZone {
	public class ZoneHibernateObjectsSection: Section {
		public const uint TAG = 0x296E6DC7; // Zone Hibernate Objects

		public class ZoneHibernateObjectsHeader {
			public uint A, B, C, D;
			public uint Offset1, GroupsCount;
			public uint Offset2, ItemsCount;

			public void Read(BinaryReader r) {
				A = r.ReadUInt32();
				B = r.ReadUInt32();
				C = r.ReadUInt32();
				D = r.ReadUInt32();
				Offset1 = r.ReadUInt32();
				GroupsCount = r.ReadUInt32();
				Offset2 = r.ReadUInt32();
				ItemsCount = r.ReadUInt32();
			}
		}

		public ZoneHibernateObjectsHeader ModelsHeader = new();
		public ZoneHibernateObjectsHeader VfxHeader = new();
		public ZoneHibernateObjectsHeader LightsHeader = new();

		public uint OffsetToPayload;
		public uint UnknownCount;
		public byte[] Raw = Array.Empty<byte>();

		public class ZoneHibernateObjectsGroup {
			public uint A, B, C, D;
			public uint Flags; // almost always 0?
			public uint Count, FirstItemIndex, Count2; // Count == Count2

			public void Read(BinaryReader r) {
				A = r.ReadUInt32();
				B = r.ReadUInt32();
				C = r.ReadUInt32();
				D = r.ReadUInt32();
				Flags = r.ReadUInt32();
				Count = r.ReadUInt32();
				FirstItemIndex = r.ReadUInt32();
				Count2 = r.ReadUInt32();
			}
		}

		public class ZoneHibernateObjectsItem {
			public uint A, B, C, D;
			public uint E, F, G, H; // F is always 0 for model and vfx items?
			public uint I, J;

			public void Read(BinaryReader r) {
				A = r.ReadUInt32();
				B = r.ReadUInt32();
				C = r.ReadUInt32();
				D = r.ReadUInt32();
				E = r.ReadUInt32();
				F = r.ReadUInt32();
				G = r.ReadUInt32();
				H = r.ReadUInt32();
				I = r.ReadUInt32();
				J = r.ReadUInt32();
			}
		}

		public List<ZoneHibernateObjectsGroup> ModelGroups = new();
		public List<ZoneHibernateObjectsItem> ModelItems = new();

		public List<ZoneHibernateObjectsGroup> VfxGroups = new();
		public List<ZoneHibernateObjectsItem> VfxItems = new();

		public List<ZoneHibernateObjectsGroup> LightGroups = new();
		public List<ZoneHibernateObjectsItem> LightItems = new();

		override public void Load(byte[] bytes, DAT1 container) {
			using var r = new BinaryReader(new MemoryStream(bytes));

			ModelsHeader.Read(r);
			VfxHeader.Read(r);
			LightsHeader.Read(r);

			OffsetToPayload = r.ReadUInt32();
			UnknownCount = r.ReadUInt32();

			Debug.Assert(ModelsHeader.Offset1 <= ModelsHeader.Offset2);
			Debug.Assert(VfxHeader.Offset1 <= VfxHeader.Offset2);
			Debug.Assert(LightsHeader.Offset1 <= LightsHeader.Offset2);

			uint minOffset = Math.Min(Math.Min(ModelsHeader.Offset1, VfxHeader.Offset1), LightsHeader.Offset1);
			Debug.Assert(minOffset == ModelsHeader.Offset1);

			Raw = r.ReadBytes((int)(minOffset - r.BaseStream.Position));
			// seems like `Raw` is always (8 + UnknownCount * 16) bytes long

			void ReadGroups(ref List<ZoneHibernateObjectsGroup> list, uint count) {
				for (int i = 0; i < count; ++i) {
					var e = new ZoneHibernateObjectsGroup();
					e.Read(r);
					list.Add(e);
				}
			}

			void ReadItems(ref List<ZoneHibernateObjectsItem> list, uint count) {
				for (int i = 0; i < count; ++i) {
					var e = new ZoneHibernateObjectsItem();
					e.Read(r);
					list.Add(e);
				}
			}

			ReadGroups(ref ModelGroups, ModelsHeader.GroupsCount);
			ReadItems(ref ModelItems, ModelsHeader.ItemsCount);

			ReadGroups(ref VfxGroups, VfxHeader.GroupsCount);
			ReadItems(ref VfxItems, VfxHeader.ItemsCount);

			ReadGroups(ref LightGroups, LightsHeader.GroupsCount);
			ReadItems(ref LightItems, LightsHeader.ItemsCount);
		}

		override public byte[] Save() {
			return null; // TODO
		}
	}
}
