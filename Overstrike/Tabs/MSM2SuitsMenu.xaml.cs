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

		protected override ListView SuitsSlots { get => _SuitsSlots; }
		protected override Grid Modified { get => _Modified; }
		protected override Grid NotModified { get => _NotModified; }
		protected override TextBlock SuitName { get => _SuitName; }
		protected override Grid SuitInfo { get => _SuitInfo; }
		protected override Image BigIcon { get => null; }
		protected override ComboBox SuitLoadoutComboBox { get => _SuitLoadoutComboBox; }
		protected override ComboBox SuitIconComboBox { get => _SuitIconComboBox; }
		protected override ComboBox SuitBigIconComboBox { get => null; }
		protected override Button ToggleSuitDeleteButton { get => _ToggleSuitDeleteButton; }
		protected override Label NotModifiedStatusLabel { get => _NotModifiedStatusLabel; }
		protected override Button ResetButton { get => _ResetButton; }

		protected override bool HasBigIcons { get => false; }
		protected override Dictionary<string, byte> LANGUAGES { get => MSM2Suit2Installer.LANGUAGES; }

		#region state

		private MSM2Character _activeCharacter = MSM2Character.Peter;
		private readonly Dictionary<string, MSM2Character?> _suitToCharacter = new();
		private readonly Dictionary<string, MSM2Character?> _loadoutToCharacter = new();
		private readonly Dictionary<string, string> _loadoutNames = new();
		private readonly ObservableCollection<LoadoutItem> _modelChoices = new();
		private readonly ObservableCollection<LoadoutItem> _forceSpiderArmsChoices = new();
		private bool _allowCrossCharacterSuitModels;

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

		#region start

		public override void OnOpen() {
			_allowCrossCharacterSuitModels = _selectedProfile.Settings_Suit_AllowCrossCharacterSuitModels;
			base.OnOpen();
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
					LoadoutPath = loadout,
					MarkedToDelete = false
				};
				_configSuits.Add(suitInfo);
				_suitToCharacter.Add(suitId, DetermineSuitCharacter(loadout));
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

		//

		private Localization_I30? LoadLocalization() {
			if (!LANGUAGES.TryGetValue(_selectedProfile.Settings_Suit_Language, out var span)) return null;
			try {
				const ulong LOCALIZATION_AID = 0xBE55D94F171BF8DE;
				return new Localization_I30(((TOC_I29)toc).GetAssetReader(span, LOCALIZATION_AID));
			} catch {
				return null;
			}
		}

		private MSM2Character? DetermineSuitCharacter(string loadout) {
			loadout = DAT1.Utils.Normalize(loadout ?? "");
			if (string.IsNullOrEmpty(loadout)) return null;
			if (_loadoutToCharacter.TryGetValue(loadout, out var cached)) return cached;
			var result = MSM2SuitCharacterResolver.TryResolve((TOC_I29)toc, loadout);
			_loadoutToCharacter[loadout] = result;
			return result;
		}

		private void SortLoadoutPaths() {
			_loadoutsPaths.Sort((left, right) => {
				var leftName = _loadoutNames.GetValueOrDefault(left, left);
				var rightName = _loadoutNames.GetValueOrDefault(right, right);
				var comparison = StringComparer.InvariantCultureIgnoreCase.Compare(leftName, rightName);
				return comparison != 0 ? comparison : StringComparer.OrdinalIgnoreCase.Compare(left, right);
			});
		}

		private static string GetFriendlyMSM2SuitName(Localization_I30? localization, string? displayName, string suitId) {
			if (string.IsNullOrEmpty(displayName)) return suitId;

			var localized = localization?.GetValue(displayName);
			if (!string.IsNullOrEmpty(localized)) return localized;

			// Suits added by mods keep the name itself in "DisplayName", because MSM2 suit installers
			// don't write localization entries yet. Such a name is not a key of the .localization at all,
			// so show it as is instead of reporting it as a key that failed to resolve.
			if (localization != null && !localization.HasKey(displayName)) return displayName;

			return $"%{displayName}%";
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
				if (!ShouldDisplaySuit(suit.SuitId)) continue;
				if (suit.MarkedToDelete && !_showDeleted) continue;
				var displayedSuit = CloneSuit(suit);
				displayedSuit.Icon = GetIcon(suit.IconPath);
				_displayedSuits.Add(displayedSuit);
			}
			SuitsSlots.ItemsSource = _displayedSuits;
			SelectSuitWithId(selectedId);
		}

		private bool ShouldDisplaySuit(string suitId) {
			var character = _suitToCharacter.GetValueOrDefault(suitId);
			return (!character.HasValue || character.Value == _activeCharacter);
		}

		//

		protected override string GetFriendlyLoadoutName(string path) {
			var normalizedPath = DAT1.Utils.Normalize(path);
			return _loadoutNames.TryGetValue(normalizedPath, out var name)
				? name
				: base.GetFriendlyLoadoutName(path);
		}

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

		#region UI logic / helpers

		protected override void OnSuitSelected(SuitSlot data) {
			RefreshModelChoices(data);
			var msm2Suit = (MSM2SuitSlot)data;
			var targetCharacter = _suitToCharacter.GetValueOrDefault(data.SuitId);
			var showArms = targetCharacter == MSM2Character.Peter && !MSM2SpiderArmsBlockedSuits.IsBlocked(data.SuitId);
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
				var sourceCharacter = DetermineSuitCharacter(item.Path);
				if (sourceCharacter == null) continue;
				if (sourceCharacter != targetCharacter && !_allowCrossCharacterSuitModels) {
					if (item.Path == selectedSuit.LoadoutPath) {
						selected = new LoadoutItem() { Path = item.Path, Name = item.Name, IsEnabled = false };
						_modelChoices.Add(selected);
					}
					continue;
				}
				var choice = new LoadoutItem() {
					Path = item.Path,
					Name = (_allowCrossCharacterSuitModels && sourceCharacter != targetCharacter)
						? $"{item.Name} ({MSM2SuitCharacterResolver.DisplayName(sourceCharacter.Value)})"
						: item.Name
				};
				_modelChoices.Add(choice);
				if (choice.Path == selectedSuit.LoadoutPath) selected = choice;
			}
			_SuitLoadoutComboBox.ItemsSource = _modelChoices;
			_SuitLoadoutComboBox.SelectedItem = selected;
		}

		#endregion
		#region event handlers

		protected override void OnSuitLoadoutChanged(SuitSlot selectedSuit) => RefreshModelChoices(selectedSuit);

		protected override void SaveSuitChanges(SuitSlot originalSuit, SuitSlot suit, JObject changes) {
			var original = (MSM2SuitSlot)originalSuit;
			var current = (MSM2SuitSlot)suit;
			if (original.SpiderArms != current.SpiderArms) changes["force_arms"] = current.SpiderArms;
			if (original.AlwaysUnlock != current.AlwaysUnlock) changes["ignore_story_progression"] = current.AlwaysUnlock;
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

		private void PeterTabButton_Click(object sender, RoutedEventArgs e) {
			SetActiveCharacter(MSM2Character.Peter);
		}

		private void MilesTabButton_Click(object sender, RoutedEventArgs e) {
			SetActiveCharacter(MSM2Character.Miles);
		}

		private void SetActiveCharacter(MSM2Character character) {
			if (_activeCharacter == character) return;
			_activeCharacter = character;
			UpdateTabStyles();
			RefreshDisplayedSuits();
		}

		private void UpdateTabStyles() {
			var activeStyle = (Style)FindResource("CharacterTabActiveStyle");
			var inactiveStyle = (Style)FindResource("CharacterTabStyle");
			_PeterTabButton.Style = _activeCharacter == MSM2Character.Peter ? activeStyle : inactiveStyle;
			_MilesTabButton.Style = _activeCharacter == MSM2Character.Miles ? activeStyle : inactiveStyle;
		}

		#endregion
	}
}
