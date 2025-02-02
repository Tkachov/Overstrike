// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using SharpCompress.Archives;
using System.IO;
using System;
using System.IO.Compression;
using DAT1;
using Overstrike.Data;
using Overstrike.Utils;

namespace Overstrike.Installers {
	internal abstract class InstallerBase {
		protected ModEntry _mod;
		protected string _gamePath;

		public InstallerBase(string gamePath) {
			_mod = null;
			_gamePath = gamePath;
		}

		public abstract void Install(ModEntry mod, int index);

		#region .zip

		protected ZipArchive ReadModFile() {
			return NestedFiles.GetNestedZip(_mod.Path);
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
	}

	internal abstract class InstallerBase_I20: InstallerBase {
		protected TOC_I20 _toc;

		public InstallerBase_I20(TOC_I20 toc, string gamePath) : base(gamePath) {
			_toc = toc;
		}

		#region toc

		protected void AddOrUpdateAssetEntry(byte span, ulong assetId, uint archiveIndex, uint offset, uint size) {
			int assetIndex = _toc.FindOrAddAsset(span, assetId);
			new TOC_I20.AssetUpdater(assetIndex)
				.UpdateArchiveIndex(archiveIndex)
				.UpdateArchiveOffset(offset)
				.UpdateSize(size)
				.Apply(_toc);
		}

		protected void OverwriteAsset(byte span, ulong assetId, uint archiveIndex, BinaryWriter archiveWriter, Stream data) {
			long archiveOffset = archiveWriter.BaseStream.Position;
			data.CopyTo(archiveWriter.BaseStream);
			long fileSize = archiveWriter.BaseStream.Position - archiveOffset;

			AddOrUpdateAssetEntry(span, assetId, archiveIndex, (uint)archiveOffset, (uint)fileSize);
		}

		#endregion
	}

	internal abstract class InstallerBase_I29: InstallerBase {
		protected TOC_I29 _toc;

		public InstallerBase_I29(TOC_I29 toc, string gamePath) : base(gamePath) {
			_toc = toc;
		}

		#region toc

		protected void OverwriteAsset_v1(byte span, ulong assetId, uint archiveIndex, BinaryWriter archiveWriter, Stream data, bool withHeader) {
			byte[] header = new byte[36];
			if (withHeader) data.Read(header, 0, 36);

			long archiveOffset = archiveWriter.BaseStream.Position;
			data.CopyTo(archiveWriter.BaseStream);
			long fileSize = archiveWriter.BaseStream.Position - archiveOffset;

			int assetIndex = _toc.FindOrAddAsset(span, assetId);
			var updater = new TOC_I29.AssetUpdater(assetIndex);
			// if (withHeader) updater.UpdateHeader(header); // keep the original header, as V1 didn't extract the correct bytes
			updater
				.UpdateArchiveIndex(archiveIndex)
				.UpdateArchiveOffset((uint)archiveOffset)
				.UpdateSize((uint)fileSize)
				.Apply(_toc);
		}

		protected void OverwriteAsset(byte span, ulong assetId, uint archiveIndex, BinaryWriter archiveWriter, byte[] header, byte[] textureMeta, byte[] data) {
			long archiveOffset = archiveWriter.BaseStream.Position;
			archiveWriter.Write(data);
			long fileSize = archiveWriter.BaseStream.Position - archiveOffset;

			int assetIndex = _toc.FindOrAddAsset(span, assetId);
			var updater = new TOC_I29.AssetUpdater(assetIndex);
			if (header != null) updater.UpdateHeader(header);
			if (textureMeta != null) updater.UpdateTextureMeta(textureMeta);
			updater
				.UpdateArchiveIndex(archiveIndex)
				.UpdateArchiveOffset((uint)archiveOffset)
				.UpdateSize((uint)fileSize)
				.Apply(_toc);
		}

		#endregion
	}
}
