// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DAT1.Sections.TOC {
	public class ArchiveFileEntryBase {
		public byte[] Filename;

		public string GetFilename() {
			int cnt = Array.IndexOf(Filename, (byte)0);
			return Encoding.ASCII.GetString(Filename, 0, cnt);
		}
	}

	public class ArchivesMapSection_I20: ArraySection<ArchivesMapSection_I20.ArchiveFileEntry> {
		public class ArchiveFileEntry: ArchiveFileEntryBase {
			public uint InstallBucket, Chunkmap;
		}

		public const uint TAG = 0x398ABFF0; // Archive TOC File Metadata

		public List<ArchiveFileEntry> Entries => Values;

		protected override uint GetValueByteSize() { return 72; }

		protected override ArchiveFileEntry Read(BinaryReader r) {
			var ib = r.ReadUInt32();
			var cm = r.ReadUInt32();
			var fn = r.ReadBytes(64);
			return new ArchiveFileEntry() { InstallBucket = ib, Chunkmap = cm, Filename = fn };
		}

		protected override void Write(BinaryWriter w, ArchiveFileEntry v) {
			w.Write(v.InstallBucket);
			w.Write(v.Chunkmap);
			w.Write(v.Filename);
		}
	}

	public class ArchivesMapSection_I29: ArraySection<ArchivesMapSection_I29.ArchiveFileEntry> {
		public class ArchiveFileEntry: ArchiveFileEntryBase {
			public ulong A, B;
			public uint C;
			public ushort D;
			public uint E;
		}

		public const uint TAG = 0x398ABFF0;

		public List<ArchiveFileEntry> Entries => Values;

		protected override uint GetValueByteSize() { return 66; }

		protected override ArchiveFileEntry Read(BinaryReader r) {
			var fn = r.ReadBytes(40);
			var a = r.ReadUInt64();
			var b = r.ReadUInt64();
			var c = r.ReadUInt32();
			var d = r.ReadUInt16();
			var e = r.ReadUInt32();
			return new ArchiveFileEntry() { Filename = fn, A = a, B = b, C = c, D = d, E = e };
		}

		protected override void Write(BinaryWriter w, ArchiveFileEntry v) {
			w.Write(v.Filename);
			w.Write(v.A);
			w.Write(v.B);
			w.Write(v.C);
			w.Write(v.D);
			w.Write(v.E);
		}
	}
}
