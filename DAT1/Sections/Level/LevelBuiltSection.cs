// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO;

namespace DAT1.Sections.Level {
	public class LevelBuiltSection: Section {
		public const uint TAG = 0x7CA7267D; // Level Built

		public ulong Unk1;
		public uint A, RegionsCount, SomeCount, D, ZonesCount, LinksCount, UnknownsCount, H;

		override public void Load(byte[] bytes, DAT1 container) {
			using var r = new BinaryReader(new MemoryStream(bytes));

			Unk1 = r.ReadUInt64();
			A = r.ReadUInt32();
			RegionsCount = r.ReadUInt32();
			SomeCount = r.ReadUInt32();
			D = r.ReadUInt32();
			ZonesCount = r.ReadUInt32();
			LinksCount = r.ReadUInt32();
			UnknownsCount = r.ReadUInt32();
			H = r.ReadUInt32();
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);

			w.Write(Unk1);
			w.Write(A);
			w.Write(RegionsCount);
			w.Write(SomeCount);
			w.Write(D);
			w.Write(ZonesCount);
			w.Write(LinksCount);
			w.Write(UnknownsCount);
			w.Write(H);

			return s.ToArray();
		}
	}
}
