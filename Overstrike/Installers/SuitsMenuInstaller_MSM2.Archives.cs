// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Utils;
using OverstrikeShared.STG;
using OverstrikeShared.Utils;
using System.Collections.Generic;
using System.IO;

namespace Overstrike.Installers {
	internal partial class SuitsMenuInstaller_MSM2 {
		private sealed class SuitsMenuArchiveAsset {
			public byte Span { get; }
			public ulong Id { get; }
			public byte[] Data { get; }
			public byte[] Header { get; }

			public SuitsMenuArchiveAsset(byte span, ulong id, byte[] data, byte[] header) {
				Span = span;
				Id = id;
				Data = data;
				Header = header;
			}
		}

		private byte[] PrepareConfigHeader(ulong assetId, int byteCount, string description) {
			var assetIndex = _toc.FindFirstAssetIndexById(assetId);
			var originalHeader = (assetIndex < 0 ? null : _toc.GetHeaderByAssetIndex(assetIndex));
			if (originalHeader == null) {
				throw new InvalidDataException($"{description} has no asset header");
			}

			var header = new AssetHeaderHelper(originalHeader);
			if (header.Pairs.Count == 0) {
				throw new InvalidDataException($"{description} header has no size field");
			}

			header.Pairs[0].A = (uint)byteCount;
			return header.Save();
		}

		private void WriteSuitsMenuArchive(ulong systemProgressionAssetId, byte[] systemProgressionBytes, byte[] header, List<SuitsMenuArchiveAsset> extraAssets) {
			var archivePath = Path.Combine(_gamePath, "d", "mods", "suits_menu");
			var archiveIndex = GetArchiveIndex(Path.GetRelativePath(_gamePath, archivePath));

			using var stream = File.Create(archivePath);
			using var writer = new BinaryWriter(stream);
			OverwriteAsset(0, systemProgressionAssetId, archiveIndex, writer, header, null, systemProgressionBytes);
			foreach (var asset in extraAssets) {
				BinaryStreams.Align16(writer);
				OverwriteAsset(asset.Span, asset.Id, archiveIndex, writer, asset.Header, null, asset.Data);
			}
		}
	}
}
