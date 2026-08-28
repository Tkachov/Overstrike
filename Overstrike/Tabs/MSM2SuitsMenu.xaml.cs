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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Overstrike.Tabs {
	public partial class MSM2SuitsMenu : SuitsMenuBase {
		private sealed class MSM2SuitSlot : SuitSlot {
			public string? SpiderArms { get; set; }
			public bool AlwaysUnlock { get; set; }
		}

		public MSM2SuitsMenu() {
			InitializeComponent();
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = null, Name = "Keep this suit's Spider-Arms" });
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = "hero_spiderman_advanced_legs", Name = "Advanced Suit 2.0" });
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = "hero_spiderman_momoko_legs", Name = "Kumo Suit" });
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = "hero_spiderman_superior_legs", Name = "Superior Spider-Man Suit" });
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = "hero_spiderman_itsvnoir_legs", Name = "Into the Spider-Verse Noir Suit" });
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = "hero_spiderman_ironspider_legs", Name = "Iron Spider Armor" });
			_forceSpiderArmsChoices.Add(new LoadoutItem() { Path = "hero_spiderman_iw_legs", Name = "Iron Spider Suit" });
			_SuitSpiderArmsComboBox.ItemsSource = _forceSpiderArmsChoices;
			SuitsSlots.ItemContainerGenerator.StatusChanged += SuitsSlots_ItemGeneratorStatusChanged;
		}

		protected override ListView SuitsSlots => _SuitsSlots;
		protected override Grid Modified => _Modified;
		protected override Grid NotModified => _NotModified;
		protected override TextBlock SuitName => _SuitName;
		protected override Grid SuitInfo => _SuitInfo;
		protected override Image BigIcon => null;
		protected override ComboBox SuitLoadoutComboBox => _SuitLoadoutComboBox;
		protected override ComboBox SuitIconComboBox => _SuitIconComboBox;
		protected override ComboBox SuitBigIconComboBox => null;
		protected override Button ToggleSuitDeleteButton => _ToggleSuitDeleteButton;
		protected override Label NotModifiedStatusLabel => _NotModifiedStatusLabel;
		protected override Button ResetButton => _ResetButton;
		protected override bool HasBigIcons => false;
		protected override Dictionary<string, byte> LANGUAGES => MSM2Suit2Installer.LANGUAGES;

		private MSM2SuitCharacter _activeCharacter = MSM2SuitCharacter.Peter;
		private readonly Dictionary<string, MSM2SuitCharacter?> _suitToCharacter = new();
		private readonly Dictionary<string, MSM2SuitCharacter?> _loadoutToCharacter = new();
		private readonly Dictionary<string, string> _loadoutNames = new();
		private readonly ObservableCollection<LoadoutItem> _modelChoices = new();
		private readonly ObservableCollection<LoadoutItem> _forceSpiderArmsChoices = new();
		private bool _allowCrossCharacterSuitModels;

		public bool AllowCrossCharacterSuitModels {
			get => _allowCrossCharacterSuitModels;
			set {
				if (_allowCrossCharacterSuitModels == value) return;
				_allowCrossCharacterSuitModels = value;
				if (SuitsSlots.SelectedItem is SuitSlot selectedSuit) SuitSelected(selectedSuit);
			}
		}

		public override void OnOpen() {
			_allowCrossCharacterSuitModels = _selectedProfile.Settings_SuitMenu_AllowCrossCharacterSuitModels;
			base.OnOpen();
		}

		protected override dynamic LoadToc(string tocPath) {
			var result = new TOC_I29();
			result.Load(tocPath);
			return result;
		}

		protected override dynamic LoadTexture(dynamic currentToc, string path) {
			try {
				return new Texture_I30(currentToc.GetAssetReader(path));
			} catch {
				return null;
			}
		}

		public static JObject LoadConfig_MSM2(TOC_I29 currentToc) {
			try {
				const ulong SYSTEM_PROGRESSION_CONFIG_AID = 0x9C9C72A303FCFA30;
				var config = new Config_I30(currentToc.GetAssetReader(SYSTEM_PROGRESSION_CONFIG_AID));
				return new JObject() { ["suits"] = config.ContentSection.Data["SuitList"]["Suits"] };
			} catch {
				return null;
			}
		}

		protected override JObject LoadConfigInternal(dynamic currentToc) => LoadConfig_MSM2(currentToc);

		protected override void LoadConfigSuits(JObject config) {
			_configSuits.Clear();
			_suitToCharacter.Clear();
			_loadoutToCharacter.Clear();
			_loadoutNames.Clear();
			var localization = LoadLocalization();

			foreach (var suit in config["suits"]) {
				var icon = (string?)suit["Icon"]?["AssetPath"] ?? "";
				if (toc.FindFirstAssetIndexByPath(icon) == -1) {
					icon = (string?)suit["VariantGroup"]?["Icon"]?["AssetPath"] ?? icon;
				}

				var suitId = (string)suit["Name"];
				var displayName = (string?)suit["DisplayName"];
				var loadout = DAT1.Utils.Normalize((string?)suit["Item"] ?? "");
				icon = DAT1.Utils.Normalize(icon);
				RememberIcon(icon);
				RememberLoadout(loadout);
				LoadIcon(icon);

				var suitInfo = new MSM2SuitSlot() {
					SuitId = suitId,
					Name = GetFriendlyMSM2SuitName(localization, displayName, suitId),
					IconPath = icon,
					BigIconPath = null,
					LoadoutPath = loadout
				};
				_configSuits.Add(suitInfo);
				_suitToCharacter[suitId] = TryDetermineSuitCharacter(loadout);
				if (!string.IsNullOrEmpty(loadout)) _loadoutNames.TryAdd(loadout, suitInfo.Name);
			}

			SortLoadoutPaths();
		}

		protected override SuitSlot CloneSuit(SuitSlot suit) {
			var msm2Suit = suit as MSM2SuitSlot;
			return new MSM2SuitSlot() {
				SuitId = suit.SuitId,
				Name = suit.Name,
				Icon = suit.Icon,
				BigIcon = suit.BigIcon,
				IconPath = suit.IconPath,
				BigIconPath = suit.BigIconPath,
				LoadoutPath = suit.LoadoutPath,
				MarkedToDelete = suit.MarkedToDelete,
				SpiderArms = msm2Suit?.SpiderArms,
				AlwaysUnlock = msm2Suit?.AlwaysUnlock ?? false
			};
		}

		protected override void LoadSuitChanges(SuitSlot suit, JObject changes) {
			var msm2Suit = (MSM2SuitSlot)suit;
			msm2Suit.SpiderArms = (string?)changes["force_arms"];
			msm2Suit.AlwaysUnlock = (bool?)changes["ignore_story_progression"] == true;
		}

		protected override void SaveSuitChanges(SuitSlot originalSuit, SuitSlot suit, JObject changes) {
			var original = (MSM2SuitSlot)originalSuit;
			var current = (MSM2SuitSlot)suit;
			if (original.SpiderArms != current.SpiderArms) changes["force_arms"] = current.SpiderArms;
			if (original.AlwaysUnlock != current.AlwaysUnlock) changes["ignore_story_progression"] = current.AlwaysUnlock;
		}

		private Localization_I30? LoadLocalization() {
			if (!LANGUAGES.TryGetValue(_selectedProfile.Settings_Suit_Language, out var span)) return null;
			try {
				const ulong LOCALIZATION_AID = 0xBE55D94F171BF8DE;
				return new Localization_I30(((TOC_I29)toc).GetAssetReader(span, LOCALIZATION_AID));
			} catch {
				return null;
			}
		}

		private static string GetFriendlyMSM2SuitName(Localization_I30? localization, string? displayName, string suitId) {
			if (!string.IsNullOrEmpty(displayName)) {
				var localized = localization?.GetValue(displayName);
				if (!string.IsNullOrEmpty(localized)) return localized;
				return $"%{displayName}%";
			}
			return suitId;
		}

		private void SortLoadoutPaths() {
			_loadoutsPaths.Sort((left, right) => {
				var leftName = _loadoutNames.GetValueOrDefault(left, left);
				var rightName = _loadoutNames.GetValueOrDefault(right, right);
				var comparison = StringComparer.InvariantCultureIgnoreCase.Compare(leftName, rightName);
				return comparison != 0 ? comparison : StringComparer.OrdinalIgnoreCase.Compare(left, right);
			});
		}

		protected override string GetFriendlyLoadoutName(string path) {
			var normalizedPath = DAT1.Utils.Normalize(path);
			return _loadoutNames.GetValueOrDefault(normalizedPath, base.GetFriendlyLoadoutName(path));
		}

		private MSM2SuitCharacter? TryDetermineSuitCharacter(string loadout) {
			loadout = DAT1.Utils.Normalize(loadout ?? "");
			if (string.IsNullOrEmpty(loadout)) return null;
			if (_loadoutToCharacter.TryGetValue(loadout, out var cached)) return cached;
			var result = MSM2SuitCharacterResolver.TryResolve((TOC_I29)toc, loadout);
			_loadoutToCharacter[loadout] = result;
			return result;
		}

		protected override void OnSuitSelected(SuitSlot data) {
			RefreshModelChoices(data);
			var msm2Suit = (MSM2SuitSlot)data;
			var targetCharacter = _suitToCharacter.GetValueOrDefault(data.SuitId);
			var showArms = targetCharacter == MSM2SuitCharacter.Peter && !MSM2SpiderArmsBlockedSuits.IsBlocked(data.SuitId);
			_SuitSpiderArmsPanel.Visibility = showArms ? Visibility.Visible : Visibility.Collapsed;
			_SuitSpiderArmsRageWarning.Visibility = data.SuitId == "SUIT_SYMBIOTE" ? Visibility.Visible : Visibility.Collapsed;
			_SuitAlwaysUnlockCheckBox.Visibility = MSM2CutsceneSuits.IsEligible(data.SuitId) ? Visibility.Visible : Visibility.Collapsed;
			_SuitAlwaysUnlockCheckBox.IsChecked = msm2Suit.AlwaysUnlock;

			LoadoutItem? arms = null;
			foreach (var item in _forceSpiderArmsChoices) {
				if (item.Path == msm2Suit.SpiderArms) arms = item;
			}
			_SuitSpiderArmsComboBox.SelectedItem = arms;
		}

		private void RefreshModelChoices(SuitSlot selectedSuit) {
			_modelChoices.Clear();
			var targetCharacter = _suitToCharacter.GetValueOrDefault(selectedSuit.SuitId);
			LoadoutItem? selected = null;
			foreach (var item in _loadouts) {
				var sourceCharacter = TryDetermineSuitCharacter(item.Path);
				if (sourceCharacter == null) continue;
				if (sourceCharacter != targetCharacter && !AllowCrossCharacterSuitModels) {
					if (item.Path == selectedSuit.LoadoutPath) {
						selected = new LoadoutItem() { Path = item.Path, Name = item.Name, IsEnabled = false };
						_modelChoices.Add(selected);
					}
					continue;
				}
				var choice = new LoadoutItem() {
					Path = item.Path,
					Name = (AllowCrossCharacterSuitModels && sourceCharacter != targetCharacter)
						? $"{item.Name} ({MSM2SuitCharacterResolver.DisplayName(sourceCharacter.Value)})"
						: item.Name
				};
				_modelChoices.Add(choice);
				if (choice.Path == selectedSuit.LoadoutPath) selected = choice;
			}
			_SuitLoadoutComboBox.ItemsSource = _modelChoices;
			_SuitLoadoutComboBox.SelectedItem = selected;
		}

		protected override void OnSuitLoadoutChanged(SuitSlot selectedSuit) => RefreshModelChoices(selectedSuit);

		protected override void MakeDisplayedSuits() => RefreshDisplayedSuits();

		private bool ShouldDisplaySuit(MSM2SuitCharacter? character) => !character.HasValue || character.Value == _activeCharacter;

		private void RefreshDisplayedSuits() {
			var selectedId = GetCurrentlySelectedSuitId();
			_displayedSuits.Clear();
			foreach (var suit in _customizedSuits) {
				if (!ShouldDisplaySuit(_suitToCharacter.GetValueOrDefault(suit.SuitId))) continue;
				if (suit.MarkedToDelete && !_showDeleted) continue;
				var displayedSuit = CloneSuit(suit);
				displayedSuit.Icon = GetIcon(suit.IconPath);
				_displayedSuits.Add(displayedSuit);
			}
			SuitsSlots.ItemsSource = _displayedSuits;
			SelectSuitWithId(selectedId);
		}

		protected override BitmapSource GetIcon(string path) {
			if (_iconsOrigs.ContainsKey(path) && _iconsOrigs[path] != null && (!_icons.ContainsKey(path) || _icons[path] == null)) {
				_icons[path] = Utils.Imaging.ConvertToBitmapImage(_iconsOrigs[path]);
			}
			if (_icons.ContainsKey(path) && _icons[path] != null) return _icons[path];
			_placeholderImage ??= Utils.Imaging.ConvertToBitmapImage(Properties.Resources.suit_missing_msm2);
			return _placeholderImage;
		}

		private void SuitSpiderArmsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e.AddedItems.Count == 0 || SuitsSlots.SelectedItem is not MSM2SuitSlot selectedSuit) return;
			var choice = (LoadoutItem)e.AddedItems[0];
			if (selectedSuit.SpiderArms == choice.Path) return;
			_hasChanges = true;
			SetWasReset(false);
			selectedSuit.SpiderArms = choice.Path;
			foreach (var suit in _customizedSuits) {
				if (suit.SuitId == selectedSuit.SuitId) ((MSM2SuitSlot)suit).SpiderArms = choice.Path;
			}
		}

		private void SuitAlwaysUnlockCheckBox_Changed(object sender, RoutedEventArgs e) {
			if (SuitsSlots.SelectedItem is not MSM2SuitSlot selectedSuit) return;
			var value = _SuitAlwaysUnlockCheckBox.IsChecked == true;
			if (selectedSuit.AlwaysUnlock == value) return;
			_hasChanges = true;
			SetWasReset(false);
			selectedSuit.AlwaysUnlock = value;
			foreach (var suit in _customizedSuits) {
				if (suit.SuitId == selectedSuit.SuitId) ((MSM2SuitSlot)suit).AlwaysUnlock = value;
			}
		}

		private void PeterTabButton_Click(object sender, RoutedEventArgs e) => SetActiveCharacter(MSM2SuitCharacter.Peter);
		private void MilesTabButton_Click(object sender, RoutedEventArgs e) => SetActiveCharacter(MSM2SuitCharacter.Miles);

		private void SetActiveCharacter(MSM2SuitCharacter character) {
			if (_activeCharacter == character) return;
			_activeCharacter = character;
			_PeterTabButton.Style = (Style)FindResource(character == MSM2SuitCharacter.Peter ? "CharacterTabActiveStyle" : "CharacterTabStyle");
			_MilesTabButton.Style = (Style)FindResource(character == MSM2SuitCharacter.Miles ? "CharacterTabActiveStyle" : "CharacterTabStyle");
			RefreshDisplayedSuits();
		}
	}
}
