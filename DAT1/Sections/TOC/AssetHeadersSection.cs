﻿// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.TOC
{
    public class AssetHeadersSection : Section // RCRA-specific
    {
		public const uint TAG = 0x654BDED9;

		public List<byte[]> Headers = new List<byte[]>();

        public AssetHeadersSection(BinaryReader r, uint size)
        {
            uint count = size / 36;
            for (uint i = 0; i < count; ++i)
            {
                var b = r.ReadBytes(36);
                Headers.Add(b);
            }
        }

        override public byte[] Save()
        {
            var s = new MemoryStream();
            var w = new BinaryWriter(s);
            foreach (var e in Headers)
            {
                w.Write(e);
            }
            return s.ToArray();
        }
    }
}
