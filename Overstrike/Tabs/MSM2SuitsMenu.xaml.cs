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

		// MSM2 uses SuitList.Suits instead of TechWebLists
		protected override JObject LoadConfigInternal(dynamic toc)
		{
			try
			{
				const ulong SYSTEM_PROGRESSION_CONFIG_AID = 0x9C9C72A303FCFA30;
				var config = new Config_I30(toc.GetAssetReader((byte)0, SYSTEM_PROGRESSION_CONFIG_AID));
				var root = config.ContentSection.Data;

				var suits = (JArray)root["SuitList"]["Suits"];
				var normalized = new JArray();
				foreach (var suit in suits)
				{
					normalized.Add(new JObject()
					{
						["Name"] = suit["Name"],
						["DisplayName"] = suit["DisplayName"],
						["PreviewImage"] = suit["Icon"]?["AssetPath"] ?? "",
						["GivesItems"] = suit["Item"] != null
							? new JObject() { ["Item"] = suit["Item"] }
							: null
					});
				}

				return new JObject() { ["suits"] = normalized };
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "LoadConfigInternal failed");
			}

			return null;
		}

		// Called by MetaInstaller_I29 after mods install to cache config
		public static JObject LoadConfigForCache(TOC_I29 toc)
		{
			try
			{
				const ulong SYSTEM_PROGRESSION_CONFIG_AID = 0x9C9C72A303FCFA30;
				var config = new Config_I30(toc.GetAssetReader((byte)0, SYSTEM_PROGRESSION_CONFIG_AID));
				var root = config.ContentSection.Data;

				var suits = (JArray)root["SuitList"]["Suits"];
				var normalized = new JArray();
				foreach (var suit in suits)
				{
					normalized.Add(new JObject()
					{
						["Name"] = suit["Name"],
						["DisplayName"] = suit["DisplayName"],
						["PreviewImage"] = suit["Icon"]?["AssetPath"] ?? "",
						["GivesItems"] = suit["Item"] != null
							? new JObject() { ["Item"] = suit["Item"] }
							: null
					});
				}

				return new JObject() { ["suits"] = normalized };
			}
			catch { }

			return null;
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
				if (suit.MarkedToDelete && !_showDeleted) continue;

				bool isMilesSuit = suit.SuitId.Contains("miles", StringComparison.OrdinalIgnoreCase)
					|| suit.SuitId.Equals("SUIT_TAURIN", StringComparison.OrdinalIgnoreCase)
					|| suit.SuitId.Equals("SUIT_PANTHER", StringComparison.OrdinalIgnoreCase);
				bool showThisSuit = (_activeCharacter == MSM2Character.Miles) ? isMilesSuit : !isMilesSuit;
				if (!showThisSuit) continue;

				_displayedSuits.Add(new SuitSlot()
				{
					SuitId = suit.SuitId,
					Name = suit.Name,
					Icon = GetIcon(suit.IconPath),
					BigIcon = GetIcon(suit.BigIconPath),
					IconPath = suit.IconPath,
					BigIconPath = suit.BigIconPath,
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
