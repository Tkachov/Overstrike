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
	internal class SuitModDetector: DetectorBase {
		public SuitModDetector() : base() {}

		public override string[] GetExtensions() {
			return new string[] {"suit"};
		}

		public override void Detect(Stream file, string path, List<ModEntry> mods, List<string> warnings) {
			// legacy suit
			try {
				ModEntry.ModType detectedModType = ModEntry.ModType.UNKNOWN;

				using (ZipArchive zip = new ZipArchive(file)) {
					// find important files

					ZipArchiveEntry idTxt = null;
					ZipArchiveEntry infoTxt = null;

					foreach (ZipArchiveEntry entry in zip.Entries) {
						if (entry.Name.Equals("id.txt", StringComparison.OrdinalIgnoreCase)) {
							idTxt = entry;
						} else if (entry.Name.Equals("info.txt", StringComparison.OrdinalIgnoreCase)) {
							infoTxt = entry;
						}
					}

					if (idTxt == null || infoTxt == null) {
						return;
					}

					// read id.txt

					string id = null;
					using (var stream = idTxt.Open()) {
						using (StreamReader reader = new StreamReader(stream)) {
							var str = reader.ReadToEnd();
							var lines = str.Split("\n");
							if (lines.Length > 0) {
								id = lines[0].Trim();
							}
						}
					}

					if (id == null) {
						return;
					}

					// check assets file (<id>) exists

					bool hasAssets = false;
					foreach (ZipArchiveEntry entry in zip.Entries) {
						if (entry.FullName.Equals(id + "/" + id, StringComparison.OrdinalIgnoreCase)) {
							hasAssets = true;
							break;
						}
					}

					if (!hasAssets) {
						return;
					}

					// read info.txt

					using (var stream = infoTxt.Open()) {
						var firstByte = stream.ReadByte();

						if (infoTxt.Length % 21 == 1) {
							// version 0
							detectedModType = ModEntry.ModType.SUIT_MSMR;
						} else if (infoTxt.Length % 21 == 2) {
							if (firstByte == 0) {
								// version 1 / MSMR
								detectedModType = ModEntry.ModType.SUIT_MSMR;
							} else if (firstByte == 1) {
								// version 1 / MM
								detectedModType = ModEntry.ModType.SUIT_MM;
							}
						}
						
						if (infoTxt.Length % 17 == 2) {
							if (firstByte == 2) {
								// version 2 / MM
								detectedModType = ModEntry.ModType.SUIT_MM_V2;
							}
						}
					}
				}

				if (detectedModType != ModEntry.ModType.UNKNOWN) {
					var shortPath = GetShortPath(path);
					var name = Path.GetFileName(shortPath);
					mods.Add(new ModEntry(name, path, detectedModType));
				}
			} catch {}

			// suit 2
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

					if (magic == 0x54495553) {
						if (version == 1) {
							switch (game) {
								case 1: detectedModType = ModEntry.ModType.SUIT2_MSM2; break;
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
