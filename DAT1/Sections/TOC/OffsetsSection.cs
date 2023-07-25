// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.TOC
{
    public class OffsetsSection : Section
    {
        public class OffsetEntry
        {
            public uint ArchiveIndex, Offset;
        }

        public List<OffsetEntry> Entries = new List<OffsetEntry>();

        public OffsetsSection(BinaryReader r, uint size)
        {
            uint count = size / 8;
            for (uint i = 0; i < count; ++i)
            {
                var a = r.ReadUInt32();
                var b = r.ReadUInt32();
                Entries.Add(new OffsetEntry() { ArchiveIndex = a, Offset = b });
            }
        }

        override public byte[] Save()
        {
            var s = new MemoryStream();
            var w = new BinaryWriter(s);
            foreach (var e in Entries)
            {
                w.Write(e.ArchiveIndex);
                w.Write(e.Offset);
            }
            return s.ToArray();
        }
    }
}
