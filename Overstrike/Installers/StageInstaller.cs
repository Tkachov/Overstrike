// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using System.IO.Compression;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Overstrike.Data;

namespace Overstrike.Installers {
	internal abstract class StageInstallerHelper {
		protected abstract uint CreateArchive(string filename);
		protected virtual void PrepWork(ZipArchive zip) {}
		protected abstract ZipArchive ReadStageFile();
		protected abstract void ProcessAsset(byte span, ulong assetId, ZipArchiveEntry entry, uint archiveIndexToWriteInto, BinaryWriter archiveWriter);
		protected abstract void SortAssets();

		public void Work(string modPath, string relativeModPath) {
			var newArchiveIndex = CreateArchive(relativeModPath);

			using (var f = new FileStream(modPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
				using (var w = new BinaryWriter(f)) {
					using (ZipArchive zip = ReadStageFile()) {
						PrepWork(zip);
						foreach (ZipArchiveEntry entry in zip.Entries) {
							HandleArchiveEntry(entry, newArchiveIndex, w);
						}
					}
				}
			}

			SortAssets();
		}

		protected virtual void HandleArchiveEntry(ZipArchiveEntry entry, uint archiveIndexToWriteInto, BinaryWriter archiveWriter) {
			if (entry.Name == "" && entry.FullName.EndsWith("/")) return; // directory

			byte span;
			ulong assetId;
			if (IsAssetFile(entry.FullName, out span, out assetId)) {
				ProcessAsset(span, assetId, entry, archiveIndexToWriteInto, archiveWriter);
			}
		}

		protected static bool IsAssetFile(string path, out byte span, out ulong assetId) {
			span = 0;
			assetId = 0;

			if (path == null) return false;

			var index = -1;
			for (var i = 0; i < path.Length; ++i) {
				if (path[i] == Path.DirectorySeparatorChar || path[i] == Path.AltDirectorySeparatorChar) {
					index = i;
					break;
				}
			}
			if (index < 0) return false;

			var spanDir = path.Substring(0, index);
			int spanNum;
			var isNumeric = int.TryParse(spanDir, out spanNum);
			if (isNumeric && spanNum >= 0 && spanNum <= 255) {
				span = (byte)spanNum;
			} else {
				return false;
			}

			var filePath = path.Substring(index + 1);
			if (filePath == "") return false;
			if (Regex.IsMatch(filePath, "^[0-9A-Fa-f]{16}$")) {
				assetId = ulong.Parse(filePath, NumberStyles.HexNumber);
			} else {
				assetId = CRC64.Hash(filePath);
			}

			return true;
		}
	}

	internal class StageInstaller_I20: InstallerBase_I20 {
		public StageInstaller_I20(TOC_I20 toc, string gamePath): base(toc, gamePath) {}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			var modsPath = Path.Combine(_gamePath, "asset_archive", "mods");
			var modPath = Path.Combine(modsPath, "mod" + index);
			var relativeModPath = "mods\\mod" + index;

			new Helper(this).Work(modPath, relativeModPath);
		}

		class Helper: StageInstallerHelper {
			private StageInstaller_I20 _outer;
			public Helper(StageInstaller_I20 outer) {
				_outer = outer;
			}

			protected override uint CreateArchive(string filename) => _outer._toc.AddNewArchive(filename, TOCBase.ArchiveAddingImpl.SUITTOOL);

			protected override ZipArchive ReadStageFile() => _outer.ReadModFile();

			protected override void ProcessAsset(byte span, ulong assetId, ZipArchiveEntry entry, uint archiveIndexToWriteInto, BinaryWriter archiveWriter) {
				_outer.OverwriteAsset(span, assetId, archiveIndexToWriteInto, archiveWriter, entry.Open());
			}

			protected override void SortAssets() => _outer._toc.SortAssets();
		}
	}

	internal class StageInstaller_I29: InstallerBase_I29 {
		private HashSet<string> _headerlessAssets = new();

		public StageInstaller_I29(TOC_I29 toc, string gamePath): base(toc, gamePath) {}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			var modsPath = Path.Combine(_gamePath, "d", "mods");
			var modPath = Path.Combine(modsPath, "mod" + index);
			var relativeModPath = "d\\mods\\mod" + index;

			new Helper(this).Work(modPath, relativeModPath);
		}

		class Helper: StageInstallerHelper {
			private StageInstaller_I29 _outer;
			public Helper(StageInstaller_I29 outer) {
				_outer = outer;
			}

			protected override uint CreateArchive(string filename) => _outer._toc.AddNewArchive(filename, TOCBase.ArchiveAddingImpl.DEFAULT);

			protected override void PrepWork(ZipArchive zip) {
				_outer._headerlessAssets.Clear();

				var entry = _outer.GetEntryByFullName(zip, "info.json");
				JObject info = null;
				
				if (entry != null) {
					using (var stream = entry.Open()) {
						using (StreamReader reader = new StreamReader(stream)) {
							var str = reader.ReadToEnd();
							info = JObject.Parse(str);
						}
					}
				}

				if (info != null) {
					JArray headerless = (JArray)info["headerless"];
					if (headerless != null) {
						foreach (string assetLocation in headerless) {
							var slashIndex = assetLocation.IndexOf('/');
							if (slashIndex == -1) continue;

							var path = assetLocation.Substring(slashIndex + 1);
							if (Regex.IsMatch(path, "^[0-9A-Fa-f]{16}$")) {
								_outer._headerlessAssets.Add(assetLocation);
							} else {
								var assetId = CRC64.Hash(path);
								_outer._headerlessAssets.Add(assetLocation.Substring(0, slashIndex + 1) + $"{assetId:X016}");
							}
						}
					}
				}
			}

			protected override ZipArchive ReadStageFile() => _outer.ReadModFile();

			protected override void ProcessAsset(byte span, ulong assetId, ZipArchiveEntry entry, uint archiveIndexToWriteInto, BinaryWriter archiveWriter) {
				var hasHeader = (span % 8) == 0;
				var location = $"{span}/{assetId:X016}";
				if (_outer._headerlessAssets.Contains(location)) {
					hasHeader = false;
				}

				_outer.OverwriteAsset_v1(span, assetId, archiveIndexToWriteInto, archiveWriter, entry.Open(), hasHeader);
			}

			protected override void SortAssets() => _outer._toc.SortAssets();
		}
	}
	
	internal class StageInstaller_I29_V2: InstallerBase_I29 {
		public StageInstaller_I29_V2(TOC_I29 toc, string gamePath) : base(toc, gamePath) {}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			var modsPath = Path.Combine(_gamePath, "d", "mods");
			var modPath = Path.Combine(modsPath, "mod" + index);
			var relativeModPath = "d\\mods\\mod" + index;

			new Helper(this).Work(modPath, relativeModPath);
		}

		class Helper: StageInstallerHelper {
			private StageInstaller_I29_V2 _outer;
			public Helper(StageInstaller_I29_V2 outer) {
				_outer = outer;
			}

			protected override uint CreateArchive(string filename) => _outer._toc.AddNewArchive(filename, TOCBase.ArchiveAddingImpl.DEFAULT);

			protected override void PrepWork(ZipArchive zip) {}

			protected override ZipArchive ReadStageFile() => _outer.ReadModFile();

			protected override void ProcessAsset(byte span, ulong assetId, ZipArchiveEntry entry, uint archiveIndexToWriteInto, BinaryWriter archiveWriter) {
				using var data = entry.Open();
				using var ms = new MemoryStream();
				data.CopyTo(ms);

				byte[] bytes = ms.ToArray();

				using var br = new BinaryReader(new MemoryStream(bytes));
				var magic = br.ReadUInt32();

				// TODO: a class in shared for that?
				if (magic == 0x00475453) { // STG\x00
					var flags = br.ReadUInt32();
					var headerSize = br.ReadUInt32();
					var textureMetaSize = br.ReadUInt32();
					
					var headerData = br.ReadBytes((int)headerSize);
					Align16(br);

					var textureMetaData = br.ReadBytes((int)textureMetaSize);
					Align16(br);

					byte[] actualBytes = br.ReadBytes((int)(bytes.Length - br.BaseStream.Position));

					byte[] header = null;
					if ((flags & 0x1) != 0) { // TODO: constant
						header = headerData;
					}

					byte[] textureMeta = null;
					if ((flags & 0x2) != 0) { // TODO: constant
						textureMeta = textureMetaData;
					}

					_outer.OverwriteAsset(span, assetId, archiveIndexToWriteInto, archiveWriter, header, textureMeta, actualBytes);
					return;
				}

				_outer.OverwriteAsset(span, assetId, archiveIndexToWriteInto, archiveWriter, null, null, bytes);

				static void Align16(BinaryReader br) {
					var pos = br.BaseStream.Position % 16;
					if (pos != 0) {
						var rem = 16 - pos;
						br.ReadBytes((int)rem);
					}
				}
			}

			protected override void SortAssets() => _outer._toc.SortAssets();
		}
	}
}
