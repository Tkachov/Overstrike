using DAT1;
using DAT1.Sections.TOC;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Overstrike.Installers {
	internal abstract class InstallerBase {
		protected TOC _toc;
		protected ModEntry _mod;
		protected string _gamePath;

		public InstallerBase(TOC toc, string gamePath) {
			_toc = toc;
			_mod = null;
			_gamePath = gamePath;
		}

		public abstract void Install(ModEntry mod, int index);

		#region .zip

		protected ZipArchive ReadModFile() {
			var cwd = Directory.GetCurrentDirectory();
			var zipPath = Path.Combine(cwd, "Mods Library", _mod.Path);
			return new ZipArchive(GetFile(zipPath));
		}

		private Stream GetFile(string path) {
			var index = path.IndexOf("||");
			if (index != -1) {
				string basefile = path.Substring(0, index);
				string rest = path.Substring(index + 2);
				using (ZipArchive zip = ZipFile.Open(basefile, ZipArchiveMode.Read)) {
					return GetFile(zip, rest);
				}
			}

			return File.OpenRead(path);
		}

		private Stream GetFile(ZipArchive zip, string path) {
			var fullpath = path;
			var index = path.IndexOf("||");
			if (index != -1) {
				fullpath = path.Substring(0, index);
			}

			foreach (ZipArchiveEntry entry in zip.Entries) {
				if (entry.FullName.Equals(fullpath, StringComparison.OrdinalIgnoreCase)) {
					if (index == -1) {
						var s = entry.Open();
						var ms = new MemoryStream();
						s.CopyTo(ms);
						ms.Seek(0, SeekOrigin.Begin);
						return ms;
					} else {
						using (ZipArchive zip2 = new ZipArchive(entry.Open())) {
							return GetFile(zip2, path.Substring(index + 2));
						}
					}
				}
			}

			return null;
		}

		protected ZipArchiveEntry GetEntryByName(ZipArchive zip, string name) {
			foreach (ZipArchiveEntry entry in zip.Entries) {
				if (entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
					return entry;
			}

			return null;
		}

		protected ZipArchiveEntry GetEntryByFullName(ZipArchive zip, string name) {
			foreach (ZipArchiveEntry entry in zip.Entries) {
				if (entry.FullName.Equals(name, StringComparison.OrdinalIgnoreCase))
					return entry;
			}

			return null;
		}

		#endregion
		#region toc

		protected byte[] GetAssetBytes(string path) => _toc.ExtractAsset(_toc.FindAssetEntryByPath(path));
		protected byte[] GetAssetBytes(ulong assetId) => _toc.ExtractAsset(_toc.FindAssetEntryById(assetId));
		protected BinaryReader GetAssetReader(string path) => new BinaryReader(new MemoryStream(GetAssetBytes(path)));
		protected BinaryReader GetAssetReader(ulong assetId) => new BinaryReader(new MemoryStream(GetAssetBytes(assetId)));

		protected uint GetArchiveIndex(string filename, TOC.ArchiveAddingImpl mode) {
			uint index = 0;
			foreach (var entry in _toc.Dat1.ArchivesSection.Entries) {
				if (entry.GetFilename() == filename) {
					return index;
				}

				++index;
			}

			return _toc.AddNewArchive(filename, mode);
		}

		protected void SortAssets() {
			var ids = _toc.Dat1.AssetIdsSection.Ids;
			var sizes = _toc.Dat1.SizesSection.Entries;
			var offsets = _toc.Dat1.OffsetsSection.Entries;

			foreach (var span in _toc.Dat1.SpansSection.Entries) {
				var start = span.AssetIndex;
				var end = span.AssetIndex + span.Count;

				var assets = new List<(ulong Id, SizeEntriesSection.SizeEntry Size, OffsetsSection.OffsetEntry Offset)>();
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
	}
}
