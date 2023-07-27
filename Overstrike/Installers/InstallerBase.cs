// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using SharpCompress.Archives;
using System.IO;
using System;
using System.IO.Compression;

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
			var cwd = Directory.GetCurrentDirectory();
			var fullPath = Path.Combine(cwd, "Mods Library", _mod.Path);
			return new ZipArchive(GetFile(fullPath));
		}

		private Stream GetFile(string path) {
			var index = path.IndexOf("||");
			if (index != -1) {
				string basefile = path.Substring(0, index);
				string rest = path.Substring(index + 2);

				using (var archive = ArchiveFactory.Open(File.OpenRead(basefile))) {
					return GetFile(archive, rest);
				}
			}

			return File.OpenRead(path);
		}

		private Stream GetFile(IArchive archive, string path) {
			var fullpath = path;
			var index = path.IndexOf("||");
			if (index != -1) {
				fullpath = path.Substring(0, index);
			}

			foreach (var entry in archive.Entries) {
				if (entry.IsDirectory) continue;
				if (entry.Key.Equals(fullpath, StringComparison.OrdinalIgnoreCase)) {
					var file = new MemoryStream();
					using (var entryStream = entry.OpenEntryStream()) {
						entryStream.CopyTo(file);
					}
					file.Seek(0, SeekOrigin.Begin);

					if (index == -1) {
						return file;
					} else {
						using (var archive2 = ArchiveFactory.Open(file)) {
							return GetFile(archive2, path.Substring(index + 2));
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
	}
}
