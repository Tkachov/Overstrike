using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT1.Sections.Config
{
    public class ConfigReferencesSection: ReferencesSection
    {
        public const uint TAG = 0x58B8558A; // Config Asset Refs

        public ConfigReferencesSection(BinaryReader r, uint size): base(r, size) {}
    }
}
