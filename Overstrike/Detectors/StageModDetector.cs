using System.IO.Compression;
using System.IO;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Overstrike.Detectors {
	internal class StageModDetector: DetectorBase {
		public StageModDetector() : base() {}

		public override string[] GetExtensions() {
			return new string[] {"stage"};
		}

		public override void Detect(Stream file, string path, List<ModEntry> mods) {
			try {
				bool hasFiles = false;
				JObject info = null;

				using (ZipArchive zip = new ZipArchive(file)) {
					foreach (ZipArchiveEntry entry in zip.Entries) {
						if (entry.FullName.Equals("info.json", StringComparison.OrdinalIgnoreCase)) {
							using (var stream = entry.Open()) {
								using (StreamReader reader = new StreamReader(stream)) {
									var str = reader.ReadToEnd();
									info = JObject.Parse(str);
								}
							}
						} else {							
							var root = GetRootFolder(entry.FullName);
							if (root != null) {
								int span;
								var isNumeric = int.TryParse(root, out span);
								if (isNumeric && span >= 0 && span <= 255) {
									hasFiles = true;
								}
							}
						}
					}
				}

				if (!hasFiles || info == null) return;

				var shortPath = GetShortPath(path);
				var name = Path.GetFileName(shortPath);
				var type = ModEntry.ModType.UNKNOWN;
				if (info != null) {
					string n = (string)info["name"];
					string a = (string)info["author"];
					if (n != null && n.Trim() != "") {
						name = n;
						if (a != null && a.Trim() != "") {
							name += " by " + a;
						}
					}

					string g = (string)info["game"];
					if (g != null && g.Trim() != "") {
						if (g == Profile.GAME_MSMR) type = ModEntry.ModType.STAGE_MSMR;
						else if (g == Profile.GAME_MM) type = ModEntry.ModType.STAGE_MM;
					}
				}

				if (type != ModEntry.ModType.UNKNOWN) {
					mods.Add(new ModEntry(name, path, type));
				}
			} catch (Exception) { }
		}

		private string GetRootFolder(string path) {
			if (path == null) return null;

			string root = "";
			foreach (var c in path) {
				if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar) break;
				root += c;
			}
			return root;
		}
	}
}
