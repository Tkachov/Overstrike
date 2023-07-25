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
    public class ArchivesMapSection : Section
    {
        public class ArchiveFileEntry
        {
            public uint InstallBucket, Chunkmap;
            public byte[] Filename;

            public string GetFilename()
            {
                int cnt = Array.IndexOf(Filename, (byte)0);
                return Encoding.ASCII.GetString(Filename, 0, cnt);
            }
        }

        public List<ArchiveFileEntry> Entries = new List<ArchiveFileEntry>();

        public ArchivesMapSection(BinaryReader r, uint size)
        {
            uint count = size / 72;
            for (uint i = 0; i < count; ++i)
            {
                var ib = r.ReadUInt32();
                var cm = r.ReadUInt32();
                var fn = r.ReadBytes(64);
                Entries.Add(new ArchiveFileEntry() { InstallBucket = ib, Chunkmap = cm, Filename = fn });
            }
        }

        override public byte[] Save()
        {
            var s = new MemoryStream();
            var w = new BinaryWriter(s);
            foreach (var e in Entries)
            {
                w.Write(e.InstallBucket);
                w.Write(e.Chunkmap);
                w.Write(e.Filename);
            }
            return s.ToArray();
        }
    }
}
