using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT1.Sections
{
    public class ReferencesSection : Section
    {
        public class ReferenceEntry
        {
            public UInt64 AssetId;
            public uint AssetPathStringOffset;
            public uint ExtensionHash;
        }

        public List<ReferenceEntry> Entries = new List<ReferenceEntry>();

        public ReferencesSection(BinaryReader r, uint size)
        {
            uint count = size / 16;
            for (uint i = 0; i < count; ++i)
            {
                var id = r.ReadUInt64();
                var so = r.ReadUInt32();
                var eh = r.ReadUInt32();
                Entries.Add(new ReferenceEntry() { AssetId = id, AssetPathStringOffset = so, ExtensionHash = eh });
            }
        }

        override public byte[] Save()
        {
            var s = new MemoryStream();
            var w = new BinaryWriter(s);
            foreach (var e in Entries)
            {
                w.Write((UInt64)e.AssetId);
                w.Write((uint)e.AssetPathStringOffset);
                w.Write((uint)e.ExtensionHash);
            }
            return s.ToArray();
        }
    }
}
