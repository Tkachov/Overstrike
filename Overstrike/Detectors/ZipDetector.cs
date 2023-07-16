using System.IO.Compression;
using System.IO;
using System;
using System.Collections.Generic;

namespace Overstrike.Detectors {
	internal class ZipDetector: DetectorBase {
		private ModsDetection _detection;

		public ZipDetector(ModsDetection detection) : base() {
			_detection = detection;
		}

		public override string[] GetExtensions() {
			return new string[] {"zip"};
		}

		public override void Detect(Stream file, string path, List<ModEntry> mods) {
			try {
				using (ZipArchive zip = new ZipArchive(file)) {
					_detection.Detect(zip, path, mods);
				}
			} catch (Exception) { }
		}
	}
}
