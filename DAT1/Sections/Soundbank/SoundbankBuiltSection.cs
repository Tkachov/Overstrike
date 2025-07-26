// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO;

namespace DAT1.Sections.Soundbank {
	public class SoundbankBuiltSection: Section {
		public const uint TAG = 0x4765351A; // Sound Bank Built

		public uint BankId, BnkSize;
		public uint A, B, C, D;
		public uint E, F, G, H;
		public uint I, J, K, L;
		public uint M, N;

		override public void Load(byte[] bytes, DAT1 container) {
			using var r = new BinaryReader(new MemoryStream(bytes));

			BankId = r.ReadUInt32();
			BnkSize = r.ReadUInt32();

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
			K = r.ReadUInt32();
			L = r.ReadUInt32();

			M = r.ReadUInt32();
			N = r.ReadUInt32();
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);

			w.Write(BankId);
			w.Write(BnkSize);

			w.Write(A);
			w.Write(B);
			w.Write(C);
			w.Write(D);

			w.Write(E);
			w.Write(F);
			w.Write(G);
			w.Write(H);

			w.Write(I);
			w.Write(J);
			w.Write(K);
			w.Write(L);

			w.Write(M);
			w.Write(N);

			return s.ToArray();
		}
	}
}
