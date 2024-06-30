// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using SharpCompress.Archives;
using System;
using System.IO;
using System.IO.Compression;

namespace Overstrike.Utils {
	internal static class NestedFiles {
		public static string GetAbsoluteModPath(string pathRelativeToLibrary) {
			var cwd = Directory.GetCurrentDirectory();
			return Path.Combine(cwd, "Mods Library", pathRelativeToLibrary);
		}

		// support path with || for nested files

		public static ZipArchive GetNestedZip(string path) {
			var fullPath = GetAbsoluteModPath(path);
			return new ZipArchive(GetNestedFile(fullPath));
		}

		public static Stream GetNestedFile(string path) {
			var index = path.IndexOf("||");
			if (index != -1) {
				string basefile = path.Substring(0, index);
				string rest = path.Substring(index + 2);

				using (var archive = ArchiveFactory.Open(File.OpenRead(basefile))) {
					return GetNestedFile(archive, rest);
				}
			}

			return File.OpenRead(path);
		}

		public static Stream GetNestedFile(IArchive archive, string path) {
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
							return GetNestedFile(archive2, path.Substring(index + 2));
						}
					}
				}
			}

			return null;
		}

		// zip helpers

		public static ZipArchiveEntry GetZipEntryByFullName(ZipArchive zip, string name) {
			foreach (ZipArchiveEntry entry in zip.Entries) {
				if (entry.FullName.Equals(name, StringComparison.OrdinalIgnoreCase))
					return entry;
			}

			return null;
		}

		public static byte[] GetZippedFileBytes(ZipArchive zip, string filename) {
			var zipEntry = GetZipEntryByFullName(zip, filename);
			using var stream = zipEntry.Open();
			var file = new MemoryStream();
			stream.CopyTo(file);
			file.Seek(0, SeekOrigin.Begin);
			return file.ToArray();
		}
	}
}
