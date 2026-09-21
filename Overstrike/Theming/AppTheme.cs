// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

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
