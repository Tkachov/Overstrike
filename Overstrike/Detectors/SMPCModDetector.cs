using System.IO.Compression;
using System.IO;
using System;
using System.Collections.Generic;

namespace Overstrike.Detectors {
	internal class SMPCModDetector: DetectorBase {
		public SMPCModDetector() : base() {}

		public override string[] GetExtensions() {
			return new string[] {"smpcmod", "mmpcmod"};
		}

		public override void Detect(Stream file, string path, List<ModEntry> mods) {
			try {
				bool hasFiles = false;
				bool hasInfo = false;

				string modName = null;
				string author = null;

				using (ZipArchive zip = new ZipArchive(file)) {
					foreach (ZipArchiveEntry entry in zip.Entries) {
						if (entry.FullName.Equals("SMPCMod.info", StringComparison.OrdinalIgnoreCase)) {
							using (var stream = entry.Open()) {
								using (StreamReader reader = new StreamReader(stream)) {
									var str = reader.ReadToEnd();
									var lines = str.Split("\n");
									foreach (var line in lines) {
										var sep = line.IndexOf("=");
										if (sep != -1) {
											var key = line.Substring(0, sep);
											var value = line.Substring(sep + 1);
											if (key.Equals("Title", StringComparison.OrdinalIgnoreCase)) {
												modName = value;
											} else if (key.Equals("Author", StringComparison.OrdinalIgnoreCase)) {
												author = value;
											}
										}
									}
								}
							}

							hasInfo = true;
						} else if (entry.FullName.StartsWith("ModFiles/", StringComparison.OrdinalIgnoreCase)) {
							hasFiles = true;
						}
					}
				}

				if (!hasFiles || !hasInfo) return;

				var shortPath = GetShortPath(path);
				var name = Path.GetFileName(shortPath);
				if (modName != null && modName.Trim() != "") {
					name = modName.Trim();
					if (author != null && author.Trim() != "") {
						name += " by " + author.Trim();
					}
				}

				mods.Add(new ModEntry(name, path, path.EndsWith(".smpcmod", StringComparison.OrdinalIgnoreCase) ? ModEntry.ModType.SMPC : ModEntry.ModType.MMPC));
			} catch (Exception) { }
		}
	}
}
