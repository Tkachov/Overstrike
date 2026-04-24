// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using Overstrike.Installers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Overstrike.Tabs
{
	public enum MSM2Character
	{
		Peter,
		Miles
	}

	public partial class MSM2SuitsMenu : SuitsMenuBase
	{
		private MSM2Character _activeCharacter = MSM2Character.Peter;
		private Dictionary<string, MSM2Character> _suitToCharacter = new();

		public MSM2SuitsMenu()
		{
			InitializeComponent();
			SuitsSlots.ItemContainerGenerator.StatusChanged += SuitsSlots_ItemGeneratorStatusChanged;
		}

		protected override ListView SuitsSlots { get => _SuitsSlots; }
		protected override Grid Modified { get => _Modified; }
		protected override Grid NotModified { get => _NotModified; }
		protected override TextBlock SuitName { get => _SuitName; }
		protected override Grid SuitInfo { get => _SuitInfo; }
		protected override System.Windows.Controls.Image BigIcon { get => null; }
		protected override ComboBox SuitLoadoutComboBox { get => _SuitLoadoutComboBox; }
		protected override ComboBox SuitIconComboBox { get => _SuitIconComboBox; }
		protected override ComboBox SuitBigIconComboBox { get => null; }
		protected override Button ToggleSuitDeleteButton { get => _ToggleSuitDeleteButton; }
		protected override Label NotModifiedStatusLabel { get => _NotModifiedStatusLabel; }
		protected override Button ResetButton { get => _ResetButton; }

		protected override bool HasBigIcons { get => false; }
		protected override Dictionary<string, byte> LANGUAGES { get => MSM2Suit2Installer.LANGUAGES; }

		// Use TOC_I29 instead of TOC_I20
		protected override dynamic LoadToc(string tocPath)
		{
			var toc = new TOC_I29();
			toc.Load(tocPath);
			return toc;
		}

		// Use Texture_I30 instead of Texture_I20
		protected override dynamic LoadTexture(dynamic toc, string path)
		{
			try
			{
				return new Texture_I30(toc.GetAssetReader(path));
			}
			catch
			{
				return null;
			}
		}

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

		protected override void LoadConfigSuits(JObject config) {
			_configSuits.Clear();
			_suitToCharacter.Clear();
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

				icon = DAT1.Utils.Normalize(icon ?? "");
				loadout = DAT1.Utils.Normalize(loadout);

				RememberIcon(icon);
				RememberLoadout(loadout);

				LoadIcon(icon);

				var suitInfo = new SuitSlot() {
					SuitId = name,
					Name = GetFriendlySuitName(displayName),
					Icon = null,
					BigIcon = null,
					IconPath = icon,
					BigIconPath = null,
					LoadoutPath = loadout,
					MarkedToDelete = false
				};
				_configSuits.Add(suitInfo);
				_suitToCharacter.Add(name, DetermineSuitCharacter(loadout));
			}
		}

		private MSM2Character DetermineSuitCharacter(string loadout) {
			try {
				var config = new Config_I30(toc.GetAssetReader(loadout));
				var root = config.ContentSection.Data;
				return ((string)root["ValidCharacters"][0] == "kSpiderManPeter" ? MSM2Character.Peter : MSM2Character.Miles);
			} catch {}

			return MSM2Character.Peter;
		}

		// Character tab handlers

		private void PeterTabButton_Click(object sender, RoutedEventArgs e)
		{
			SetActiveCharacter(MSM2Character.Peter);
		}

		private void MilesTabButton_Click(object sender, RoutedEventArgs e)
		{
			SetActiveCharacter(MSM2Character.Miles);
		}

		private void SetActiveCharacter(MSM2Character character)
		{
			if (_activeCharacter == character) return;
			_activeCharacter = character;
			UpdateTabStyles();
			RefreshDisplayedSuits();
		}
		protected override void MakeDisplayedSuits()
		{
			RefreshDisplayedSuits();
		}

		private void UpdateTabStyles()
		{
			var activeStyle = (Style)FindResource("CharacterTabActiveStyle");
			var inactiveStyle = (Style)FindResource("CharacterTabStyle");
			_PeterTabButton.Style = _activeCharacter == MSM2Character.Peter ? activeStyle : inactiveStyle;
			_MilesTabButton.Style = _activeCharacter == MSM2Character.Miles ? activeStyle : inactiveStyle;
		}

		private void RefreshDisplayedSuits()
		{
			var selectedId = GetCurrentlySelectedSuitId();

			_displayedSuits.Clear();
			foreach (var suit in _customizedSuits)
			{
				if (_suitToCharacter[suit.SuitId] != _activeCharacter) continue;
				if (suit.MarkedToDelete && !_showDeleted) continue;

				_displayedSuits.Add(new SuitSlot()
				{
					SuitId = suit.SuitId,
					Name = suit.Name,
					Icon = GetIcon(suit.IconPath),
					BigIcon = null,
					IconPath = suit.IconPath,
					BigIconPath = null,
					LoadoutPath = suit.LoadoutPath,
					MarkedToDelete = suit.MarkedToDelete
				});
			}

			SuitsSlots.ItemsSource = _displayedSuits;
			SelectSuitWithId(selectedId);
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

	}
}
