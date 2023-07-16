using System.Collections.Generic;
using System.IO;

namespace Overstrike.Detectors {
	internal abstract class DetectorBase {
		public DetectorBase() {}

		public abstract string[] GetExtensions();
		public abstract void Detect(Stream file, string path, List<ModEntry> mods);

		protected string GetShortPath(string path) {
			var index = path.LastIndexOf("||");
			if (index != -1) {
				return path.Substring(index + 2);
			}

			return path;
		}
	}
}
