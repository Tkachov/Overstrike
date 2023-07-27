// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.TOC
{
	// RCRA implementation
	// TODO: make a common interface instead of copy-pasting class entirely

	public class SizeEntriesSection2 : Section
    {
        public class SizeEntry
        {
            public uint Size, ArchiveIndex, Offset;
            public int HeaderOffset;
        }

        public List<SizeEntry> Entries = new List<SizeEntry>();

        public SizeEntriesSection2(BinaryReader r, uint size)
        {
            uint count = size / 12;
            for (uint i = 0; i < count; ++i)
            {
                var sz = r.ReadUInt32();
                var ai = r.ReadUInt32();
                var o = r.ReadUInt32();
				var ho = r.ReadInt32();
				Entries.Add(new SizeEntry() { Size = sz, ArchiveIndex = ai, Offset = o, HeaderOffset = ho });
            }
        }

        override public byte[] Save()
        {
            var s = new MemoryStream();
            var w = new BinaryWriter(s);
            foreach (var e in Entries)
            {
                w.Write(e.Size);
                w.Write(e.ArchiveIndex);
                w.Write(e.Offset);
				w.Write(e.HeaderOffset);
			}
            return s.ToArray();
        }
    }
}
