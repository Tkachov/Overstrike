// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;

namespace DAT1.Sections.Generic {
	public class ReferencesSection : Section
    {
        public class ReferenceEntry
        {
            public ulong AssetId;
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
                w.Write(e.AssetId);
                w.Write(e.AssetPathStringOffset);
                w.Write(e.ExtensionHash);
            }
            return s.ToArray();
        }
    }
}
