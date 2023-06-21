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
