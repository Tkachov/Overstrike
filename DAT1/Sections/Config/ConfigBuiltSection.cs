using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT1.Sections.Config
{
    public class ConfigBuiltSection : SerializedSection
    {
        public const uint TAG = 0xE501186F; // Config Built

        public ConfigBuiltSection(DAT1 dat1, BinaryReader r, uint size) : base(dat1, r, size) { }
    }
}
