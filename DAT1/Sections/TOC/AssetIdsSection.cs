using System;
using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.TOC
{
    public class AssetIdsSection : Section
    {
        public List<ulong> Ids = new List<ulong>();

        public AssetIdsSection(BinaryReader r, uint size)
        {
            uint count = size / 8;
            for (uint i = 0; i < count; ++i)
            {
                Ids.Add(r.ReadUInt64());
            }
        }
        override public byte[] Save()
        {
            var s = new MemoryStream();
            var w = new BinaryWriter(s);
            foreach (var e in Ids)
            {
                w.Write(e);
            }
            return s.ToArray();
        }
    }
}
