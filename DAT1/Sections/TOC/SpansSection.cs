using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.TOC
{
    public class SpansSection : Section
    {
        public class Span
        {
            public uint AssetIndex, Count;
        }

        public List<Span> Entries = new List<Span>();

        public SpansSection(BinaryReader r, uint size)
        {
            uint count = size / 8;
            for (uint i = 0; i < count; ++i)
            {
                var a = r.ReadUInt32();
                var b = r.ReadUInt32();
                Entries.Add(new Span() { AssetIndex = a, Count = b });
            }
        }

        override public byte[] Save()
        {
            var s = new MemoryStream();
            var w = new BinaryWriter(s);
            foreach (var e in Entries)
            {
                w.Write(e.AssetIndex);
                w.Write(e.Count);
            }
            return s.ToArray();
        }
    }
}
