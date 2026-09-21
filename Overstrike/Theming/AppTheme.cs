using System;
using System.Collections.Generic;
using System.IO;

namespace Overstrike.Theming {
	internal sealed class AppTheme {
		public const string DEFAULT_THEME_ID = "default";

		public required string Id { get; init; }
		public required string Name { get; init; }
		public required string Version { get; init; }
		public required Dictionary<string, string> Colors { get; init; }
		public bool IsBuiltIn { get; init; }
		public string? DirectoryPath { get; init; }

		public string DisplayName {
			get {
				if (IsBuiltIn || String.IsNullOrWhiteSpace(Version) || Version == ThemeManager.CurrentOverstrikeVersion) {
					return Name;
				}

				return $"{Name} (for {Version})";
			}
		}

		public string? GetResourceOverridePath(string fileName) {
			if (String.IsNullOrWhiteSpace(DirectoryPath)) return null;

			var path = Path.Combine(DirectoryPath, "Resources", fileName);
			return File.Exists(path) ? path : null;
		}
	}
}
