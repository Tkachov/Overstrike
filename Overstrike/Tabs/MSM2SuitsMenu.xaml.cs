// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using Overstrike.Installers;
using Overstrike.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Overstrike.Tabs {
	public partial class MSM2SuitsMenu : SuitsMenuBase {
		public MSM2SuitsMenu() {
			InitializeComponent();
			_ChangeSuitModelSection.Visibility = Visibility.Collapsed;
			_SuitForceSpiderArmsPanel.Visibility = Visibility.Collapsed;
			_SuitStoryProgressionCheckBox.Visibility = Visibility.Collapsed;
			UpdateModelAvailability();
			SuitsSlots.ItemContainerGenerator.StatusChanged += SuitsSlots_ItemGeneratorStatusChanged;
		}

		protected override ListView SuitsSlots { get => _SuitsSlots; }
		protected override Grid Modified { get => _Modified; }
		protected override Grid NotModified { get => _NotModified; }
		protected override TextBlock SuitName { get => _SuitName; }
		protected override Grid SuitInfo { get => _SuitInfo; }
		protected override Image BigIcon { get => null; }
		protected override ComboBox SuitLoadoutComboBox { get => _SuitLoadoutComboBox; }
		protected override bool HasForceModel { get => true; }
		protected override ComboBox SuitForceModelComboBox { get => _SuitForceModelComboBox; }
		protected override CheckBox SuitForceMaskCheckBox { get => _SuitForceMaskCheckBox; }
		protected override bool HasForceSpiderArms { get => true; }
		protected override ComboBox SuitForceSpiderArmsComboBox { get => _SuitForceSpiderArmsComboBox; }
		protected override bool HasStoryProgressionOverride { get => _enableStoryProgressionOverride; }
		protected override CheckBox SuitStoryProgressionCheckBox { get => _SuitStoryProgressionCheckBox; }
		protected override ComboBox SuitIconComboBox { get => _SuitIconComboBox; }
		protected override ComboBox SuitBigIconComboBox { get => null; }
		protected override Button ToggleSuitDeleteButton { get => _ToggleSuitDeleteButton; }
		protected override Label NotModifiedStatusLabel { get => _NotModifiedStatusLabel; }
		protected override Button ResetButton { get => _ResetButton; }

		protected override bool HasBigIcons { get => false; }
		protected override Dictionary<string, byte> LANGUAGES { get => MSM2Suit2Installer.LANGUAGES; }

		#region state

		private MSM2SuitCharacter _activeCharacter = MSM2SuitCharacter.Peter;
		private Dictionary<string, MSM2SuitCharacter?> _suitToCharacter = new();
		private Dictionary<string, MSM2SuitCharacter?> _loadoutToCharacter = new();
		private readonly Dictionary<string, string> _loadoutNames = new();
		private readonly Dictionary<string, string> _suitDisplayNames = new();
		private readonly List<LoadoutItem> _forceSpiderArmsChoices = new();
		private readonly object _localizationLock = new();
		private readonly Dictionary<byte, Localization_I30> _localizations = new();
		private readonly HashSet<byte> _unavailableLocalizations = new();
		private TOC_I29? _localizationToc;
		private int _localizationRefreshGeneration;
		private bool _allowCrossCharacterSuitModels;
		private bool _enableSpiderArms;
		private bool _enableChangeModel;
		private bool _enableStoryProgressionOverride;

		public bool AllowCrossCharacterSuitModels {
			get => _allowCrossCharacterSuitModels;
			set {
				if (_allowCrossCharacterSuitModels == value) return;
				_allowCrossCharacterSuitModels = value;
				if (SuitsSlots.SelectedItem is SuitSlot selectedSuit) {
					SuitSelected(selectedSuit);
				}
			}
		}

		public bool EnableChangeModel {
			get => _enableChangeModel;
			set {
				if (_enableChangeModel == value) return;
				_enableChangeModel = value;
				if (SuitsSlots.SelectedItem is SuitSlot selectedSuit) {
					SuitSelected(selectedSuit);
				}
			}
		}

		public bool EnableSpiderArms {
			get => _enableSpiderArms;
			set {
				if (_enableSpiderArms == value) return;
				_enableSpiderArms = value;
				UpdateSpiderArmsVisibility();
				if (SuitsSlots.SelectedItem is SuitSlot selectedSuit) {
					SuitSelected(selectedSuit);
				}
			}
		}

		private void UpdateSpiderArmsVisibility() {
			_SuitForceSpiderArmsPanel.Visibility = (_enableSpiderArms ? Visibility.Visible : Visibility.Collapsed);
			UpdateModelAvailability();
		}

		public bool EnableStoryProgressionOverride {
			get => _enableStoryProgressionOverride;
			set {
				if (_enableStoryProgressionOverride == value) return;
				_enableStoryProgressionOverride = value;
				if (SuitsSlots.SelectedItem is SuitSlot selectedSuit) {
					SuitSelected(selectedSuit);
				} else {
					_SuitStoryProgressionCheckBox.Visibility = Visibility.Collapsed;
				}
			}
		}

		private void UpdateModelAvailability() {
			var hasVisibleControls = _ChangeSuitModelSection.Visibility == Visibility.Visible
				|| _SuitForceSpiderArmsPanel.Visibility == Visibility.Visible;
			_ModelUnavailableMessage.Visibility = (hasVisibleControls ? Visibility.Collapsed : Visibility.Visible);
			_ModelUnavailableMessage.Text = (!_enableChangeModel && !_enableSpiderArms
				? "Model overrides are disabled in Settings. Saved choices are preserved."
				: "No model overrides are available for this suit.");
		}

		protected override dynamic LoadToc(string tocPath) {
			var toc = new TOC_I29();
			toc.Load(tocPath);
			return toc;
		}

		protected override dynamic LoadTexture(dynamic toc, string path) {
			try {
				return new Texture_I30(toc.GetAssetReader(path));
			} catch {
				return null;
			}
		}

		#endregion

		#region loading

		#region - thread logic

		public static JObject LoadConfig_MSM2(TOC_I29 toc) {
			try {
				const ulong SYSTEM_PROGRESSION_CONFIG_AID = 0x9C9C72A303FCFA30;
				var config = new Config_I30(toc.GetAssetReader(SYSTEM_PROGRESSION_CONFIG_AID));
				var root = config.ContentSection.Data;
				return new JObject() { ["suits"] = root["SuitList"]["Suits"] };
			} catch {}

			return null;
		}

		protected override JObject LoadConfigInternal(dynamic toc) {
			return LoadConfig_MSM2(toc);
		}

		#endregion
		#region - filling state

		protected override void LoadConfigSuits(JObject config) {
			_configSuits.Clear();
			_suitToCharacter.Clear();
			_loadoutToCharacter.Clear();
			_loadoutNames.Clear();
			_suitDisplayNames.Clear();
			var unnamedModIndex = 0;
			foreach (var suit in config["suits"]) {
				var icon = "";
				if (suit["Icon"] != null) {
					icon = (string)suit["Icon"]["AssetPath"];
				}

				if (toc.FindFirstAssetIndexByPath(icon) == -1) {
					if (suit["VariantGroup"] != null && suit["VariantGroup"]["Icon"] != null && suit["VariantGroup"]["Icon"]["AssetPath"] != null) {
						icon = (string)suit["VariantGroup"]["Icon"]["AssetPath"];
					}
				}

				var name = (string)suit["Name"];
				var displayName = (string)suit["DisplayName"];
				var loadout = (string)suit["Item"];
				_suitDisplayNames[name] = displayName;

				icon = DAT1.Utils.Normalize(icon ?? "");
				loadout = DAT1.Utils.Normalize(loadout);

				RememberIcon(icon);
				RememberLoadout(loadout);

				LoadIcon(icon);

				var suitInfo = new SuitSlot() {
					SuitId = name,
					Name = GetFriendlyMSM2SuitName(displayName, name, ref unnamedModIndex),
					Icon = null,
					BigIcon = null,
					IconPath = icon,
					BigIconPath = null,
					LoadoutPath = loadout,
					MarkedToDelete = false
				};
				_configSuits.Add(suitInfo);
				_suitToCharacter.Add(name, TryDetermineSuitCharacter(loadout));
				if (!string.IsNullOrEmpty(loadout) && !_loadoutNames.ContainsKey(loadout)) {
					_loadoutNames.Add(loadout, suitInfo.Name);
				}
			}

			SortLoadoutPaths();
		}

		private void SortLoadoutPaths() {
			_loadoutsPaths.Sort((left, right) => {
				var leftName = (_loadoutNames.TryGetValue(left, out var leftFriendlyName) ? leftFriendlyName : left);
				var rightName = (_loadoutNames.TryGetValue(right, out var rightFriendlyName) ? rightFriendlyName : right);
				var comparison = StringComparer.InvariantCultureIgnoreCase.Compare(leftName, rightName);
				return (comparison != 0 ? comparison : StringComparer.OrdinalIgnoreCase.Compare(left, right));
			});
		}

		// GetLocalization() blocks on a TOC asset read the first time a given language is used.
		// Warming it up on a background thread first keeps that first-use read off the UI thread;
		// every call after the warm-up hits the in-memory cache, so RefreshLocalizedNames() itself
		// stays cheap and can run on the UI thread as usual.
		public void RefreshLocalizedNamesAsync() {
			if (!_loaded) return;

			var profile = _selectedProfile;
			var currentToc = (TOC_I29)toc;
			var language = _selectedProfile.Settings_Suit_Language;
			var generation = ++_localizationRefreshGeneration;
			Task.Run(() => {
				GetLocalization(currentToc, language);
				if (language != "us") GetLocalization(currentToc, "us");
			}).ContinueWith(_ => {
				Dispatcher.Invoke(() => {
					if (!_loaded || generation != _localizationRefreshGeneration) return;
					if (!ReferenceEquals(_selectedProfile, profile) || !ReferenceEquals((TOC_I29)toc, currentToc)) return;
					RefreshLocalizedNames();
				});
			});
		}

		public void RefreshLocalizedNames() {
			if (!_loaded) return;

			var unnamedModIndex = 0;
			var names = new Dictionary<string, string>(StringComparer.Ordinal);
			_loadoutNames.Clear();
			foreach (var suit in _configSuits) {
				_suitDisplayNames.TryGetValue(suit.SuitId, out var displayName);
				suit.Name = GetFriendlyMSM2SuitName(displayName, suit.SuitId, ref unnamedModIndex);
				names[suit.SuitId] = suit.Name;
				if (!string.IsNullOrEmpty(suit.LoadoutPath) && !_loadoutNames.ContainsKey(suit.LoadoutPath)) {
					_loadoutNames.Add(suit.LoadoutPath, suit.Name);
				}
			}

			foreach (var suit in _customizedSuits) {
				if (names.TryGetValue(suit.SuitId, out var name)) {
					suit.Name = name;
				}
			}

			SortLoadoutPaths();
			MakeLoadouts();
			RefreshDisplayedSuits();
		}

		private Localization_I30? GetLocalization(string? language) {
			return GetLocalization((TOC_I29)toc, language);
		}

		private Localization_I30? GetLocalization(TOC_I29 currentToc, string? language) {
			if (!LANGUAGES.TryGetValue(language ?? "", out var span)) return null;

			lock (_localizationLock) {
				if (!ReferenceEquals(_localizationToc, currentToc)) {
					_localizations.Clear();
					_unavailableLocalizations.Clear();
					_localizationToc = currentToc;
				}

				if (_localizations.TryGetValue(span, out var localization)) return localization;
				if (_unavailableLocalizations.Contains(span)) return null;

				try {
					const ulong LOCALIZATION_AID = 0xBE55D94F171BF8DE; // localization/localization_all.localization
					localization = new Localization_I30(currentToc.GetAssetReader(span, LOCALIZATION_AID));
					_localizations[span] = localization;
					return localization;
				} catch {
					_unavailableLocalizations.Add(span);
					return null;
				}
			}
		}

		private string GetFriendlyMSM2SuitName(string? displayName, string? suitId, ref int unnamedModIndex) {
			if (!string.IsNullOrWhiteSpace(displayName)) {
				var language = _selectedProfile.Settings_Suit_Language;
				var localized = GetLocalization(language)?.GetValue(displayName);

				// English is the authoritative fallback when a selected language does not contain a key.
				if (string.IsNullOrWhiteSpace(localized) && language != "us") {
					localized = GetLocalization("us")?.GetValue(displayName);
				}

				if (!string.IsNullOrWhiteSpace(localized)) return NormalizeDisplayNameCapitalization(localized);
				if (LooksLikeLocalizationKey(displayName)) {
					var readableKey = MakeLocalizationKeyReadable(displayName);
					if (!string.IsNullOrWhiteSpace(readableKey)) return readableKey;
				} else {
					var literalName = displayName.Trim();
					var readableName = (LooksLikeIdentifier(literalName) ? MakeIdentifierReadable(literalName) : NormalizeDisplayNameCapitalization(literalName));
					if (!string.IsNullOrWhiteSpace(readableName)) return readableName;
				}
			}

			if (!string.IsNullOrWhiteSpace(suitId)) {
				var normalizedId = suitId.Trim();
				var readableId = MakeSuitIdReadable(normalizedId);
				if (!string.IsNullOrWhiteSpace(readableId) && readableId != normalizedId) return readableId;
				return normalizedId;
			}

			unnamedModIndex++;
			return $"Modded Suit {unnamedModIndex}";
		}

		private static bool LooksLikeLocalizationKey(string value) {
			return value.StartsWith("SUIT_", StringComparison.Ordinal)
				&& value.Contains("_TITLE", StringComparison.Ordinal);
		}

		private static string MakeLocalizationKeyReadable(string key) {
			var value = Regex.Replace(key, "^SUIT_|_TITLE(?:_I30)?$", "");
			return MakeIdentifierReadable(value);
		}

		private static bool LooksLikeIdentifier(string value) {
			return !value.Contains(' ') && (value.Contains('_') || value.Contains('-'));
		}

		private static string MakeIdentifierReadable(string value) {
			value = Regex.Replace(value, "(?<=\\d)_(?=\\d)", ".");
			value = Regex.Replace(value, "(?<=[a-z0-9])(?=[A-Z])", " ");
			value = value.Replace('_', ' ').Replace('-', ' ').Trim();
			return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
		}

		private static string NormalizeDisplayNameCapitalization(string value) {
			var trimmed = value.Trim();
			var hasLetter = false;
			var hasLowercaseLetter = false;
			foreach (var character in trimmed) {
				if (!char.IsLetter(character)) continue;
				hasLetter = true;
				if (char.IsLower(character)) {
					hasLowercaseLetter = true;
					break;
				}
			}

			// Localization values that already use mixed case are treated as authoritative.
			if (!hasLetter || hasLowercaseLetter) return trimmed;

			var result = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
			result = Regex.Replace(result, "\\b(?:Ii|Iii|Iv|V|Vi|Vii|Viii|Ix|X)\\b", match => match.Value.ToUpperInvariant());
			return Regex.Replace(result, "(?<!^)\\b(?:And|Or|The|Of|In|On|At|To|For)\\b", match => match.Value.ToLowerInvariant());
		}

		private static string MakeSuitIdReadable(string suitId) {
			var value = Regex.Replace(suitId, "^SUIT_", "");
			return MakeIdentifierReadable(value);
		}

		//

		// The base implementation reads loadouts as I20-format Config, which isn't the format MSM2
		// actually uses -- it always fails silently and falls back to a raw-filename-derived name
		// (e.g. "ultimate6160" instead of "Advanced Suit 2.0"'s own internal name). Read the reward
		// loadout's own "Name" field with the correct Config_I30 instead.
		protected override string GetFriendlyLoadoutName(string path) {
			var normalizedPath = DAT1.Utils.Normalize(path);
			if (!string.IsNullOrEmpty(normalizedPath) && _loadoutNames.TryGetValue(normalizedPath, out var suitName)) {
				return suitName;
			}

			try {
				var config = new Config_I30(toc.GetAssetReader(path));
				var name = (string)config.ContentSection.Data["Name"];
				if (!string.IsNullOrEmpty(name)) return (LooksLikeIdentifier(name) ? MakeIdentifierReadable(name) : NormalizeDisplayNameCapitalization(name));
			} catch {}

			return base.GetFriendlyLoadoutName(path);
		}

		private MSM2SuitCharacter? TryDetermineSuitCharacter(string loadout) {
			if (string.IsNullOrEmpty(loadout)) return null;
			loadout = DAT1.Utils.Normalize(loadout);
			if (string.IsNullOrEmpty(loadout)) return null;
			if (_loadoutToCharacter.TryGetValue(loadout, out var cached)) return cached;

			var result = MSM2SuitCharacterResolver.TryResolve((TOC_I29)toc, loadout);
			_loadoutToCharacter[loadout] = result;
			return result;
		}

		private bool ShouldDisplaySuit(MSM2SuitCharacter? character) => !character.HasValue || character.Value == _activeCharacter;

		protected override void RefreshForceModelChoices(SuitSlot selectedSuit) {
			_forceModelChoices.Clear();
			_forceModelChoices.Add(new LoadoutItem() { Path = null, Name = "None" });

			var isEligibleSlot = MSM2CutsceneSuits.IsEligible(selectedSuit.SuitId);
			_ChangeSuitModelSection.Visibility = (_enableChangeModel && isEligibleSlot ? Visibility.Visible : Visibility.Collapsed);
			UpdateModelAvailability();
			if (!_enableChangeModel || !isEligibleSlot) {
				SuitForceModelComboBox.ItemsSource = _forceModelChoices;
				return;
			}

			var targetCharacter = TryDetermineSuitCharacter(selectedSuit.LoadoutPath);
			foreach (var item in _loadouts) {
				var sourceCharacter = TryDetermineSuitCharacter(item.Path);
				if (targetCharacter != null && sourceCharacter != null && (sourceCharacter == targetCharacter || AllowCrossCharacterSuitModels)) {
					var displayName = item.Name;
					if (AllowCrossCharacterSuitModels && sourceCharacter.Value != _activeCharacter) {
						displayName += $" ({MSM2SuitCharacterResolver.DisplayName(sourceCharacter.Value)})";
					}
					_forceModelChoices.Add(new LoadoutItem() { Path = item.Path, Name = displayName });
				} else if (sourceCharacter == null) {
					// Do not silently hide third-party loadouts that omit ValidCharacters.
					// They remain visible but cannot be selected because the installer cannot
					// prove that they are safe for the current character.
					_forceModelChoices.Add(new LoadoutItem() {
						Path = item.Path,
						Name = $"{item.Name} (unavailable: character not declared)",
						IsEnabled = false
					});
				} else if (item.Path == selectedSuit.ForceModelPath) {
					// Retain an old incompatible saved choice as a disabled, explained item
					// instead of presenting an empty combobox while keeping its value hidden.
					_forceModelChoices.Add(new LoadoutItem() {
						Path = item.Path,
						Name = $"{item.Name} (unavailable: different character)",
						IsEnabled = false
					});
				}
			}

			SuitForceModelComboBox.ItemsSource = _forceModelChoices;
		}

		protected override void RefreshForceSpiderArmsChoices(SuitSlot selectedSuit) {
			_forceSpiderArmsChoices.Clear();
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = null, Name = "Keep this suit's Spider-Arms" });

			var isBlockedSuit = MSM2SpiderArmsBlockedSuits.IsBlocked(selectedSuit.SuitId);
			var showRageWarning = ShouldShowSpiderArmsRageWarning(selectedSuit.SuitId);
			var character = TryDetermineSuitCharacter(selectedSuit.LoadoutPath);
			var isPeter = character == MSM2SuitCharacter.Peter;
			_SuitForceSpiderArmsRageWarning.Visibility = (showRageWarning ? Visibility.Visible : Visibility.Collapsed);

			// Miles never uses this system at all, so there is nothing to show him -- not even a
			// disabled placeholder. Anti-Venom is hidden separately because the option conflicts
			// with its suit power (see MSM2SpiderArmsBlockedSuits).
			_SuitForceSpiderArmsPanel.Visibility = (_enableSpiderArms && isPeter && !isBlockedSuit ? Visibility.Visible : Visibility.Collapsed);
			UpdateModelAvailability();
			if (!isPeter || isBlockedSuit) {
				SuitForceSpiderArmsComboBox.ItemsSource = _forceSpiderArmsChoices;
				return;
			}

			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = "hero_spiderman_advanced_legs", Name = "Advanced Suit 2.0" });
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = "hero_spiderman_momoko_legs", Name = "Kumo Suit" });
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = "hero_spiderman_superior_legs", Name = "Superior Spider-Man Suit" });
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = "hero_spiderman_itsvnoir_legs", Name = "Into the Spider-Verse Noir Suit" });
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = "hero_spiderman_ironspider_legs", Name = "Iron Spider Armor" });
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = "hero_spiderman_iw_legs", Name = "Iron Spider Suit" });

			SuitForceSpiderArmsComboBox.ItemsSource = _forceSpiderArmsChoices;
			SuitForceSpiderArmsComboBox.IsEnabled = _enableSpiderArms;
		}

		private static bool ShouldShowSpiderArmsRageWarning(string suitSlotName) => suitSlotName == "SUIT_SYMBIOTE";

		protected override void OnSuitSelected(SuitSlot data) {
			_SuitStoryProgressionCheckBox.Visibility = (_enableStoryProgressionOverride && MSM2CutsceneSuits.IsEligible(data.SuitId)
				? Visibility.Visible
				: Visibility.Collapsed);
		}

		#endregion
		#region - making observable items

		protected override void MakeDisplayedSuits() {
			RefreshDisplayedSuits();
		}

		private void RefreshDisplayedSuits() {
			var selectedId = GetCurrentlySelectedSuitId();

			_displayedSuits.Clear();
			foreach (var suit in _customizedSuits) {
				_suitToCharacter.TryGetValue(suit.SuitId, out var character);
				if (!ShouldDisplaySuit(character)) continue;
				if (suit.MarkedToDelete && !_showDeleted) continue;

				_displayedSuits.Add(new SuitSlot() {
					SuitId = suit.SuitId,
					Name = suit.Name,
					Icon = GetIcon(suit.IconPath),
					BigIcon = null,
					IconPath = suit.IconPath,
					BigIconPath = null,
					LoadoutPath = suit.LoadoutPath,
					ForceModelPath = suit.ForceModelPath,
					ForceSuitMask = suit.ForceSuitMask,
					ForceSpiderArms = suit.ForceSpiderArms,
					IgnoreStoryProgression = suit.IgnoreStoryProgression,
					MarkedToDelete = suit.MarkedToDelete
				});
			}

			SuitsSlots.ItemsSource = _displayedSuits;
			SelectSuitWithId(selectedId);
		}

		//

		protected override BitmapSource GetIcon(string path) {
			if (_iconsOrigs.ContainsKey(path) && _iconsOrigs[path] != null && (!_icons.ContainsKey(path) || _icons[path] == null))
				_icons[path] = Utils.Imaging.ConvertToBitmapImage(_iconsOrigs[path]);

			if (_icons.ContainsKey(path) && _icons[path] != null)
				return _icons[path];

			if (_placeholderImage == null)
				_placeholderImage = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.suit_missing_msm2);

			return _placeholderImage;
		}

		#endregion

		#endregion

		#region event handlers

		private void PeterTabButton_Click(object sender, RoutedEventArgs e) {
			SetActiveCharacter(MSM2SuitCharacter.Peter);
		}

		private void MilesTabButton_Click(object sender, RoutedEventArgs e) {
			SetActiveCharacter(MSM2SuitCharacter.Miles);
		}

		private void SetActiveCharacter(MSM2SuitCharacter character) {
			if (_activeCharacter == character) return;
			_activeCharacter = character;
			UpdateTabStyles();
			RefreshDisplayedSuits();
		}

		private void UpdateTabStyles() {
			var activeStyle = (Style)FindResource("CharacterTabActiveStyle");
			var inactiveStyle = (Style)FindResource("CharacterTabStyle");
			_PeterTabButton.Style = (_activeCharacter == MSM2SuitCharacter.Peter ? activeStyle : inactiveStyle);
			_MilesTabButton.Style = (_activeCharacter == MSM2SuitCharacter.Miles ? activeStyle : inactiveStyle);
		}

		#endregion
	}
}
