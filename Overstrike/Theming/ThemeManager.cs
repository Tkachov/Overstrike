// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace Overstrike.Theming {
	internal sealed class ThemeManager {
		private readonly Application _application;
		private readonly Dictionary<string, AppTheme> _themesById = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, BitmapImage> _bitmapCache = new(StringComparer.OrdinalIgnoreCase);
		private List<AppTheme> _availableThemes = new();
		private ResourceDictionary? _activeThemeResources;

		public static string CurrentOverstrikeVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
		public IReadOnlyList<AppTheme> AvailableThemes => _availableThemes;
		public AppTheme ActiveTheme { get; private set; }

		public ThemeManager(Application application) {
			_application = application;
			ActiveTheme = CreateDefaultTheme();
		}

		public void LoadThemes(string? selectedThemeId) {
			RemoveActiveThemeResources();
			ActiveTheme = CreateDefaultTheme();
			_availableThemes = new List<AppTheme>() { ActiveTheme };
			_themesById.Clear();
			_themesById[ActiveTheme.Id] = ActiveTheme;
			_bitmapCache.Clear();

			var themesPath = Path.Combine(AppContext.BaseDirectory, "Themes");
			if (Directory.Exists(themesPath)) {
				foreach (var directory in Directory.GetDirectories(themesPath)) {
					var theme = TryLoadTheme(directory);
					if (theme == null || _themesById.ContainsKey(theme.Id)) continue;

					_availableThemes.Add(theme);
					_themesById[theme.Id] = theme;
				}
			}

			ApplyThemeById(selectedThemeId);
		}

		public bool ApplyThemeById(string? themeId) {
			var theme = ResolveTheme(themeId);
			if (String.Equals(ActiveTheme.Id, theme.Id, StringComparison.OrdinalIgnoreCase) && ReferenceEquals(theme.Resources, _activeThemeResources)) {
				return false;
			}

			RemoveActiveThemeResources();
			if (theme.Resources != null) {
				_application.Resources.MergedDictionaries.Add(theme.Resources);
				_activeThemeResources = theme.Resources;
			}

			ActiveTheme = theme;
			_bitmapCache.Clear();
			return true;
		}

		public BitmapImage GetBitmapImage(string resourceKey) {
			if (_bitmapCache.TryGetValue(resourceKey, out var cached)) {
				return cached;
			}

			var image = _application.TryFindResource(resourceKey) as BitmapImage;
			if (image == null) {
				throw new KeyNotFoundException(resourceKey);
			}

			_bitmapCache[resourceKey] = image;
			return image;
		}

		private AppTheme ResolveTheme(string? themeId) {
			if (!String.IsNullOrWhiteSpace(themeId) && _themesById.TryGetValue(themeId, out var theme)) {
				return theme;
			}

			return _themesById[AppTheme.DEFAULT_THEME_ID];
		}

		private static AppTheme CreateDefaultTheme() {
			return new AppTheme() {
				Id = AppTheme.DEFAULT_THEME_ID,
				Name = "Default",
				Version = CurrentOverstrikeVersion,
				IsBuiltIn = true,
				Resources = null,
			};
		}

		private static AppTheme? TryLoadTheme(string directoryPath) {
			var themePath = Path.Combine(directoryPath, AppTheme.THEME_FILE_NAME);
			if (!File.Exists(themePath)) return null;

			try {
				var resources = LoadThemeResources(themePath);
				var name = resources.Contains(AppTheme.NAME_RESOURCE_KEY) ? (resources[AppTheme.NAME_RESOURCE_KEY] as string)?.Trim() : null;
				if (String.IsNullOrWhiteSpace(name)) {
					name = Path.GetFileName(directoryPath);
				}

				var version = resources.Contains(AppTheme.VERSION_RESOURCE_KEY) ? (resources[AppTheme.VERSION_RESOURCE_KEY] as string)?.Trim() : null;
				if (String.IsNullOrWhiteSpace(version)) {
					version = CurrentOverstrikeVersion;
				}

				return new AppTheme() {
					Id = Path.GetFileName(directoryPath),
					Name = name,
					Version = version,
					IsBuiltIn = false,
					Resources = resources,
				};
			} catch {
				return null;
			}
		}

		private static ResourceDictionary LoadThemeResources(string themePath) {
			using var stream = File.OpenRead(themePath);
			var parserContext = new ParserContext() {
				BaseUri = new Uri(themePath, UriKind.Absolute),
			};
			var loaded = XamlReader.Load(stream, parserContext);
			if (loaded is not ResourceDictionary resources) {
				throw new InvalidDataException(themePath);
			}

			return resources;
		}

		private void RemoveActiveThemeResources() {
			if (_activeThemeResources != null) {
				_application.Resources.MergedDictionaries.Remove(_activeThemeResources);
				_activeThemeResources = null;
			}
		}
	}
}
