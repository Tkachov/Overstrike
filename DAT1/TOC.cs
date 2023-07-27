// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.TOC;
using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DAT1 {
	public class AssetEntryBase {
		public int index;
		public ulong id;
		public uint archive;
	}

	/*
		i16 SO
		i20 MSMR
		i24 ?
		i29 RCRA
		i31 MM
	*/

	public abstract class TOCBase {
		public DAT1 Dat1 = null;
		protected string AssetArchivePath = null;
		public bool IsLoaded => Dat1 != null;

		public AssetIdsSection AssetIdsSection => Dat1.Section<AssetIdsSection>(AssetIdsSection.TAG);
		public OffsetsSection OffsetsSection => Dat1.Section<OffsetsSection>(OffsetsSection.TAG);
		public SpansSection SpansSection => Dat1.Section<SpansSection>(SpansSection.TAG);

		public abstract bool Load(string filename);
		public abstract bool Save(string filename);

		public virtual AssetEntryBase[] FindAssetEntriesByPath(string assetPath, bool stopOnFirst = false) {
			return FindAssetEntriesById(CRC64.Hash(assetPath), stopOnFirst);
		}

		public virtual AssetEntryBase[] FindAssetEntriesById(ulong assetId, bool stopOnFirst = false) {
			List<AssetEntryBase> results = new();

			if (IsLoaded) {
				var ids = AssetIdsSection.Ids;
				for (int i = 0; i < ids.Count; ++i) { // linear search =\
					if (ids[i] == assetId) {
						results.Add(GetAssetEntryByIndex(i));
						if (stopOnFirst) break;
					}
				}
			}

			return results.ToArray();
		}

		public virtual AssetEntryBase FindAssetEntryByPath(string assetPath) {
			return FindAssetEntriesByPath(assetPath, true)[0];
		}

		public virtual AssetEntryBase FindAssetEntryById(ulong assetId) {
			return FindAssetEntriesById(assetId, true)[0];
		}

		public abstract AssetEntryBase GetAssetEntryByIndex(int index);

		public virtual byte[] ExtractAsset(int index) {
			return ExtractAsset(GetAssetEntryByIndex(index));
		}

		public abstract byte[] ExtractAsset(AssetEntryBase AssetBase);

		protected class BlockHeader {
			public uint realOffset;
			//public uint unk1;
			public uint compOffset;
			//public uint unk2;
			public uint realSize;
			public uint compSize;
			//public uint unk3;
			//public uint unk4;
		}

		public enum ArchiveAddingImpl {
			DEFAULT,
			SMPCTOOL,
			SUITTOOL
		}

		public abstract uint AddNewArchive(string filename, ArchiveAddingImpl impl = ArchiveAddingImpl.DEFAULT);

		protected struct ArchivePair {
			public FileStream f;
			public bool compressed;
		}

		protected abstract ArchivePair OpenArchive(uint index);

		protected virtual ArchivePair OpenArchive(string fn) {
			string full = Path.Combine(AssetArchivePath, fn);
			FileStream fs = File.OpenRead(full);

			ArchivePair p;
			p.f = fs;
			p.compressed = DSAR.IsCompressed(fs);
			return p;
		}
	}

	public class TOC_I20: TOCBase {
		private const uint MAGIC = 0x77AF12AF;

		public SizeEntriesSection_I16 SizesSection => Dat1.Section<SizeEntriesSection_I16>(SizeEntriesSection_I16.TAG);
		public ArchivesMapSection_I20 ArchivesSection => Dat1.Section<ArchivesMapSection_I20>(ArchivesMapSection_I20.TAG);

		public class AssetEntry: AssetEntryBase {
			public uint offset;
			public uint size;
		}

		public override bool Load(string filename) {
			try {
				var f = File.OpenRead(filename);
				var r = new BinaryReader(f);
				uint magic = r.ReadUInt32();
				if (magic != MAGIC) {
					return false;
				}

				uint uncompressedLength = r.ReadUInt32();

				int length = (int)(f.Length - 8);
				byte[] bytes = ZlibStream.UncompressBuffer(r.ReadBytes(length));
				r.Close();
				r.Dispose();
				f.Close();
				f.Dispose();

				Dat1 = new DAT1(new BinaryReader(new MemoryStream(bytes)));
				AssetArchivePath = Path.GetDirectoryName(filename);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		public override bool Save(string filename) {
			if (!IsLoaded)
				return false;

			byte[] uncompressed = Dat1.Save();
			/// byte[] compressed = ZlibStream.CompressBuffer(uncompressed);
			
			byte[] compressed;

			using (var ms = new MemoryStream()) {
				Stream compressor =
					new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.BestCompression); // Default); // None);

				using (compressor) {
					compressor.Write(uncompressed, 0, uncompressed.Length);
					compressor.Flush();
				}
				compressed = ms.ToArray();
			}

			using (var f = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None)) {
				using (var w = new BinaryWriter(f)) {
					w.Write((uint)MAGIC);
					w.Write((uint)uncompressed.Length);
					w.Write(compressed);
				}
			}

			return true;
		}

        public override AssetEntryBase GetAssetEntryByIndex(int index) {
			try {
				AssetEntry result = new() {
					index = index,
					id = AssetIdsSection.Ids[index],
					archive = OffsetsSection.Entries[index].ArchiveIndex,
					offset = OffsetsSection.Entries[index].Offset,
					size = SizesSection.Entries[index].Value
				};
				return result;
            } catch (Exception) {}

            return null;
		}

		public void UpdateAssetEntry(AssetEntry entry) {
			int index = entry.index;
			AssetIdsSection.Ids[index] = entry.id;
			OffsetsSection.Entries[index].ArchiveIndex = entry.archive;
			OffsetsSection.Entries[index].Offset = entry.offset;
			SizesSection.Entries[index].Value = entry.size;
		}

        public override byte[] ExtractAsset(AssetEntryBase AssetBase)
		{
			if (!IsLoaded)
				return null;

			AssetEntry Asset = (AssetEntry)AssetBase;
			if (Asset == null)
				return null;

			ArchivePair p = OpenArchive(Asset.archive);
			return DSAR.ExtractAsset(p.f, p.compressed, (int)Asset.offset, (int)Asset.size);
        }

		protected override ArchivePair OpenArchive(uint index) { 
			return OpenArchive(ArchivesSection.Entries[(int)index].GetFilename());
		}

		public override uint AddNewArchive(string filename, ArchiveAddingImpl impl = ArchiveAddingImpl.DEFAULT) {
			switch (impl) {
				case ArchiveAddingImpl.DEFAULT: return AddNewArchive_Default(filename);
				case ArchiveAddingImpl.SMPCTOOL: return AddNewArchive_SMPCTool(filename);
				case ArchiveAddingImpl.SUITTOOL: return AddNewArchive_SuitTool(filename);
				default: return 0;
			}
		}

		private uint AddNewArchive_Default(string filename) {
			int index = 0;
			foreach (var entry in ArchivesSection.Entries) {
				if (entry.InstallBucket != 0) break;
				++index;
			}

			byte[] bytes = new byte[64];
			for (int i = 0; i < 64; ++i) bytes[i] = 0;
			byte[] fn = Encoding.ASCII.GetBytes(filename);
			fn.CopyTo(bytes, 0);
			bytes[fn.Length] = 0;

			ArchivesSection.Entries.Insert(index, new ArchivesMapSection_I20.ArchiveFileEntry() {
				InstallBucket = 0,
				Chunkmap = (uint)(10000 + index),
				Filename = bytes
			});

			return (uint)index;
		}

		private uint AddNewArchive_SMPCTool(string filename) {
			int index = ArchivesSection.Entries.Count;

			byte[] bytes = new byte[64];
			for (int i = 0; i < 64; ++i) bytes[i] = 0;
			byte[] fn = Encoding.ASCII.GetBytes(filename);
			fn.CopyTo(bytes, 0);
			bytes[fn.Length] = 0;

			ArchivesSection.Entries.Add(new ArchivesMapSection_I20.ArchiveFileEntry() {
				InstallBucket = 0,
				Chunkmap = 0,
				Filename = bytes
			});

			return (uint)index;
		}

		private uint AddNewArchive_SuitTool(string filename) {
			int index = ArchivesSection.Entries.Count;

			byte[] bytes = new byte[64];
			for (int i = 0; i < 64; ++i) bytes[i] = 0;
			byte[] fn = Encoding.ASCII.GetBytes(filename);
			fn.CopyTo(bytes, 0);
			bytes[fn.Length] = 0;

			ArchivesSection.Entries.Add(new ArchivesMapSection_I20.ArchiveFileEntry() {
				InstallBucket = 0,
				Chunkmap = (uint)(10000 + index),
				Filename = bytes
			});

			return (uint)index;
		}
	}
}
