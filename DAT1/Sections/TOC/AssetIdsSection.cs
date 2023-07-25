// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.TOC {
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
