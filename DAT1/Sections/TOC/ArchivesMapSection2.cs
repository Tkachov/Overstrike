// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DAT1.Sections.TOC
{
	// RCRA implementation
	// TODO: make a common interface instead of copy-pasting class entirely

	public class ArchivesMapSection2 : Section
    {
        public class ArchiveFileEntry
        {
            public byte[] Filename; // 40 bytes, something else goes after first '\0'

            // unknown stuff
            public ulong A, B;
            public uint C;
            public ushort D;
            public uint E;

            public string GetFilename()
            {
                int cnt = Array.IndexOf(Filename, (byte)0);
                return Encoding.ASCII.GetString(Filename, 0, cnt);
            }
        }

        public List<ArchiveFileEntry> Entries = new List<ArchiveFileEntry>();

        public ArchivesMapSection2(BinaryReader r, uint size)
        {
            uint count = size / 66;
            for (uint i = 0; i < count; ++i)
            {
				var fn = r.ReadBytes(40);
				var a = r.ReadUInt64();
				var b = r.ReadUInt64();
				var c = r.ReadUInt32();
				var d = r.ReadUInt16();
				var e = r.ReadUInt32();

				Entries.Add(new ArchiveFileEntry() { Filename = fn, A = a, B = b, C = c, D = d, E = e });
            }
        }

        override public byte[] Save()
        {
            var s = new MemoryStream();
            var w = new BinaryWriter(s);
            foreach (var e in Entries)
            {
                w.Write(e.Filename);
				w.Write(e.A);
				w.Write(e.B);
				w.Write(e.C);
				w.Write(e.D);
				w.Write(e.E);
			}
            return s.ToArray();
        }
    }
}
