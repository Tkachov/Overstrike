// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO.Compression;
using System.IO;
using System;
using System.Collections.Generic;
using Overstrike.Data;
using System.Text;

namespace Overstrike.Detectors {
	internal class SuitStyleModDetector: DetectorBase {
		public SuitStyleModDetector(): base() {}

		public override string[] GetExtensions() {
			return new string[] {"suit_style"};
		}

		public override void Detect(Stream file, string path, List<ModEntry> mods, List<string> warnings) {
			try {
				ModEntry.ModType detectedModType = ModEntry.ModType.UNKNOWN;
				string name = null;
				string author = null;

				var prefix = new byte[16];
				var header = new byte[16];

				file.Seek(0, SeekOrigin.Begin);
				file.Read(prefix, 0, 16);

				file.Seek(-16, SeekOrigin.End);
				file.Read(header, 0, 16);

				for (int i = 0; i < 16; ++i) {
					header[i] ^= prefix[i];
				}

				using (var r = new BinaryReader(new MemoryStream(header))) {
					var magic = r.ReadUInt32();
					var size = r.ReadUInt32();
					var version = r.ReadByte();
					var game = r.ReadByte();

					if (magic == 0x4C595453) {
						if (version == 1) {
							switch (game) {
								case 1: detectedModType = ModEntry.ModType.SUIT_STYLE_MSM2; break;
								default: break;
							}

							string ReadString(Stream file) {
								var len = file.ReadByte();
								var buf = new byte[len];
								file.Read(buf);
								return Encoding.UTF8.GetString(buf);
							}

							file.Seek(-16 - size, SeekOrigin.End);
							ReadString(file);
							ReadString(file);
							name = ReadString(file);
							author = ReadString(file);
						}
					}
				}

				if (detectedModType != ModEntry.ModType.UNKNOWN) {
					var shortPath = GetShortPath(path);
					var modName = Path.GetFileName(shortPath);
					if (name != null && name.Trim() != "") {
						modName = name;
						if (author != null && author.Trim() != "") {
							modName += " by " + author;
						}
					}

					mods.Add(new ModEntry(modName, path, detectedModType));
				}
			} catch {}
		}
	}
}
