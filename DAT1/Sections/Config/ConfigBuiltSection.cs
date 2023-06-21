using System.IO;
using DAT1.Sections.Generic;

namespace DAT1.Sections.Config
{
    public class ConfigBuiltSection : SerializedSection
    {
        public const uint TAG = 0xE501186F; // Config Built

        public ConfigBuiltSection(DAT1 dat1, BinaryReader r, uint size) : base(dat1, r, size) { }
    }
}
