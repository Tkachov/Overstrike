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
		private static readonly string[] FINAL_COLOR_KEYS = new[] {
			"MainWindow_IconButtonHoverBackground",
			"MainWindow_IconButtonPressedBackground",
			"MainWindow_WindowTopBorder",
			"MainWindow_Background",
			"MainWindow_OverlayBackground",
			"MainWindow_ModsOrderLabelForeground",
			"MainWindow_StatusMessageForeground",
			"MainWindow_StatusMessageErrorForeground",
			"MainWindow_SettingsGlobalDescriptionForeground",
			"MainWindow_ScriptWarningForeground",
			"FirstLaunch_WindowTopBorder",
			"FirstLaunch_Background",
			"FirstLaunch_ProfilesHeaderBackground",
			"FirstLaunch_ProfilesHeaderBorder",
			"FirstLaunch_ProfilesHeaderProfileNameForeground",
			"FirstLaunch_ProfilesHeaderGamePathForeground",
			"FirstLaunch_ProfilesListBorder",
			"CreateProfile_WindowTopBorder",
			"CreateProfile_Background",
			"ModsDetectionSplash_TopBorder",
			"ModsDetectionSplash_Background",
			"ModsDetectionSplash_OperationForeground",
			"ErrorLogWindow_Background",
			"ModularWizard_Border",
			"ModularWizard_MainBackground",
			"ModularWizard_FooterBackground",
			"ModularWizard_NumberLabelForeground",
			"TocMismatchDialog_WindowTopBorder",
			"TocMismatchDialog_Background",
			"TocMismatchDialog_FooterForeground",
			"MSMRSuitsMenu_SeparatorButtonForeground",
			"MSMRSuitsMenu_SeparatorButtonBackground",
			"MSMRSuitsMenu_ListSelectionBackground",
			"MSMRSuitsMenu_OuterBorder",
			"MSMRSuitsMenu_Background",
			"MSMRSuitsMenu_ListBackground",
			"MSMRSuitsMenu_SlotBorder",
			"MSMRSuitsMenu_SideBackground",
			"MSMRSuitsMenu_Divider",
			"MMSuitsMenu_SeparatorButtonForeground",
			"MMSuitsMenu_SeparatorButtonBackground",
			"MMSuitsMenu_ListSelectionBackground",
			"MMSuitsMenu_OuterBorder",
			"MMSuitsMenu_Background",
			"MMSuitsMenu_ListBackground",
			"MMSuitsMenu_SlotBorder",
			"MMSuitsMenu_SideBackground",
			"MMSuitsMenu_Divider",
			"MSM2SuitsMenu_SeparatorButtonForeground",
			"MSM2SuitsMenu_SeparatorButtonBackground",
			"MSM2SuitsMenu_CharacterTabForeground",
			"MSM2SuitsMenu_CharacterTabBackground",
			"MSM2SuitsMenu_CharacterTabBorder",
			"MSM2SuitsMenu_CharacterTabHoverBorder",
			"MSM2SuitsMenu_CharacterTabHoverForeground",
			"MSM2SuitsMenu_CharacterTabActiveForeground",
			"MSM2SuitsMenu_CharacterTabActiveBackground",
			"MSM2SuitsMenu_CharacterTabActiveBorder",
			"MSM2SuitsMenu_ListSelectionBackground",
			"MSM2SuitsMenu_OuterBorder",
			"MSM2SuitsMenu_Background",
			"MSM2SuitsMenu_TabStripBackground",
			"MSM2SuitsMenu_ListBackground",
			"MSM2SuitsMenu_SlotBorder",
			"MSM2SuitsMenu_SideBackground",
			"MSM2SuitsMenu_HeaderDivider",
			"MSM2SuitsMenu_SpiderArmsWarningForeground",
			"MSM2SuitsMenu_FooterDivider",
		};

		private static readonly Dictionary<string, string> DEFAULT_COLOR_VALUES = new(StringComparer.OrdinalIgnoreCase) {
			["Shared_WindowTopBorder"] = "#EEE",
			["Shared_WindowBackground"] = "#FFF",
			["Shared_AppBackground"] = "#EEE",
			["Shared_PanelBackground"] = "#F6F6F6",
			["Shared_MutedText"] = "#666",
			["Shared_LightText"] = "#AAA",
			["Shared_LightBorder"] = "#CCC",
			["Shared_MediumBorder"] = "#AAA",
			["Shared_ErrorText"] = "#F04",
			["Shared_SeparatorButtonBackground"] = "Gray",
			["Shared_SeparatorButtonForeground"] = "White",
			["Shared_MSMRSelection"] = "#098ae4",
			["Shared_MSMROuterBorder"] = "#057",
			["Shared_MSMRBackground"] = "#034",
			["Shared_MSMRSlotBorder"] = "#00DDEE",
			["Shared_MMSuitsOuterBorder"] = "#0D1C2B",
			["Shared_MMSuitsBackground"] = "#070C1B",
			["Shared_MMSuitsSlotBorder"] = "#0FF",
			["Shared_MSM2TabForeground"] = "#CCC",
			["Shared_MSM2TabBackground"] = "#032338",
			["Shared_MSM2TabBorder"] = "#032338",
			["Shared_MSM2TabHoverBorder"] = "#3CCCF7",
			["Shared_MSM2TabHoverForeground"] = "#62F7F8",
			["Shared_MSM2TabActiveForeground"] = "#62F7F8",
			["Shared_MSM2TabActiveBackground"] = "#389ED5",
			["Shared_MSM2TabActiveBorder"] = "#3CCCF7",
			["Shared_MSM2Selection"] = "#61EFF0",
			["Shared_MSM2OuterBorder"] = "#06132F",
			["Shared_MSM2Background"] = "#000B1F",
			["Shared_MSM2ListBackground"] = "#06132F",
			["Shared_MSM2SlotBorder"] = "#61EFF0",
			["Shared_MSM2Divider"] = "#DDD",
			["MainWindow_IconButtonHoverBackground"] = "Shared_WindowTopBorder",
			["MainWindow_IconButtonPressedBackground"] = "Shared_LightBorder",
			["MainWindow_WindowTopBorder"] = "Shared_WindowTopBorder",
			["MainWindow_Background"] = "Shared_WindowBackground",
			["MainWindow_OverlayBackground"] = "Shared_WindowBackground",
			["MainWindow_ModsOrderLabelForeground"] = "Shared_LightText",
			["MainWindow_StatusMessageForeground"] = "Shared_LightText",
			["MainWindow_StatusMessageErrorForeground"] = "Shared_ErrorText",
			["MainWindow_SettingsGlobalDescriptionForeground"] = "Shared_MutedText",
			["MainWindow_ScriptWarningForeground"] = "Shared_ErrorText",
			["FirstLaunch_WindowTopBorder"] = "Shared_WindowTopBorder",
			["FirstLaunch_Background"] = "Shared_WindowBackground",
			["FirstLaunch_ProfilesHeaderBackground"] = "Shared_PanelBackground",
			["FirstLaunch_ProfilesHeaderBorder"] = "Shared_MediumBorder",
			["FirstLaunch_ProfilesHeaderProfileNameForeground"] = "Shared_MutedText",
			["FirstLaunch_ProfilesHeaderGamePathForeground"] = "Shared_MutedText",
			["FirstLaunch_ProfilesListBorder"] = "Shared_MediumBorder",
			["CreateProfile_WindowTopBorder"] = "Shared_WindowTopBorder",
			["CreateProfile_Background"] = "Shared_WindowBackground",
			["ModsDetectionSplash_TopBorder"] = "Shared_LightBorder",
			["ModsDetectionSplash_Background"] = "Shared_WindowBackground",
			["ModsDetectionSplash_OperationForeground"] = "Shared_MutedText",
			["ErrorLogWindow_Background"] = "Shared_AppBackground",
			["ModularWizard_Border"] = "Shared_LightBorder",
			["ModularWizard_MainBackground"] = "Shared_WindowBackground",
			["ModularWizard_FooterBackground"] = "Shared_WindowBackground",
			["ModularWizard_NumberLabelForeground"] = "Shared_LightBorder",
			["TocMismatchDialog_WindowTopBorder"] = "Shared_WindowTopBorder",
			["TocMismatchDialog_Background"] = "Shared_WindowBackground",
			["TocMismatchDialog_FooterForeground"] = "Shared_MutedText",
			["MSMRSuitsMenu_SeparatorButtonForeground"] = "Shared_SeparatorButtonForeground",
			["MSMRSuitsMenu_SeparatorButtonBackground"] = "Shared_SeparatorButtonBackground",
			["MSMRSuitsMenu_ListSelectionBackground"] = "Shared_MSMRSelection",
			["MSMRSuitsMenu_OuterBorder"] = "Shared_MSMROuterBorder",
			["MSMRSuitsMenu_Background"] = "Shared_MSMRBackground",
			["MSMRSuitsMenu_ListBackground"] = "Shared_MSMROuterBorder",
			["MSMRSuitsMenu_SlotBorder"] = "Shared_MSMRSlotBorder",
			["MSMRSuitsMenu_SideBackground"] = "Shared_WindowBackground",
			["MSMRSuitsMenu_Divider"] = "Shared_LightBorder",
			["MMSuitsMenu_SeparatorButtonForeground"] = "Shared_SeparatorButtonForeground",
			["MMSuitsMenu_SeparatorButtonBackground"] = "Shared_SeparatorButtonBackground",
			["MMSuitsMenu_ListSelectionBackground"] = "Shared_MSMRSelection",
			["MMSuitsMenu_OuterBorder"] = "Shared_MMSuitsOuterBorder",
			["MMSuitsMenu_Background"] = "Shared_MMSuitsBackground",
			["MMSuitsMenu_ListBackground"] = "Shared_MMSuitsOuterBorder",
			["MMSuitsMenu_SlotBorder"] = "Shared_MMSuitsSlotBorder",
			["MMSuitsMenu_SideBackground"] = "Shared_WindowBackground",
			["MMSuitsMenu_Divider"] = "Shared_LightBorder",
			["MSM2SuitsMenu_SeparatorButtonForeground"] = "Shared_SeparatorButtonForeground",
			["MSM2SuitsMenu_SeparatorButtonBackground"] = "Shared_SeparatorButtonBackground",
			["MSM2SuitsMenu_CharacterTabForeground"] = "Shared_MSM2TabForeground",
			["MSM2SuitsMenu_CharacterTabBackground"] = "Shared_MSM2TabBackground",
			["MSM2SuitsMenu_CharacterTabBorder"] = "Shared_MSM2TabBorder",
			["MSM2SuitsMenu_CharacterTabHoverBorder"] = "Shared_MSM2TabHoverBorder",
			["MSM2SuitsMenu_CharacterTabHoverForeground"] = "Shared_MSM2TabHoverForeground",
			["MSM2SuitsMenu_CharacterTabActiveForeground"] = "Shared_MSM2TabActiveForeground",
			["MSM2SuitsMenu_CharacterTabActiveBackground"] = "Shared_MSM2TabActiveBackground",
			["MSM2SuitsMenu_CharacterTabActiveBorder"] = "Shared_MSM2TabActiveBorder",
			["MSM2SuitsMenu_ListSelectionBackground"] = "Shared_MSM2Selection",
			["MSM2SuitsMenu_OuterBorder"] = "Shared_MSM2OuterBorder",
			["MSM2SuitsMenu_Background"] = "Shared_MSM2Background",
			["MSM2SuitsMenu_TabStripBackground"] = "Shared_MSM2Background",
			["MSM2SuitsMenu_ListBackground"] = "Shared_MSM2ListBackground",
			["MSM2SuitsMenu_SlotBorder"] = "Shared_MSM2SlotBorder",
			["MSM2SuitsMenu_SideBackground"] = "Shared_WindowBackground",
			["MSM2SuitsMenu_HeaderDivider"] = "Shared_MSM2Divider",
			["MSM2SuitsMenu_SpiderArmsWarningForeground"] = "Shared_MutedText",
			["MSM2SuitsMenu_FooterDivider"] = "Shared_MSM2Divider",
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

			foreach (var resourceKey in FINAL_COLOR_KEYS) {
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
