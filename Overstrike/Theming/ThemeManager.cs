using Newtonsoft.Json.Linq;
using Overstrike.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Overstrike.Theming {
	internal sealed class ThemeManager {
		private sealed class ThemeColorDefinition {
			public required string Key { get; init; }
			public required string DefaultValue { get; init; }
		}

		private static readonly ThemeColorDefinition[] COLOR_DEFINITIONS = new[] {
			new ThemeColorDefinition() { Key = "window_top_border", DefaultValue = "#EEE" },
			new ThemeColorDefinition() { Key = "window_background", DefaultValue = "#FFF" },
			new ThemeColorDefinition() { Key = "app_background", DefaultValue = "#EEE" },
			new ThemeColorDefinition() { Key = "panel_background", DefaultValue = "#F6F6F6" },
			new ThemeColorDefinition() { Key = "muted_text", DefaultValue = "#666" },
			new ThemeColorDefinition() { Key = "light_text", DefaultValue = "#AAA" },
			new ThemeColorDefinition() { Key = "light_border", DefaultValue = "#CCC" },
			new ThemeColorDefinition() { Key = "medium_border", DefaultValue = "#AAA" },
			new ThemeColorDefinition() { Key = "error_text", DefaultValue = "#F04" },
			new ThemeColorDefinition() { Key = "icon_button_hover", DefaultValue = "#EEE" },
			new ThemeColorDefinition() { Key = "icon_button_pressed", DefaultValue = "#CCC" },
			new ThemeColorDefinition() { Key = "suits_separator_background", DefaultValue = "Gray" },
			new ThemeColorDefinition() { Key = "suits_separator_foreground", DefaultValue = "White" },
			new ThemeColorDefinition() { Key = "msmr_selection", DefaultValue = "#098ae4" },
			new ThemeColorDefinition() { Key = "msmr_outer_border", DefaultValue = "#057" },
			new ThemeColorDefinition() { Key = "msmr_background", DefaultValue = "#034" },
			new ThemeColorDefinition() { Key = "msmr_slot_border", DefaultValue = "#00DDEE" },
			new ThemeColorDefinition() { Key = "msmr_side_background", DefaultValue = "#FFF" },
			new ThemeColorDefinition() { Key = "msmr_divider", DefaultValue = "#CCC" },
			new ThemeColorDefinition() { Key = "mm_selection", DefaultValue = "#098ae4" },
			new ThemeColorDefinition() { Key = "mm_outer_border", DefaultValue = "#0D1C2B" },
			new ThemeColorDefinition() { Key = "mm_background", DefaultValue = "#070C1B" },
			new ThemeColorDefinition() { Key = "mm_list_background", DefaultValue = "#0D1C2B" },
			new ThemeColorDefinition() { Key = "mm_slot_border", DefaultValue = "#0FF" },
			new ThemeColorDefinition() { Key = "mm_side_background", DefaultValue = "#FFF" },
			new ThemeColorDefinition() { Key = "mm_divider", DefaultValue = "#CCC" },
			new ThemeColorDefinition() { Key = "msm2_tab_foreground", DefaultValue = "#CCC" },
			new ThemeColorDefinition() { Key = "msm2_tab_background", DefaultValue = "#032338" },
			new ThemeColorDefinition() { Key = "msm2_tab_border", DefaultValue = "#032338" },
			new ThemeColorDefinition() { Key = "msm2_tab_hover_border", DefaultValue = "#3CCCF7" },
			new ThemeColorDefinition() { Key = "msm2_tab_hover_foreground", DefaultValue = "#62F7F8" },
			new ThemeColorDefinition() { Key = "msm2_tab_active_foreground", DefaultValue = "#62F7F8" },
			new ThemeColorDefinition() { Key = "msm2_tab_active_background", DefaultValue = "#389ED5" },
			new ThemeColorDefinition() { Key = "msm2_tab_active_border", DefaultValue = "#3CCCF7" },
			new ThemeColorDefinition() { Key = "msm2_selection", DefaultValue = "#61EFF0" },
			new ThemeColorDefinition() { Key = "msm2_outer_border", DefaultValue = "#06132F" },
			new ThemeColorDefinition() { Key = "msm2_background", DefaultValue = "#000B1F" },
			new ThemeColorDefinition() { Key = "msm2_list_background", DefaultValue = "#06132F" },
			new ThemeColorDefinition() { Key = "msm2_slot_border", DefaultValue = "#61EFF0" },
			new ThemeColorDefinition() { Key = "msm2_side_background", DefaultValue = "#FFF" },
			new ThemeColorDefinition() { Key = "msm2_divider", DefaultValue = "#DDD" },
		};

		private static readonly (string Key, string FileName, Func<byte[]> BuiltInBytes)[] RESOURCE_DEFINITIONS = new (string Key, string FileName, Func<byte[]> BuiltInBytes)[] {
			("add_icon", "add_icon.png", () => Properties.Resources.add_icon),
			("badge_mmpc", "badge_mmpc.png", () => Properties.Resources.badge_mmpc),
			("badge_modular", "badge_modular.png", () => Properties.Resources.badge_modular),
			("badge_script", "badge_script.png", () => Properties.Resources.badge_script),
			("badge_smpc", "badge_smpc.png", () => Properties.Resources.badge_smpc),
			("badge_stage", "badge_stage.png", () => Properties.Resources.badge_stage),
			("badge_style", "badge_style.png", () => Properties.Resources.badge_style),
			("badge_suit", "badge_suit.png", () => Properties.Resources.badge_suit),
			("badge_suit2", "badge_suit2.png", () => Properties.Resources.badge_suit2),
			("banner_i30_back", "banner_i30_back.png", () => Properties.Resources.banner_i30_back),
			("banner_i30_logo", "banner_i30_logo.png", () => Properties.Resources.banner_i30_logo),
			("banner_i30_logo2", "banner_i30_logo2.png", () => Properties.Resources.banner_i30_logo2),
			("banner_i33_logo", "banner_i33_logo.png", () => Properties.Resources.banner_i33_logo),
			("banner_mm_back", "banner_mm_back.png", () => Properties.Resources.banner_mm_back),
			("banner_mm_logo", "banner_mm_logo.png", () => Properties.Resources.banner_mm_logo),
			("banner_mm_logo2", "banner_mm_logo2.png", () => Properties.Resources.banner_mm_logo2),
			("banner_msm2_logo", "banner_msm2_logo.png", () => Properties.Resources.banner_msm2_logo),
			("banner_msm2_logo2", "banner_msm2_logo2.png", () => Properties.Resources.banner_msm2_logo2),
			("banner_msmr_back", "banner_msmr_back.png", () => Properties.Resources.banner_msmr_back),
			("banner_msmr_logo", "banner_msmr_logo.png", () => Properties.Resources.banner_msmr_logo),
			("banner_msmr_logo2", "banner_msmr_logo2.png", () => Properties.Resources.banner_msmr_logo2),
			("banner_rcra_back", "banner_rcra_back.png", () => Properties.Resources.banner_rcra_back),
			("banner_rcra_logo", "banner_rcra_logo.png", () => Properties.Resources.banner_rcra_logo),
			("banner_rcra_logo2", "banner_rcra_logo2.png", () => Properties.Resources.banner_rcra_logo2),
			("reload_icon", "reload_icon.png", () => Properties.Resources.reload_icon),
			("suit_missing", "suit_missing.png", () => Properties.Resources.suit_missing),
			("suit_missing_mm", "suit_missing_mm.png", () => Properties.Resources.suit_missing_mm),
			("suit_missing_mm_big", "suit_missing_mm_big.png", () => Properties.Resources.suit_missing_mm_big),
			("suit_missing_msm2", "suit_missing_msm2.png", () => Properties.Resources.suit_missing_msm2),
		};

		private readonly Application _application;
		private readonly Dictionary<string, AppTheme> _themesById = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, BitmapImage> _bitmapCache = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, (string FileName, Func<byte[]> BuiltInBytes)> _resourceDefinitions = new(StringComparer.OrdinalIgnoreCase);
		private List<AppTheme> _availableThemes = new();

		public static string CurrentOverstrikeVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
		public IReadOnlyList<AppTheme> AvailableThemes => _availableThemes;
		public AppTheme ActiveTheme { get; private set; }
		public event Action? ThemeChanged;

		public ThemeManager(Application application) {
			_application = application;
			ActiveTheme = CreateDefaultTheme();

			foreach (var definition in RESOURCE_DEFINITIONS) {
				_resourceDefinitions[definition.Key] = (definition.FileName, definition.BuiltInBytes);
			}
		}

		public void LoadThemes(string? selectedThemeId) {
			_availableThemes = new List<AppTheme>() { CreateDefaultTheme() };
			_themesById.Clear();

			foreach (var theme in _availableThemes) {
				_themesById[theme.Id] = theme;
			}

			var themesPath = Path.Combine(AppContext.BaseDirectory, "Themes");
			if (Directory.Exists(themesPath)) {
				foreach (var directory in Directory.GetDirectories(themesPath)) {
					var theme = TryLoadTheme(directory);
					if (theme == null || _themesById.ContainsKey(theme.Id)) continue;

					_availableThemes.Add(theme);
					_themesById[theme.Id] = theme;
				}
			}

			ApplyThemeById(selectedThemeId, true);
		}

		public bool ApplyThemeById(string? themeId, bool silentFallback = false) {
			var theme = ResolveTheme(themeId);
			var changed = !String.Equals(ActiveTheme.Id, theme.Id, StringComparison.OrdinalIgnoreCase);

			ActiveTheme = theme;
			ApplyBrushResources(theme);
			_bitmapCache.Clear();

			ThemeChanged?.Invoke();
			return changed || (!silentFallback && !String.Equals(themeId, theme.Id, StringComparison.OrdinalIgnoreCase));
		}

		public BitmapImage GetBitmapImage(string resourceKey) {
			if (_bitmapCache.TryGetValue(resourceKey, out var cached)) {
				return cached;
			}

			if (!_resourceDefinitions.TryGetValue(resourceKey, out var definition)) {
				throw new KeyNotFoundException(resourceKey);
			}

			BitmapImage image = null;

			var overridePath = ActiveTheme.GetResourceOverridePath(definition.FileName);
			if (overridePath != null) {
				try {
					image = Imaging.ConvertToBitmapImage(File.ReadAllBytes(overridePath));
				} catch {}
			}

			image ??= Imaging.ConvertToBitmapImage(definition.BuiltInBytes());
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
			var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var definition in COLOR_DEFINITIONS) {
				colors[definition.Key] = definition.DefaultValue;
			}

			return new AppTheme() {
				Id = AppTheme.DEFAULT_THEME_ID,
				Name = "Default",
				Author = "Overstrike",
				OverstrikeVersion = CurrentOverstrikeVersion,
				Colors = colors,
				IsBuiltIn = true,
				DirectoryPath = null,
			};
		}

		private AppTheme? TryLoadTheme(string directoryPath) {
			var manifestPath = Path.Combine(directoryPath, "theme.json");
			if (!File.Exists(manifestPath)) return null;

			try {
				var json = JObject.Parse(File.ReadAllText(manifestPath));
				var name = ((string?)json["name"])?.Trim();
				var author = ((string?)json["author"])?.Trim();
				var version = ((string?)json["overstrike_version"])?.Trim();
				if (String.IsNullOrWhiteSpace(name) || String.IsNullOrWhiteSpace(author) || String.IsNullOrWhiteSpace(version)) {
					return null;
				}

				var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				var colorsJson = json["colors"] as JObject;
				foreach (var definition in COLOR_DEFINITIONS) {
					var value = (string?)colorsJson?[definition.Key];
					colors[definition.Key] = TryParseColor(value, out _) ? value! : definition.DefaultValue;
				}

				return new AppTheme() {
					Id = Path.GetFileName(directoryPath),
					Name = name,
					Author = author,
					OverstrikeVersion = version,
					Colors = colors,
					IsBuiltIn = false,
					DirectoryPath = directoryPath,
				};
			} catch {
				return null;
			}
		}

		private void ApplyBrushResources(AppTheme theme) {
			foreach (var definition in COLOR_DEFINITIONS) {
				var colorText = theme.Colors.GetValueOrDefault(definition.Key) ?? definition.DefaultValue;
				if (!TryParseColor(colorText, out var color)) {
					TryParseColor(definition.DefaultValue, out color);
				}

				var brush = new SolidColorBrush(color);
				brush.Freeze();
				_application.Resources[definition.Key] = brush;
			}
		}

		private static bool TryParseColor(string? value, out Color color) {
			try {
				if (!String.IsNullOrWhiteSpace(value)) {
					var parsed = ColorConverter.ConvertFromString(value);
					if (parsed != null) {
						color = (Color)parsed;
						return true;
					}
				}
			} catch {}

			color = default;
			return false;
		}
	}
}
