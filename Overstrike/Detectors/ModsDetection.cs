using SharpCompress.Archives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Overstrike.Detectors {
	internal class ModsDetection {
		private List<DetectorBase> _detectors;

		public ModsDetection() {
			_detectors = new List<DetectorBase>() {
				new SMPCModDetector(),
				new SuitModDetector(),
				new StageModDetector(),
				new ArchiveDetector(this)
			};
		}

		public void Detect(string path, List<ModEntry> mods) {
			mods.Clear();
			
			foreach (var detector in _detectors) {
				string[] extensions = detector.GetExtensions();
				foreach (var extension in extensions) {
					string[] files = Directory.GetFiles(path, "*." + extension, SearchOption.AllDirectories);
					foreach (var file in files) {
						detector.Detect(File.OpenRead(file), GetRelativePath(file, path), mods);
					}
				}
			}
		}

		internal void Detect(IArchive archive, string path, List<ModEntry> mods) {
			Dictionary<string, DetectorBase> detectors = new Dictionary<string, DetectorBase>();
			foreach (var detector in _detectors) {
				string[] extensions = detector.GetExtensions();
				foreach (var extension in extensions) {
					detectors[extension] = detector;
				}
			}

			foreach (var entry in archive.Entries) {
				if (entry.IsDirectory) continue;

				foreach (var extension in detectors.Keys) {
					if (entry.Key.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase)) {
						var file = new MemoryStream();
						using (var entryStream = entry.OpenEntryStream()) {
							entryStream.CopyTo(file);
						}
						file.Seek(0, SeekOrigin.Begin);

						var internalPath = path + "||" + entry.Key;
						detectors[extension].Detect(file, internalPath, mods);
						break;
					}
				}
			}
		}

		private string GetRelativePath(string file, string basepath) {
			Debug.Assert(file.StartsWith(basepath, StringComparison.OrdinalIgnoreCase));
			var result = file.Substring(basepath.Length);
			if (result.Length > 0) {
				if (result[0] == '/' || result[0] == '\\')
					result = result.Substring(1);
			}
			return result;
		}
	}
}
