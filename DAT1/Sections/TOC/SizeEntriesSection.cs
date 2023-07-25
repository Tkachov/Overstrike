// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.TOC
{
    public class SizeEntriesSection : Section
    {
        public class SizeEntry
        {
            public uint Always1, Value, Index;
        }

        public List<SizeEntry> Entries = new List<SizeEntry>();

        public SizeEntriesSection(BinaryReader r, uint size)
        {
            uint count = size / 12;
            for (uint i = 0; i < count; ++i)
            {
                var a1 = r.ReadUInt32();
                var v = r.ReadUInt32();
                var ndx = r.ReadUInt32();
                Entries.Add(new SizeEntry() { Always1 = a1, Value = v, Index = ndx });
            }
        }

        override public byte[] Save()
        {
            var s = new MemoryStream();
            var w = new BinaryWriter(s);
            foreach (var e in Entries)
            {
                w.Write(e.Always1);
                w.Write(e.Value);
                w.Write(e.Index);
            }
            return s.ToArray();
        }
    }
}
