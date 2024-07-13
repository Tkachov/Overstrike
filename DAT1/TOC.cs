// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.TOC;
using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DAT1 {
	public abstract class TOCBase {
		public DAT1 Dat1 = null;
		protected string AssetArchivePath = null;
		public bool IsLoaded => Dat1 != null;

		public AssetIdsSection AssetIdsSection => Dat1.Section<AssetIdsSection>(AssetIdsSection.TAG);
		public SpansSection SpansSection => Dat1.Section<SpansSection>(SpansSection.TAG);

		public abstract bool Load(string filename);
		public abstract bool Save(string filename);

		#region assets

		#region find asset indexes

		public virtual int FindAssetIndex(byte span, ulong assetId) {
			var spanEntry = SpansSection.Entries[span];
			var index = AssetIdsSection.Ids.BinarySearch((int)spanEntry.AssetIndex, (int)spanEntry.Count, assetId, null); // default comparer
			if (index < 0) return -1;
			if (AssetIdsSection.Ids[index] != assetId) return -1;
			return index;
		}

		public virtual int[] FindAssetIndexesByPath(string assetPath, bool stopOnFirst = false) {
			return FindAssetIndexesById(CRC64.Hash(assetPath), stopOnFirst);
		}

		public virtual int[] FindAssetIndexesById(ulong assetId, bool stopOnFirst = false) {
			List<int> results = new();

			for (var spanIndex = 0; spanIndex < SpansSection.Entries.Count; ++spanIndex) {
				var index = FindAssetIndex((byte)spanIndex, assetId);
				if (index != -1) {
					results.Add(index);
					if (stopOnFirst) break;
				}
			}

			return results.ToArray();
		}

		public virtual int FindFirstAssetIndexByPath(string assetPath) {
			var arr = FindAssetIndexesByPath(assetPath, true);
			return (arr.Length > 0 ? arr[0] : -1);
		}

		public virtual int FindFirstAssetIndexById(ulong assetId) {
			var arr = FindAssetIndexesById(assetId, true);
			return (arr.Length > 0 ? arr[0] : -1);
		}

		#endregion
		#region get asset info by index

		public virtual ulong? GetAssetIdByAssetIndex(int index) => (0 <= index && index < AssetIdsSection.Ids.Count ? AssetIdsSection.Ids[index] : null);

		public virtual byte? GetSpanIndexByAssetIndex(int assetIndex) {
			byte span = 0;
			foreach (var entry in SpansSection.Entries) {
				if (entry.AssetIndex <= assetIndex && assetIndex < entry.AssetIndex + entry.Count) {
					return span;
				}

				++span;
			}

			return null;
		}

		public abstract uint? GetArchiveIndexByAssetIndex(int index);
		public abstract uint? GetOffsetInArchiveByAssetIndex(int index);
		public abstract uint? GetSizeInArchiveByAssetIndex(int index);

		#endregion
		#region extract asset

		public virtual byte[] ExtractAsset(int index) {
			if (!IsLoaded) return null;

			var archiveIndex = GetArchiveIndexByAssetIndex(index);
			var archiveOffset = GetOffsetInArchiveByAssetIndex(index);
			var size = GetSizeInArchiveByAssetIndex(index);
			if (archiveIndex == null || archiveOffset == null || size == null) return null;

			var archiveName = GetArchiveFilename((uint)archiveIndex);
			var archive = OpenArchive(archiveName);
			return DSAR.ExtractAsset(archive, (long)archiveOffset, (long)size);
		}

		public byte[] GetAssetBytes(byte span, ulong assetId) => ExtractAsset(FindAssetIndex(span, assetId));
		public byte[] GetAssetBytes(ulong assetId) => ExtractAsset(FindFirstAssetIndexById(assetId));
		public byte[] GetAssetBytes(string path) => ExtractAsset(FindFirstAssetIndexByPath(path));

		public BinaryReader GetAssetReader(byte span, ulong assetId) => new(new MemoryStream(GetAssetBytes(span, assetId)));
		public BinaryReader GetAssetReader(ulong assetId) => new(new MemoryStream(GetAssetBytes(assetId)));
		public BinaryReader GetAssetReader(string path) => new(new MemoryStream(GetAssetBytes(path)));

		#endregion
		#region modify assets

		/*
			Adds a new asset into all required TOC sections and returns index of that asset.
			Some of the structures will not be filled with valid values!
			Assets order will be invalid, so SortAssets() call is required!
		*/
		public abstract int AddAsset(byte span, ulong assetId);

		public virtual void UpdateAsset(AssetUpdaterBase updater) {
			updater.Apply(this);
		}

		public abstract void SortAssets();

		public virtual int FindOrAddAsset(byte span, ulong assetId) {
			int assetIndex = FindAssetIndex(span, assetId);
			if (assetIndex == -1) {
				assetIndex = AddAsset(span, assetId);
			}
			return assetIndex;
		}

		public abstract class AssetUpdaterBase {
			protected int _index;

			protected bool _updateAssetId = false;
			protected ulong _assetId;

			protected bool _updateArchiveIndex = false;
			protected uint _archiveIndex;

			protected bool _updateArchiveOffset = false;
			protected uint _archiveOffset;

			protected bool _updateSize = false;
			protected uint _size;

			public AssetUpdaterBase(int index) {
				_index = index;
			}

			public AssetUpdaterBase UpdateAssetId(ulong assetId) {
				_updateAssetId = true;
				_assetId = assetId;
				return this;
			}

			public AssetUpdaterBase UpdateArchiveIndex(uint archiveIndex) {
				_updateArchiveIndex = true;
				_archiveIndex = archiveIndex;
				return this;
			}

			public AssetUpdaterBase UpdateArchiveOffset(uint offset) {
				_updateArchiveOffset = true;
				_archiveOffset = offset;
				return this;
			}

			public AssetUpdaterBase UpdateSize(uint size) {
				_updateSize = true;
				_size = size;
				return this;
			}

			public abstract void Apply(TOCBase toc);
		}

		#endregion
		
		#endregion
		#region archives

		public abstract uint GetArchivesCount();
		public abstract string GetArchiveFilename(uint index);

		protected virtual FileStream OpenArchive(string fn) {
			string full = Path.Combine(AssetArchivePath, fn);
			return File.OpenRead(full);
		}

		#region modify archives

		public enum ArchiveAddingImpl {
			DEFAULT,
			SMPCTOOL,
			SUITTOOL
		}

		public abstract uint AddNewArchive(string filename, ArchiveAddingImpl impl = ArchiveAddingImpl.DEFAULT);

		public uint FindOrAddArchive(string filename, ArchiveAddingImpl mode) {
			for (uint index = 0; index < GetArchivesCount(); ++index) {
				if (GetArchiveFilename(index) == filename) {
					return index;
				}
			}

			return AddNewArchive(filename, mode);
		}

		#endregion
		
		#endregion
	}

	public class TOC_I20: TOCBase {
		private const uint MAGIC = 0x77AF12AF;

		public OffsetsSection OffsetsSection => Dat1.Section<OffsetsSection>(OffsetsSection.TAG);
		public SizeEntriesSection_I16 SizesSection => Dat1.Section<SizeEntriesSection_I16>(SizeEntriesSection_I16.TAG);
		public ArchivesMapSection_I20 ArchivesSection => Dat1.Section<ArchivesMapSection_I20>(ArchivesMapSection_I20.TAG);

		public override bool Load(string filename) {
			try {
				using var f = File.OpenRead(filename);
				using var r = new BinaryReader(f);

				uint magic = r.ReadUInt32();
				if (magic != MAGIC) {
					return false;
				}

				uint uncompressedLength = r.ReadUInt32();
				int length = (int)(f.Length - 8);
				byte[] bytes = ZlibStream.UncompressBuffer(r.ReadBytes(length));

				Dat1 = new DAT1(new BinaryReader(new MemoryStream(bytes)));
				AssetArchivePath = Path.GetDirectoryName(filename);

				return true;
			} catch (Exception) {
				return false;
			}
		}

		public override bool Save(string filename) {
			if (!IsLoaded) return false;

			byte[] uncompressed = Dat1.Save();
			byte[] compressed; /// = ZlibStream.CompressBuffer(uncompressed);

			using (var ms = new MemoryStream()) {
				Stream compressor = new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.BestCompression); // Default); // None);
				using (compressor) {
					compressor.Write(uncompressed, 0, uncompressed.Length);
					compressor.Flush();
				}
				compressed = ms.ToArray();
			}

			using var f = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
			using var w = new BinaryWriter(f);
			w.Write(MAGIC);
			w.Write((uint)uncompressed.Length);
			w.Write(compressed);

			return true;
		}

		#region assets

		#region get asset info by index

		public override uint? GetArchiveIndexByAssetIndex(int index) => (0 <= index && index < OffsetsSection.Entries.Count ? OffsetsSection.Entries[index].ArchiveIndex : null);
		public override uint? GetOffsetInArchiveByAssetIndex(int index) => (0 <= index && index < OffsetsSection.Entries.Count ? OffsetsSection.Entries[index].Offset : null);
		public override uint? GetSizeInArchiveByAssetIndex(int index) => (0 <= index && index < SizesSection.Entries.Count ? SizesSection.Entries[index].Value : null);

		#endregion
		#region modify assets

		public override int AddAsset(byte span, ulong assetId) {
			var spansSection = SpansSection;
			var spanEntry = spansSection.Entries[span];

			// insert into right place (SortAssets() is still required to fix SizesSection.Entries.Index)
			var assetIndex = AssetIdsSection.Ids.BinarySearch((int)spanEntry.AssetIndex, (int)spanEntry.Count, assetId, null); // default comparer
			Utils.Assert(assetIndex < 0, "AddAsset() should not be called if asset is already present");
			assetIndex = ~assetIndex;			

			++spanEntry.Count;
			for (int i = span + 1; i < spansSection.Entries.Count; ++i) {
				++spansSection.Entries[i].AssetIndex;
			}

			AssetIdsSection.Ids.Insert(assetIndex, assetId);
			SizesSection.Entries.Insert(assetIndex, new SizeEntriesSection_I16.SizeEntry() { Always1 = 1, Index = (uint)assetIndex, Value = 0 });
			OffsetsSection.Entries.Insert(assetIndex, new OffsetsSection.OffsetEntry() { ArchiveIndex = 0, Offset = 0 });

			return assetIndex;
		}

		public class AssetUpdater: AssetUpdaterBase {
			public AssetUpdater(int index) : base(index) { }

			public override void Apply(TOCBase toc) {
				if (_updateAssetId)
					toc.AssetIdsSection.Ids[_index] = _assetId;

				TOC_I20? toc_i20 = toc as TOC_I20;
				if (toc_i20 != null) {
					if (_updateArchiveIndex)
						toc_i20.OffsetsSection.Entries[_index].ArchiveIndex = _archiveIndex;

					if (_updateArchiveOffset)
						toc_i20.OffsetsSection.Entries[_index].Offset = _archiveOffset;

					if (_updateSize)
						toc_i20.SizesSection.Entries[_index].Value = _size;
				}
			}
		}

		public override void SortAssets() {
			var ids = AssetIdsSection.Ids;
			var sizes = SizesSection.Entries;
			var offsets = OffsetsSection.Entries;

			foreach (var span in SpansSection.Entries) {
				var start = span.AssetIndex;
				var end = span.AssetIndex + span.Count;

				var assets = new List<(ulong Id, SizeEntriesSection_I16.SizeEntry Size, OffsetsSection.OffsetEntry Offset)>();
				for (int i = (int)start; i < end; ++i) {
					assets.Add((ids[i], sizes[i], offsets[i]));
				}

				var compare = (ulong a, ulong b) => {
					if (a == b) return 0;
					return (a < b ? -1 : 1);
				};
				assets.Sort((x, y) => compare(x.Id, y.Id));

				for (int i = (int)start; i < end; ++i) {
					int j = (int)(i - start);
					ids[i] = assets[j].Id;
					sizes[i] = assets[j].Size;
					offsets[i] = assets[j].Offset;
				}
			}

			for (var i = 0; i < ids.Count; ++i) {
				sizes[i].Index = (uint)i;
			}
		}

		#endregion

		#endregion
		#region archives

		public override uint GetArchivesCount() => (uint)ArchivesSection.Entries.Count;
		public override string GetArchiveFilename(uint index) => ArchivesSection.Entries[(int)index].GetFilename();

		#region modify archives

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

			// fix all assets entries that referenced archives prior to the one added
			var offsets = OffsetsSection.Entries;
			foreach (var offset in offsets) {
				if (offset.ArchiveIndex >= index) {
					++offset.ArchiveIndex;
				}
			}

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

		#endregion

		#endregion
	}

	public class TOC_I29: TOCBase {
		private const uint MAGIC = 0x34E89035;

		public AssetHeadersSection AssetHeadersSection => Dat1.Section<AssetHeadersSection>(AssetHeadersSection.TAG);
		public SizeEntriesSection_I29 SizesSection => Dat1.Section<SizeEntriesSection_I29>(SizeEntriesSection_I29.TAG);
		public ArchivesMapSection_I29 ArchivesSection => Dat1.Section<ArchivesMapSection_I29>(ArchivesMapSection_I29.TAG);

		/*
		36A6C8CC = "Archive TOC Texture Asset Ids"
		62297090 = "Archive TOC Texture Header"
		C9FB9DDA = "Archive TOC Texture Meta"
		*/

		public override bool Load(string filename) {
			try {
				using var f = File.OpenRead(filename);
				using var r = new BinaryReader(f);

				uint magic = r.ReadUInt32();
				if (magic != MAGIC) {
					return false;
				}

				uint uncompressedLength = r.ReadUInt32();
				int length = (int)(f.Length - 8);
				byte[] bytes = r.ReadBytes(length);

				Dat1 = new DAT1(new BinaryReader(new MemoryStream(bytes)));
				AssetArchivePath = Path.GetDirectoryName(filename);

				return true;
			} catch (Exception) {
				return false;
			}
		}

		public override bool Save(string filename) {
			if (!IsLoaded) return false;

			byte[] bytes = Dat1.Save();
			using var f = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
			using var w = new BinaryWriter(f);
			w.Write(MAGIC);
			w.Write((uint)bytes.Length);
			w.Write(bytes);

			return true;
		}

		#region assets

		#region get asset info by index

		public override uint? GetArchiveIndexByAssetIndex(int index) => (0 <= index && index < SizesSection.Entries.Count ? SizesSection.Entries[index].ArchiveIndex : null);
		public override uint? GetOffsetInArchiveByAssetIndex(int index) => (0 <= index && index < SizesSection.Entries.Count ? SizesSection.Entries[index].Offset : null);
		public override uint? GetSizeInArchiveByAssetIndex(int index) => (0 <= index && index < SizesSection.Entries.Count ? SizesSection.Entries[index].Size : null);

		public virtual int? GetHeaderOffsetByAssetIndex(int index) => (0 <= index && index < SizesSection.Entries.Count ? SizesSection.Entries[index].HeaderOffset : null);

		public virtual byte[] GetHeaderByAssetIndex(int index) {
			var header_offset = GetHeaderOffsetByAssetIndex(index);
			if (header_offset == null) return null;
			if (header_offset == -1) return null;
			return AssetHeadersSection.Headers[(int)header_offset / 36];
		}

		#endregion
		#region extract asset

		public override byte[] ExtractAsset(int index) {
			byte[] body = base.ExtractAsset(index);
			if (body == null) return null;

			var header = GetHeaderByAssetIndex(index);
			if (header == null) {
				return body;
			}

			long real_size = body.Length + header.Length;
			byte[] bytes = new byte[real_size];
			header.CopyTo(bytes, 0);
			body.CopyTo(bytes, header.Length);
			return bytes;
		}

		#endregion
		#region modify assets

		public override int AddAsset(byte span, ulong assetId) {
			var spanEntry = SpansSection.Entries[span];

			// insert into right place (SortAssets() shouldn't be required)
			var assetIndex = AssetIdsSection.Ids.BinarySearch((int)spanEntry.AssetIndex, (int)spanEntry.Count, assetId, null); // default comparer
			Utils.Assert(assetIndex < 0, "AddAsset() should not be called if asset is already present");
			assetIndex = ~assetIndex;

			++spanEntry.Count;
			for (int i = span + 1; i < SpansSection.Entries.Count; ++i) {
				++SpansSection.Entries[i].AssetIndex;
			}

			AssetIdsSection.Ids.Insert(assetIndex, assetId);
			SizesSection.Entries.Insert(assetIndex, new SizeEntriesSection_I29.SizeEntry() {
				ArchiveIndex = 0,
				HeaderOffset = -1,
				Offset = 0,
				Size = 0
			});

			return assetIndex;
		}

		public class AssetUpdater: AssetUpdaterBase {
			protected bool _updateHeader;
			protected byte[]? _header;

			public AssetUpdater(int index) : base(index) { }

			public AssetUpdater UpdateHeader(byte[]? header) {
				_updateHeader = true;
				_header = header;
				return this;
			}

			public override void Apply(TOCBase toc) {
				if (_updateAssetId)
					toc.AssetIdsSection.Ids[_index] = _assetId;

				TOC_I29? toc_i29 = toc as TOC_I29;
				if (toc_i29 != null) {
					if (_updateArchiveIndex)
						toc_i29.SizesSection.Entries[_index].ArchiveIndex = _archiveIndex;

					if (_updateArchiveOffset)
						toc_i29.SizesSection.Entries[_index].Offset = _archiveOffset;

					if (_updateSize)
						toc_i29.SizesSection.Entries[_index].Size = _size;

					if (_updateHeader) {
						if (_header == null) {
							toc_i29.SizesSection.Entries[_index].HeaderOffset = -1;
						} else {
							var header_offset = toc_i29.SizesSection.Entries[_index].HeaderOffset;
							if (header_offset == -1) {
								var header_index = toc_i29.AssetHeadersSection.Headers.Count;
								toc_i29.AssetHeadersSection.Headers.Add(_header);
								toc_i29.SizesSection.Entries[_index].HeaderOffset = header_index * 36;
							} else {
								var header_index = header_offset / 36;
								toc_i29.AssetHeadersSection.Headers[header_index] = _header;
							}
						}
					}
				}
			}
		}

		public override void SortAssets() {
			var ids = AssetIdsSection.Ids;
			var sizes = SizesSection.Entries;

			foreach (var span in SpansSection.Entries) {
				var start = span.AssetIndex;
				var end = span.AssetIndex + span.Count;

				var assets = new List<(ulong Id, SizeEntriesSection_I29.SizeEntry Size)>();
				for (int i = (int)start; i < end; ++i) {
					assets.Add((ids[i], sizes[i]));
				}

				var compare = (ulong a, ulong b) => {
					if (a == b) return 0;
					return (a < b ? -1 : 1);
				};
				assets.Sort((x, y) => compare(x.Id, y.Id));

				for (int i = (int)start; i < end; ++i) {
					int j = (int)(i - start);
					ids[i] = assets[j].Id;
					sizes[i] = assets[j].Size;
				}
			}
		}

		#endregion

		#endregion
		#region archives

		public override uint GetArchivesCount() => (uint)ArchivesSection.Entries.Count;
		public override string GetArchiveFilename(uint index) => ArchivesSection.Entries[(int)index].GetFilename();

		#region modify archives

		public override uint AddNewArchive(string filename, ArchiveAddingImpl ignored = ArchiveAddingImpl.DEFAULT) {
			int index = ArchivesSection.Entries.Count;

			byte[] bytes = new byte[40];
			for (int i = 0; i < 40; ++i) bytes[i] = 0;
			byte[] fn = Encoding.ASCII.GetBytes(filename);
			fn.CopyTo(bytes, 0);
			bytes[fn.Length] = 0;

			ArchivesSection.Entries.Add(new ArchivesMapSection_I29.ArchiveFileEntry() {
				Filename = bytes,
				A = 2678794514496,
				B = 2678794514496,
				C = 3844228203,
				D = 32763,
				E = 0
			});

			return (uint)index;
		}

		#endregion

		#endregion
	}
}
