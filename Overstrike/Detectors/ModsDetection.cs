using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Overstrike.Detectors {
	internal class ModsDetection {
		private List<DetectorBase> _detectors;

		public ModsDetection() {
			_detectors = new List<DetectorBase>() {
				new SMPCModDetector(),
				new SuitModDetector(),
				new ZipDetector(this)
			};

			// TODO: support 7z/rar
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

		internal void Detect(ZipArchive zip, string path, List<ModEntry> mods) {
			Dictionary<string, DetectorBase> detectors = new Dictionary<string, DetectorBase>();
			foreach (var detector in _detectors) {
				string[] extensions = detector.GetExtensions();
				foreach (var extension in extensions) {
					detectors[extension] = detector;
				}
			}

			foreach (ZipArchiveEntry entry in zip.Entries) {
				foreach (var extension in detectors.Keys) {
					if (entry.Name.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase)) {
						var file = entry.Open();
						var internalPath = path + "||" + entry.FullName;
						detectors[extension].Detect(file, internalPath, mods);
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
