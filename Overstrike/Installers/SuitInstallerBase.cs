// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Files;
using System.IO;
using System.IO.Compression;

namespace Overstrike.Installers {
	internal abstract class SuitInstallerBase: MSMRInstallerBase {
		protected SuitInstallerBase(TOC toc, string gamePath) : base(toc, gamePath) {}

		#region .suit

		protected string ReadId(ZipArchiveEntry idTxt) {
			using (var stream = idTxt.Open()) {
				using (StreamReader reader = new StreamReader(stream)) {
					var str = reader.ReadToEnd();
					var lines = str.Split("\n");
					if (lines.Length > 0) {
						return lines[0].Trim();
					}
				}
			}

			return null;
		}

		#endregion
		#region toc

		protected uint GetArchiveIndex(string filename) => GetArchiveIndex(filename, TOC.ArchiveAddingImpl.SUITTOOL);

		protected void WriteArchive(string archivePath, uint archiveIndex, ulong assetId, byte span, byte[] bytes) {
			File.WriteAllBytes(archivePath, bytes);
			AddOrUpdateAssetEntry(assetId, span, archiveIndex, /*offset=*/0, (uint)bytes.Length);
		}

		protected void WriteArchive(string suitsPath, string archiveName, ulong assetId, byte[] bytes) {
			WriteArchive(suitsPath, archiveName, assetId, 0, bytes);
		}

		protected void WriteArchive(string suitsPath, string archiveName, ulong assetId, byte span, byte[] bytes) {
			var archivePath = Path.Combine(suitsPath, archiveName);
			var archiveIndex = GetArchiveIndex("Suits\\" + archiveName);
			WriteArchive(archivePath, archiveIndex, assetId, span, bytes);
		}

		#endregion
		#region .config

		protected void AddConfigReference(Config config, string path) {
			ulong aid = CRC64.Hash(path);
			foreach (var entry in config.ReferencesSection.Entries) {
				if (entry.AssetId == aid) {
					return;
				}
			}

			uint ext = 0;
			if (path.EndsWith(".config")) ext = 0xA9F149C4;
			else if (path.EndsWith(".texture")) ext = 0x95A3A227; // TODO: just calculate crc32 of extension lol

			config.ReferencesSection.Entries.Add(new DAT1.Sections.Generic.ReferencesSection.ReferenceEntry() {
				AssetId = aid,
				AssetPathStringOffset = config.AddString(path),
				ExtensionHash = ext
			});
		}

		#endregion
	}
}
