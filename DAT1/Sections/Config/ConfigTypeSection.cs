using System.IO;
using DAT1.Sections.Generic;

namespace DAT1.Sections.Config
{
    public class ConfigTypeSection: SerializedSection
    {
        public const uint TAG = 0x4A128222; // Config Type

        public ConfigTypeSection(DAT1 dat1, BinaryReader r, uint size) : base(dat1, r, size) { }
    }
}
