// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

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
		private const string DEFAULT_WINDOW_TOP_BORDER = "#EEE";
		private const string DEFAULT_WINDOW_BACKGROUND = "#FFF";
		private const string DEFAULT_APP_BACKGROUND = "#EEE";
		private const string DEFAULT_PANEL_BACKGROUND = "#F6F6F6";
		private const string DEFAULT_MUTED_TEXT = "#666";
		private const string DEFAULT_LIGHT_TEXT = "#AAA";
		private const string DEFAULT_LIGHT_BORDER = "#CCC";
		private const string DEFAULT_MEDIUM_BORDER = "#AAA";
		private const string DEFAULT_ERROR_TEXT = "#F04";
		private const string DEFAULT_SEPARATOR_BUTTON_BACKGROUND = "Gray";
		private const string DEFAULT_SEPARATOR_BUTTON_FOREGROUND = "White";
		private const string DEFAULT_MSMR_SELECTION = "#098ae4";
		private const string DEFAULT_MSMR_OUTER_BORDER = "#057";
		private const string DEFAULT_MSMR_BACKGROUND = "#034";
		private const string DEFAULT_MSMR_SLOT_BORDER = "#00DDEE";
		private const string DEFAULT_MM_OUTER_BORDER = "#0D1C2B";
		private const string DEFAULT_MM_BACKGROUND = "#070C1B";
		private const string DEFAULT_MM_SLOT_BORDER = "#0FF";
		private const string DEFAULT_MSM2_TAB_FOREGROUND = "#CCC";
		private const string DEFAULT_MSM2_TAB_BACKGROUND = "#032338";
		private const string DEFAULT_MSM2_TAB_BORDER = "#032338";
		private const string DEFAULT_MSM2_TAB_HOVER_BORDER = "#3CCCF7";
		private const string DEFAULT_MSM2_TAB_HOVER_FOREGROUND = "#62F7F8";
		private const string DEFAULT_MSM2_TAB_ACTIVE_FOREGROUND = "#62F7F8";
		private const string DEFAULT_MSM2_TAB_ACTIVE_BACKGROUND = "#389ED5";
		private const string DEFAULT_MSM2_TAB_ACTIVE_BORDER = "#3CCCF7";
		private const string DEFAULT_MSM2_SELECTION = "#61EFF0";
		private const string DEFAULT_MSM2_OUTER_BORDER = "#06132F";
		private const string DEFAULT_MSM2_BACKGROUND = "#000B1F";
		private const string DEFAULT_MSM2_LIST_BACKGROUND = "#06132F";
		private const string DEFAULT_MSM2_SLOT_BORDER = "#61EFF0";
		private const string DEFAULT_MSM2_DIVIDER = "#DDD";

		private static readonly Dictionary<string, string> DEFAULT_COLOR_VALUES = new(StringComparer.OrdinalIgnoreCase) {
			["MainWindow_IconButtonHoverBackground"] = DEFAULT_WINDOW_TOP_BORDER,
			["MainWindow_IconButtonPressedBackground"] = DEFAULT_LIGHT_BORDER,
			["MainWindow_WindowTopBorder"] = DEFAULT_WINDOW_TOP_BORDER,
			["MainWindow_Background"] = DEFAULT_WINDOW_BACKGROUND,
			["MainWindow_OverlayBackground"] = DEFAULT_WINDOW_BACKGROUND,
			["MainWindow_ModsOrderLabelForeground"] = DEFAULT_LIGHT_TEXT,
			["MainWindow_StatusMessageForeground"] = DEFAULT_LIGHT_TEXT,
			["MainWindow_StatusMessageErrorForeground"] = DEFAULT_ERROR_TEXT,
			["MainWindow_SettingsGlobalDescriptionForeground"] = DEFAULT_MUTED_TEXT,
			["MainWindow_ScriptWarningForeground"] = DEFAULT_ERROR_TEXT,
			["FirstLaunch_WindowTopBorder"] = DEFAULT_WINDOW_TOP_BORDER,
			["FirstLaunch_Background"] = DEFAULT_WINDOW_BACKGROUND,
			["FirstLaunch_ProfilesHeaderBackground"] = DEFAULT_PANEL_BACKGROUND,
			["FirstLaunch_ProfilesHeaderBorder"] = DEFAULT_MEDIUM_BORDER,
			["FirstLaunch_ProfilesHeaderProfileNameForeground"] = DEFAULT_MUTED_TEXT,
			["FirstLaunch_ProfilesHeaderGamePathForeground"] = DEFAULT_MUTED_TEXT,
			["FirstLaunch_ProfilesListBorder"] = DEFAULT_MEDIUM_BORDER,
			["CreateProfile_WindowTopBorder"] = DEFAULT_WINDOW_TOP_BORDER,
			["CreateProfile_Background"] = DEFAULT_WINDOW_BACKGROUND,
			["ModsDetectionSplash_TopBorder"] = DEFAULT_LIGHT_BORDER,
			["ModsDetectionSplash_Background"] = DEFAULT_WINDOW_BACKGROUND,
			["ModsDetectionSplash_OperationForeground"] = DEFAULT_MUTED_TEXT,
			["ErrorLogWindow_Background"] = DEFAULT_APP_BACKGROUND,
			["ModularWizard_Border"] = DEFAULT_LIGHT_BORDER,
			["ModularWizard_MainBackground"] = DEFAULT_WINDOW_BACKGROUND,
			["ModularWizard_FooterBackground"] = DEFAULT_WINDOW_BACKGROUND,
			["ModularWizard_NumberLabelForeground"] = DEFAULT_LIGHT_BORDER,
			["TocMismatchDialog_WindowTopBorder"] = DEFAULT_WINDOW_TOP_BORDER,
			["TocMismatchDialog_Background"] = DEFAULT_WINDOW_BACKGROUND,
			["TocMismatchDialog_FooterForeground"] = DEFAULT_MUTED_TEXT,
			["MSMRSuitsMenu_SeparatorButtonForeground"] = DEFAULT_SEPARATOR_BUTTON_FOREGROUND,
			["MSMRSuitsMenu_SeparatorButtonBackground"] = DEFAULT_SEPARATOR_BUTTON_BACKGROUND,
			["MSMRSuitsMenu_ListSelectionBackground"] = DEFAULT_MSMR_SELECTION,
			["MSMRSuitsMenu_OuterBorder"] = DEFAULT_MSMR_OUTER_BORDER,
			["MSMRSuitsMenu_Background"] = DEFAULT_MSMR_BACKGROUND,
			["MSMRSuitsMenu_ListBackground"] = DEFAULT_MSMR_OUTER_BORDER,
			["MSMRSuitsMenu_SlotBorder"] = DEFAULT_MSMR_SLOT_BORDER,
			["MSMRSuitsMenu_SideBackground"] = DEFAULT_WINDOW_BACKGROUND,
			["MSMRSuitsMenu_Divider"] = DEFAULT_LIGHT_BORDER,
			["MMSuitsMenu_SeparatorButtonForeground"] = DEFAULT_SEPARATOR_BUTTON_FOREGROUND,
			["MMSuitsMenu_SeparatorButtonBackground"] = DEFAULT_SEPARATOR_BUTTON_BACKGROUND,
			["MMSuitsMenu_ListSelectionBackground"] = DEFAULT_MSMR_SELECTION,
			["MMSuitsMenu_OuterBorder"] = DEFAULT_MM_OUTER_BORDER,
			["MMSuitsMenu_Background"] = DEFAULT_MM_BACKGROUND,
			["MMSuitsMenu_ListBackground"] = DEFAULT_MM_OUTER_BORDER,
			["MMSuitsMenu_SlotBorder"] = DEFAULT_MM_SLOT_BORDER,
			["MMSuitsMenu_SideBackground"] = DEFAULT_WINDOW_BACKGROUND,
			["MMSuitsMenu_Divider"] = DEFAULT_LIGHT_BORDER,
			["MSM2SuitsMenu_SeparatorButtonForeground"] = DEFAULT_SEPARATOR_BUTTON_FOREGROUND,
			["MSM2SuitsMenu_SeparatorButtonBackground"] = DEFAULT_SEPARATOR_BUTTON_BACKGROUND,
			["MSM2SuitsMenu_CharacterTabForeground"] = DEFAULT_MSM2_TAB_FOREGROUND,
			["MSM2SuitsMenu_CharacterTabBackground"] = DEFAULT_MSM2_TAB_BACKGROUND,
			["MSM2SuitsMenu_CharacterTabBorder"] = DEFAULT_MSM2_TAB_BORDER,
			["MSM2SuitsMenu_CharacterTabHoverBorder"] = DEFAULT_MSM2_TAB_HOVER_BORDER,
			["MSM2SuitsMenu_CharacterTabHoverForeground"] = DEFAULT_MSM2_TAB_HOVER_FOREGROUND,
			["MSM2SuitsMenu_CharacterTabActiveForeground"] = DEFAULT_MSM2_TAB_ACTIVE_FOREGROUND,
			["MSM2SuitsMenu_CharacterTabActiveBackground"] = DEFAULT_MSM2_TAB_ACTIVE_BACKGROUND,
			["MSM2SuitsMenu_CharacterTabActiveBorder"] = DEFAULT_MSM2_TAB_ACTIVE_BORDER,
			["MSM2SuitsMenu_ListSelectionBackground"] = DEFAULT_MSM2_SELECTION,
			["MSM2SuitsMenu_OuterBorder"] = DEFAULT_MSM2_OUTER_BORDER,
			["MSM2SuitsMenu_Background"] = DEFAULT_MSM2_BACKGROUND,
			["MSM2SuitsMenu_TabStripBackground"] = DEFAULT_MSM2_BACKGROUND,
			["MSM2SuitsMenu_ListBackground"] = DEFAULT_MSM2_LIST_BACKGROUND,
			["MSM2SuitsMenu_SlotBorder"] = DEFAULT_MSM2_SLOT_BORDER,
			["MSM2SuitsMenu_SideBackground"] = DEFAULT_WINDOW_BACKGROUND,
			["MSM2SuitsMenu_HeaderDivider"] = DEFAULT_MSM2_DIVIDER,
			["MSM2SuitsMenu_SpiderArmsWarningForeground"] = DEFAULT_MUTED_TEXT,
			["MSM2SuitsMenu_FooterDivider"] = DEFAULT_MSM2_DIVIDER,
		};

		private static readonly Dictionary<string, Func<byte[]>> RESOURCE_DEFINITIONS = new(StringComparer.OrdinalIgnoreCase) {
			["add_icon.png"] = () => Properties.Resources.add_icon,
			["badge_mmpc.png"] = () => Properties.Resources.badge_mmpc,
			["badge_modular.png"] = () => Properties.Resources.badge_modular,
			["badge_script.png"] = () => Properties.Resources.badge_script,
			["badge_smpc.png"] = () => Properties.Resources.badge_smpc,
			["badge_stage.png"] = () => Properties.Resources.badge_stage,
			["badge_style.png"] = () => Properties.Resources.badge_style,
			["badge_suit.png"] = () => Properties.Resources.badge_suit,
			["badge_suit2.png"] = () => Properties.Resources.badge_suit2,
			["banner_i30_back.png"] = () => Properties.Resources.banner_i30_back,
			["banner_i30_logo.png"] = () => Properties.Resources.banner_i30_logo,
			["banner_i30_logo2.png"] = () => Properties.Resources.banner_i30_logo2,
			["banner_i33_back.png"] = () => Properties.Resources.banner_i30_back,
			["banner_i33_logo.png"] = () => Properties.Resources.banner_i33_logo,
			["banner_i33_logo2.png"] = () => Properties.Resources.banner_i30_logo2,
			["banner_mm_back.png"] = () => Properties.Resources.banner_mm_back,
			["banner_mm_logo.png"] = () => Properties.Resources.banner_mm_logo,
			["banner_mm_logo2.png"] = () => Properties.Resources.banner_mm_logo2,
			["banner_msm2_back.png"] = () => Properties.Resources.banner_msmr_back,
			["banner_msm2_logo.png"] = () => Properties.Resources.banner_msm2_logo,
			["banner_msm2_logo2.png"] = () => Properties.Resources.banner_msm2_logo2,
			["banner_msmr_back.png"] = () => Properties.Resources.banner_msmr_back,
			["banner_msmr_logo.png"] = () => Properties.Resources.banner_msmr_logo,
			["banner_msmr_logo2.png"] = () => Properties.Resources.banner_msmr_logo2,
			["banner_rcra_back.png"] = () => Properties.Resources.banner_rcra_back,
			["banner_rcra_logo.png"] = () => Properties.Resources.banner_rcra_logo,
			["banner_rcra_logo2.png"] = () => Properties.Resources.banner_rcra_logo2,
			["reload_icon.png"] = () => Properties.Resources.reload_icon,
			["suit_missing.png"] = () => Properties.Resources.suit_missing,
			["suit_missing_mm.png"] = () => Properties.Resources.suit_missing_mm,
			["suit_missing_mm_big.png"] = () => Properties.Resources.suit_missing_mm_big,
			["suit_missing_msm2.png"] = () => Properties.Resources.suit_missing_msm2,
		};

		private readonly Application _application;
		private readonly Dictionary<string, AppTheme> _themesById = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, BitmapImage> _bitmapCache = new(StringComparer.OrdinalIgnoreCase);
		private List<AppTheme> _availableThemes = new();

		public static string CurrentOverstrikeVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
		public IReadOnlyList<AppTheme> AvailableThemes => _availableThemes;
		public AppTheme ActiveTheme { get; private set; }

		public ThemeManager(Application application) {
			_application = application;
			ActiveTheme = CreateDefaultTheme();
			ApplyBrushResources(ActiveTheme);
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

			ApplyThemeById(selectedThemeId);
		}

		public bool ApplyThemeById(string? themeId) {
			var theme = ResolveTheme(themeId);
			if (String.Equals(ActiveTheme.Id, theme.Id, StringComparison.OrdinalIgnoreCase)) {
				return false;
			}

			ActiveTheme = theme;
			ApplyBrushResources(theme);
			_bitmapCache.Clear();
			return true;
		}

		public BitmapImage GetBitmapImage(string fileName) {
			if (_bitmapCache.TryGetValue(fileName, out var cached)) {
				return cached;
			}

			if (!RESOURCE_DEFINITIONS.TryGetValue(fileName, out var builtInBytes)) {
				throw new KeyNotFoundException(fileName);
			}

			BitmapImage image = null;
			var overridePath = ActiveTheme.GetResourceOverridePath(fileName);
			if (overridePath != null) {
				try {
					image = Imaging.ConvertToBitmapImage(File.ReadAllBytes(overridePath));
				} catch {}
			}

			image ??= Imaging.ConvertToBitmapImage(builtInBytes());
			_bitmapCache[fileName] = image;
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
				Colors = new Dictionary<string, string>(DEFAULT_COLOR_VALUES, StringComparer.OrdinalIgnoreCase),
				IsBuiltIn = true,
				DirectoryPath = null,
			};
		}

		private AppTheme? TryLoadTheme(string directoryPath) {
			var manifestPath = Path.Combine(directoryPath, "Theme.json");
			if (!File.Exists(manifestPath)) return null;

			try {
				var json = JObject.Parse(File.ReadAllText(manifestPath));
				var name = ((string?)json["name"])?.Trim();
				var version = ((string?)json["version"])?.Trim();
				if (String.IsNullOrWhiteSpace(name) || String.IsNullOrWhiteSpace(version)) {
					return null;
				}

				var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				var colorsJson = json["colors"] as JObject;
				if (colorsJson != null) {
					foreach (var property in colorsJson.Properties()) {
						var value = ((string?)property.Value)?.Trim();
						if (!String.IsNullOrWhiteSpace(value)) {
							colors[property.Name] = value;
						}
					}
				}

				return new AppTheme() {
					Id = Path.GetFileName(directoryPath),
					Name = name,
					Version = version,
					Colors = colors,
					IsBuiltIn = false,
					DirectoryPath = directoryPath,
				};
			} catch {
				return null;
			}
		}

		private void ApplyBrushResources(AppTheme theme) {
			var values = new Dictionary<string, string>(DEFAULT_COLOR_VALUES, StringComparer.OrdinalIgnoreCase);
			foreach (var pair in theme.Colors) {
				values[pair.Key] = pair.Value;
			}

			foreach (var resourceKey in DEFAULT_COLOR_VALUES.Keys) {
				if (!TryResolveColor(values, resourceKey, out var color)) {
					TryResolveColor(DEFAULT_COLOR_VALUES, resourceKey, out color);
				}

				var brush = new SolidColorBrush(color);
				brush.Freeze();
				_application.Resources[resourceKey] = brush;
			}
		}

		private static bool TryResolveColor(IReadOnlyDictionary<string, string> values, string key, out Color color) {
			if (TryResolveColorText(values, key, new HashSet<string>(StringComparer.OrdinalIgnoreCase), out var value)) {
				return TryParseColor(value, out color);
			}

			color = default;
			return false;
		}

		private static bool TryResolveColorText(IReadOnlyDictionary<string, string> values, string key, HashSet<string> chain, out string value) {
			value = null;
			if (!values.TryGetValue(key, out var rawValue) || String.IsNullOrWhiteSpace(rawValue)) {
				return false;
			}

			if (TryParseColor(rawValue, out _)) {
				value = rawValue;
				return true;
			}

			if (!chain.Add(key)) {
				return false;
			}

			try {
				return TryResolveColorText(values, rawValue, chain, out value);
			} finally {
				chain.Remove(key);
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
