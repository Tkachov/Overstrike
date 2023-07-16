using System.IO;
using System;
using System.Collections.Generic;
using SharpCompress.Archives;

namespace Overstrike.Detectors {
	internal class ArchiveDetector: DetectorBase {
		private ModsDetection _detection;

		public ArchiveDetector(ModsDetection detection) : base() {
			_detection = detection;
		}

		public override string[] GetExtensions() {
			return new string[] {"7z", "rar", "zip"};
		}

		public override void Detect(Stream file, string path, List<ModEntry> mods) {
			try {
				using (var archive = ArchiveFactory.Open(file)) {
					_detection.Detect(archive, path, mods);
				}
			} catch (Exception) { }
		}
	}
}
