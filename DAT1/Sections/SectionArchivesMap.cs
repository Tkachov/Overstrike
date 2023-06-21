using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT1.Sections
{
	public class SectionArchivesMap: Section
	{
		public class ArchiveFileEntry
		{
			public uint InstallBucket, Chunkmap;
			public byte[] Filename;

			public string GetFilename() {
				int cnt = Array.IndexOf(Filename, (byte)0);
				return Encoding.ASCII.GetString(Filename, 0, cnt);
			}
		}

		public List<ArchiveFileEntry> Entries = new List<ArchiveFileEntry>();

		public SectionArchivesMap(BinaryReader r, uint size) {
			uint count = size / 72;
			for (uint i = 0; i<count; ++i) {
				var ib = r.ReadUInt32();
				var cm = r.ReadUInt32();
				var fn = r.ReadBytes(64);
				Entries.Add(new ArchiveFileEntry() { InstallBucket = ib, Chunkmap = cm, Filename = fn });
			}
		}

		override public byte[] Save() {
			var s = new MemoryStream();
			var w = new BinaryWriter(s);
			foreach (var e in Entries) {
				w.Write((uint)e.InstallBucket);
				w.Write((uint)e.Chunkmap);
				w.Write(e.Filename);
			}
			return s.ToArray();
		}
	}
}
