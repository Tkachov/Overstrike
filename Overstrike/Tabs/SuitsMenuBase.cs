// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using Overstrike.Data;
using Overstrike.Games;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Overstrike.Tabs {
	#region data classes

	public class SuitSlot { // needs to be referenced in xaml
		public string SuitId { get; set; }
		public string Name { get; set; }
		public BitmapSource Icon { get; set; }
		public BitmapSource BigIcon { get; set; }
		public string IconPath;
		public string BigIconPath;
		public string LoadoutPath;
		public bool MarkedToDelete;

		public float DisplayOpacity => (MarkedToDelete ? 0.3f : 1.0f);
	}

	#endregion

	public abstract class SuitsMenuBase: UserControl {
		#region implementation-defined

		protected abstract ListView SuitsSlots { get; }
		protected abstract Grid Modified { get; }
		protected abstract Grid NotModified { get; }
		protected abstract TextBlock SuitName { get; }
		protected abstract Grid SuitInfo { get; }
		protected abstract System.Windows.Controls.Image BigIcon { get; }
		protected abstract ComboBox SuitLoadoutComboBox { get; }
		protected abstract ComboBox SuitIconComboBox { get; }
		protected abstract ComboBox SuitBigIconComboBox { get; }
		protected abstract Button ToggleSuitDeleteButton { get; }
		protected abstract Label NotModifiedStatusLabel { get; }
		protected abstract Button ResetButton { get; }

		protected abstract bool HasBigIcons { get; }
		protected abstract Dictionary<string, byte> LANGUAGES { get; }

		#endregion
		#region data classes

		protected class LoadoutItem {
			public string Name { get; set; }
			public string Path;
		}

		protected class IconItem {
			public string Name { get; set; }
			public BitmapSource Icon { get; set; }
			public string Path;
		}

		#endregion

		#region provided by parent window

		protected Profile _selectedProfile;
		protected Action<Thread> _addTaskThread;
		protected Action<string, string> _setOverlayLabels;

		#endregion
		#region state

		protected bool _loaded = false;
		protected bool _hasChanges = false;
		protected bool _wasReset = false;

		protected Profile _tocProfile = null;
		protected TOC_I20 _toc = null;
		protected TOC_I20 toc {
			get {
				if (_toc == null) {
					try {
						var selectedGame = GameBase.GetGame(_selectedProfile.Game);
						var tocPath = selectedGame.GetTocPath(_selectedProfile.GamePath);

						var toc = new TOC_I20();
						toc.Load(tocPath);
						_toc = toc;
						_tocProfile = _selectedProfile;
					} catch {}
				}

				return _toc;
			}
		}

		protected DAT1.Files.Localization _cachedLocalization;
		protected DAT1.Files.Localization Localization {
			get {
				const ulong LOCALIZATION_AID = 0xBE55D94F171BF8DE; // localization/localization_all.localization

				if (_cachedLocalization == null) {
					try {
						var language = _selectedProfile.Settings_Suit_Language;
						byte span = 0;
						if (LANGUAGES.ContainsKey(language))
							span = LANGUAGES[language];

						_cachedLocalization = new DAT1.Files.Localization(toc.GetAssetReader(span, LOCALIZATION_AID));
					} catch {}
				}

				return _cachedLocalization;
			}
		}

		protected List<string> _iconsPaths = new();
		protected HashSet<string> _iconsPathsSet = new();

		protected List<string> _bigIconsPaths = new();
		protected HashSet<string> _bigIconsPathsSet = new();

		protected List<string> _loadoutsPaths = new();
		protected HashSet<string> _loadoutsPathsSet = new();

		protected Dictionary<string, BitmapSource> _icons = new();
		protected Dictionary<string, Bitmap> _iconsOrigs = new();
		protected BitmapImage _placeholderImage = null;
		protected BitmapImage _bigPlaceholderImage = null;

		protected List<SuitSlot> _configSuits = new();
		protected List<SuitSlot> _customizedSuits = new(); // _configSuits with user's modifications applied

		#endregion
		#region observable collections

		protected ObservableCollection<SuitSlot> _displayedSuits = new();

		protected ObservableCollection<LoadoutItem> _loadouts = new();

		protected ObservableCollection<IconItem> _iconItems = new();
		protected ObservableCollection<IconItem> _bigIconItems = new();

		#endregion
		#region other xaml bindings

		protected bool _showDeleted = true;
		public bool ShowDeleted { // is used in MainWindow for xaml binding to work
			get => _showDeleted;
			set {
				_showDeleted = value;

				var selectedSuit = GetCurrentlySelectedSuitId();
				MakeDisplayedSuits();
				SelectSuitWithId(selectedSuit);
			}
		}

		#endregion

		#region start

		public void Init(Action<Thread> addTaskThread, Action<string, string> setOverlayLabels) {
			_addTaskThread = addTaskThread;
			_setOverlayLabels = setOverlayLabels;
		}

		public void SetProfile(Profile profile) {
			_selectedProfile = profile;
		}

		public void Reopen() {
			OnOpen();
		}

		public void OnOpen() {
			SuitDeselected();

			if (_loaded) {
				if (_tocProfile == _selectedProfile) {
					// same profile, so no need to reload anything
					if (SuitsSlots.SelectedItem != null) {
						SuitSelected((SuitSlot)SuitsSlots.SelectedItem);
					}

					// it's unlikely config have changed, so not suggesting to refresh
					return;
				}

				if (SuitsCache.NormalizePath(_tocProfile.GamePath) == SuitsCache.NormalizePath(_selectedProfile.GamePath)) {
					// different profiles, same game, so no need to reload toc, but need to reload profile suits
					string previouslySelectedSuitId = GetCurrentlySelectedSuitId();

					_tocProfile = _selectedProfile;
					StartReloadProfileThread(previouslySelectedSuitId);
					return;
				}
			}

			StartLoadSuitsThread();
		}

		public void RequestReload() { // called after mods install, forcing config reload on next open of the tab
			_loaded = false;
		}

		#endregion
		#region loading

		#region - thread starting
		
		private void StartLoadSuitsThread() {
			_setOverlayLabels("Loading Suits Menu", "Loading TOC...");

			var thread = new Thread(LoadSuits);
			_addTaskThread(thread);
			thread.Start();
		}

		private void StartReloadProfileThread(string previouslySelectedSuitId) {
			_setOverlayLabels("Loading Suits Menu", "Loading TOC...");

			var thread = new Thread(() => ReloadProfileSuits(previouslySelectedSuitId));
			_addTaskThread(thread);
			thread.Start();
		}

		#endregion
		#region - thread logic

		private void LoadSuits() {
			Clear();

			var path = SuitsCache.NormalizePath(_selectedProfile.GamePath);
			var cache = ((App)App.Current).SuitsCache;
			
			var needToReloadConfig = false;
			if (cache.HasConfig(path)) {
				// check cached config ts against toc modification ts and reload if it is older
				const bool FORCE_RELOAD_IF_TOC_NEWER = true;
				if (FORCE_RELOAD_IF_TOC_NEWER) {
					try {
						var cacheTimestamp = cache.GetTimestamp(path);
						var tocTimestamp = GetTocTimestamp(_selectedProfile);

						if (cacheTimestamp < tocTimestamp) {
							// technically, Overstrike does caching first and writes toc to file second, so ignore the difference if it's small
							if (tocTimestamp - cacheTimestamp > 60) {
								needToReloadConfig = true;
							}
						}
					} catch {}
				}
			} else {
				needToReloadConfig = true;
			}

			if (needToReloadConfig) {
				// load config and put it to cache
				var loadedConfig = LoadConfig(toc);
				if (loadedConfig != null) {
					cache.SetConfig(path, loadedConfig);
				}
			}

			if (!cache.HasConfig(path)) {
				// show a warning / disable all the UI -- couldn't load the config in
				Dispatcher.Invoke(() => {
					MessageBoxResult result = MessageBox.Show("Suits Menu failed to load .config!", "Warning", MessageBoxButton.OK);
					IsEnabled = false;
				});
				return;
			}

			var config = cache.GetConfig(path);
			LoadConfigSuits(config);
			MakeCustomizedSuits();

			Dispatcher.Invoke(() => {
				MakeDisplayedSuits();
				MakeLoadouts();
				MakeIconItems();

				SetWasReset(false);
				IsEnabled = true;
			});

			_loaded = true;
		}

		private void ReloadProfileSuits(string previouslySelectedSuitId) {
			_hasChanges = false;
			MakeCustomizedSuits();

			Dispatcher.Invoke(() => {
				MakeDisplayedSuits();
				MakeLoadouts();
				MakeIconItems();

				SelectSuitWithId(previouslySelectedSuitId);
				SetWasReset(false);
			});

			_loaded = true;
		}

		//

		public static JObject LoadConfig(TOC_I20 toc) { // is also used after mods install to cache
			try {
				const ulong SYSTEM_PROGRESSION_CONFIG_AID = 0x9C9C72A303FCFA30; // configs/system/system_progression.config
				var config = new Config(toc.GetAssetReader(SYSTEM_PROGRESSION_CONFIG_AID));
				var root = config.ContentSection.Root;
				foreach (var techlist in root["TechWebLists"]) {
					if ((string)techlist["Description"] == "Suits") {
						return new JObject() {
							["suits"] = (JArray)techlist["TechWebItems"]
						};
					}
				}
			} catch {}

			return null;
		}

		private static long GetTocTimestamp(Profile profile) {
			var selectedGame = GameBase.GetGame(profile.Game);
			var tocPath = selectedGame.GetTocPath(profile.GamePath);

			var dt = File.GetLastWriteTimeUtc(tocPath);
			return ((DateTimeOffset)dt).ToUnixTimeSeconds();
		}

		#endregion
		#region - filling state

		private void Clear() {
			_loaded = false;
			_hasChanges = false;
			_toc = null;
			_cachedLocalization = null;

			_configSuits.Clear();
			_customizedSuits.Clear();

			_loadoutsPaths.Clear();
			_loadoutsPathsSet.Clear();

			_iconsPaths.Clear();
			_iconsPathsSet.Clear();
			_bigIconsPaths.Clear();
			_bigIconsPathsSet.Clear();

			_iconsOrigs.Clear();
			_icons.Clear();

			// don't clear the observable collections here and do it in corresponding methods (which are called through Dispatcher)
		}

		private void LoadConfigSuits(JObject config) {
			_configSuits.Clear();
			foreach (var suit in config["suits"]) {
				string bigIcon = "";
				string icon = (string)(suit["PreviewImage"]);
				string name = (string)(suit["Name"]);
				string displayName = (string)suit["DisplayName"];

				if (HasBigIcons) {
					bigIcon = (string)(suit["PreviewImage"]);
					icon = (string)(suit["ThumbnailImage"]);

					if (icon == null) icon = "";
					if (bigIcon == null) bigIcon = "";

					if (bigIcon != "" && icon == "") icon = bigIcon; // for sure
					else if (icon != "" && bigIcon == "") bigIcon = icon; // maybe?
				}

				string loadout = null;
				JToken givesItems = suit["GivesItems"];
				if (givesItems != null) {
					JObject givesItem = givesItems as JObject;
					if (givesItem == null) {
						JArray givesItemsArray = givesItems as JArray;
						if (givesItemsArray != null && givesItemsArray.Count > 0) {
							givesItem = (JObject)givesItems[0];
						}
					}

					if (givesItem != null) {
						loadout = (string)givesItem["Item"];
					}
				}

				icon = DAT1.Utils.Normalize(icon);
				bigIcon = DAT1.Utils.Normalize(bigIcon);
				loadout = DAT1.Utils.Normalize(loadout);

				RememberIcon(icon);
				RememberBigIcon(bigIcon);
				RememberLoadout(loadout);

				LoadIcon(icon);
				LoadIcon(bigIcon);

				var suitInfo = new SuitSlot() {
					SuitId = name,
					Name = GetFriendlySuitName(displayName),
					Icon = null,
					BigIcon = null,
					IconPath = icon,
					BigIconPath = bigIcon,
					LoadoutPath = loadout,
					MarkedToDelete = false
				};
				_configSuits.Add(suitInfo);
			}
		}

		private void MakeCustomizedSuits() {
			_customizedSuits.Clear();

			var modifications = _selectedProfile.Suits;

			var deletedSuits = new Dictionary<string, bool>();
			foreach (var suit in modifications.DeletedSuits) {
				deletedSuits.Add(suit, true);
			}

			var suitsOrder = new Dictionary<string, int>();
			var order = modifications.SuitsOrder;
			for (int i = 0; i < order.Count; ++i) {
				suitsOrder.Add(order[i], i);
			}

			var originalOrder = new Dictionary<string, int>();
			for (int i = 0; i < _configSuits.Count; ++i) {
				originalOrder.Add(_configSuits[i].SuitId, i);
			}

			foreach (var suit in _configSuits) {
				_customizedSuits.Add(new SuitSlot() {
					SuitId = suit.SuitId,
					Name = suit.Name,
					Icon = suit.Icon,
					BigIcon = suit.BigIcon,
					IconPath = suit.IconPath,
					BigIconPath = suit.BigIconPath,
					LoadoutPath = suit.LoadoutPath,
					MarkedToDelete = suit.MarkedToDelete || deletedSuits.ContainsKey(suit.SuitId)
				});
			}

			_customizedSuits.Sort((a, b) => {
				var ai = suitsOrder.ContainsKey(a.SuitId) ? suitsOrder[a.SuitId] : _customizedSuits.Count;
				var bi = suitsOrder.ContainsKey(b.SuitId) ? suitsOrder[b.SuitId] : _customizedSuits.Count;
				if (ai != bi) {
					return ai - bi;
				}

				ai = originalOrder[a.SuitId];
				bi = originalOrder[b.SuitId];
				if (ai != bi) {
					return ai - bi;
				}

				return a.Name.CompareTo(b.Name);
			});

			var modify = modifications.Modifications;
			foreach (var suit in _customizedSuits) {
				if (!modify.ContainsKey(suit.SuitId)) continue;

				var changes = modify[suit.SuitId];
				if (changes.ContainsKey("small_icon")) {
					suit.IconPath = (string)changes["small_icon"];
					RememberIcon(suit.IconPath);
					LoadIcon(suit.IconPath);
				}

				if (changes.ContainsKey("big_icon")) {
					suit.BigIconPath = (string)changes["big_icon"];
					RememberBigIcon(suit.BigIconPath);
					LoadIcon(suit.BigIconPath);
				}

				if (changes.ContainsKey("model")) {
					suit.LoadoutPath = (string)changes["model"];
					RememberLoadout(suit.LoadoutPath);
				}
			}
		}

		//

		protected void RememberIcon(string path) {
			if (path == null || path == "")
				return;
			if (_iconsPathsSet.Contains(path))
				return;

			_iconsPaths.Add(path);
			_iconsPathsSet.Add(path);
		}

		protected void RememberBigIcon(string path) {
			if (path == null || path == "")
				return;
			if (_bigIconsPathsSet.Contains(path))
				return;

			_bigIconsPaths.Add(path);
			_bigIconsPathsSet.Add(path);
		}

		protected void RememberLoadout(string path) {
			if (path == null || path == "")
				return;
			if (_loadoutsPathsSet.Contains(path))
				return;

			_loadoutsPaths.Add(path);
			_loadoutsPathsSet.Add(path);
		}

		protected void LoadIcon(string path) {
			if (path == null || path == "")
				return;
			if (_iconsOrigs.ContainsKey(path) && _iconsOrigs[path] != null)
				return;

			try {
				Texture_I20 texture = new Texture_I20(toc.GetAssetReader(path));
				var dds = texture.GetDDS();
				_iconsOrigs[path] = Utils.Imaging.DdsToBitmap(dds);
			} catch {}
		}

		protected string GetFriendlySuitName(string locstring_key) {
			string localized = Localization?.GetValue(locstring_key);
			if (localized != null) return localized;
			return "%" + locstring_key + "%";
		}

		#endregion
		#region - making observable items

		private void MakeDisplayedSuits() {
			_displayedSuits.Clear();
			foreach (var suit in _customizedSuits) {
				if (suit.MarkedToDelete && !_showDeleted) continue;

				_displayedSuits.Add(new SuitSlot() {
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
		}

		private void MakeLoadouts() {
			_loadouts.Clear();
			foreach (var path in _loadoutsPaths) {
				_loadouts.Add(new LoadoutItem() { Path = path, Name = GetFriendlyLoadoutName(path) });
			}
			SuitLoadoutComboBox.ItemsSource = _loadouts;
		}

		private void MakeIconItems() {
			_iconItems.Clear();
			foreach (var icon in _iconsPaths) {
				_iconItems.Add(new IconItem() { Path = icon, Name = Path.GetFileName(icon), Icon = GetIcon(icon) });
			}
			SuitIconComboBox.ItemsSource = _iconItems;

			if (HasBigIcons) {
				_bigIconItems.Clear();
				foreach (var icon in _bigIconsPaths) {
					_bigIconItems.Add(new IconItem() { Path = icon, Name = Path.GetFileName(icon), Icon = GetBigIcon(icon) });
				}
				SuitBigIconComboBox.ItemsSource = _bigIconItems;
			}
		}

		//

		protected string GetFriendlyLoadoutName(string path) {
			try {
				var config = new Config(toc.GetAssetReader(path));

				var path2 = (string)config.ContentSection.Root["ItemLoadoutConfig"]["AssetPath"];
				var config2 = new Config(toc.GetAssetReader(path2));

				/*
				"Loadout": {
					"ItemLoadoutLists": {
						"Items": [
							{"Item": "configs/VanityBodyType/VanityBody_SpiderMan.config"},
							{"Item": "configs/VanityHED/VanityHEDSpiderMan1.config"},
							{"Item": "configs/VanityTOR1/VanityTOR1aSpiderMan1.config"}          <--- we need this
						]
					},
					"Name": "Spider-Man White Spider Suit"
				}
				*/

				var loadoutItems = (JArray)config2.ContentSection.Root["Loadout"]["ItemLoadoutLists"]["Items"];
				foreach (JObject item in loadoutItems) {
					var path3 = (string)item["Item"];
					path3 = DAT1.Utils.Normalize(path3);
					if (!path3.StartsWith("configs/vanitytor1/")) continue;

					var config3 = new Config(toc.GetAssetReader(path3));

					/*
					"ModelList": {
						"Model": {
							"AssetPath": "characters/hero/hero_spiderman/hero_spiderman_body.model",     <--- this
							"Autoload": false
						},
						"BodyType": "kAll"
					}
					*/

					var modelPath = (string)config3.ContentSection.Root["ModelList"]["Model"]["AssetPath"];
					return Path.GetFileName(modelPath);
				}
			} catch {}

			var name = Path.GetFileName(path);

			if (name.StartsWith("inv_reward_")) {
				name = name[11..];
			} else if (name.StartsWith("inv_rewards_")) {
				name = name[12..];
			}

			if (name.StartsWith("loadout_")) {
				name = name[8..];
			}

			if (name.EndsWith(".config")) {
				name = name[..^7];
			}

			return name;
		}

		protected BitmapSource GetIcon(string path) { // can only be called from UI thread since it creates the BitmapSource objects
			if (_iconsOrigs.ContainsKey(path) && _iconsOrigs[path] != null && (!_icons.ContainsKey(path) || _icons[path] == null))
				_icons[path] = Utils.Imaging.ConvertToBitmapImage(_iconsOrigs[path]);

			if (_icons.ContainsKey(path) && _icons[path] != null)
				return _icons[path];

			if (_placeholderImage == null)
				_placeholderImage = Utils.Imaging.ConvertToBitmapImage(HasBigIcons ? Properties.Resources.suit_missing_mm : Properties.Resources.suit_missing);

			return _placeholderImage;
		}

		protected BitmapSource GetBigIcon(string path) {
			if (_iconsOrigs.ContainsKey(path) && _iconsOrigs[path] != null && (!_icons.ContainsKey(path) || _icons[path] == null))
				_icons[path] = Utils.Imaging.ConvertToBitmapImage(_iconsOrigs[path]);

			if (_icons.ContainsKey(path) && _icons[path] != null)
				return _icons[path];

			if (_bigPlaceholderImage == null)
				_bigPlaceholderImage = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.suit_missing_mm_big); // no MSMR variant since there are no big icons in it

			return _bigPlaceholderImage;
		}

		#endregion

		#endregion

		#region UI logic / helpers

		public void TickInvoke() { // called by MainWindow
			Modified.Visibility = (_hasChanges ? Visibility.Visible : Visibility.Collapsed);
			NotModified.Visibility = (_hasChanges ? Visibility.Collapsed : Visibility.Visible);
		}

		protected void SuitDeselected() {
			SuitName.Text = "No suit selected";
			SuitInfo.Visibility = Visibility.Collapsed;
		}

		protected void SuitSelected(SuitSlot data) {
			SuitName.Text = data.Name;
			SuitInfo.Visibility = Visibility.Visible;

			LoadoutItem loadout = null;
			foreach (var item in _loadouts) {
				if (item.Path == data.LoadoutPath) {
					loadout = item;
					break;
				}
			}
			SuitLoadoutComboBox.SelectedItem = loadout;

			IconItem icon = null;
			foreach (var item in _iconItems) {
				if (item.Path == data.IconPath) {
					icon = item;
					break;
				}
			}
			SuitIconComboBox.SelectedItem = icon;

			if (HasBigIcons) {
				IconItem bigIcon = null;
				foreach (var item in _bigIconItems) {
					if (item.Path == data.BigIconPath) {
						bigIcon = item;
						break;
					}
				}
				SuitBigIconComboBox.SelectedItem = bigIcon;
				BigIcon.Source = bigIcon != null ? bigIcon.Icon : GetBigIcon("");
			}

			ToggleSuitDeleteButton.Content = data.MarkedToDelete ? "Restore" : "Delete";
		}

		protected void RefreshSelectedSuit(SuitSlot selectedSuit) {
			var i = _displayedSuits.IndexOf(selectedSuit);
			_displayedSuits.RemoveAt(i);
			_displayedSuits.Insert(i, selectedSuit); // updates appearance (Icon/DisplayOpacity)

			SuitsSlots.SelectedItem = selectedSuit; // causes SuitSelected(), updating things like BigIcon
		}

		protected string GetCurrentlySelectedSuitId() => (SuitsSlots.SelectedItem == null ? null : ((SuitSlot)SuitsSlots.SelectedItem).SuitId);

		protected void SelectSuitWithId(string suitId) {
			foreach (var suit in _displayedSuits) {
				if (suit.SuitId == suitId) {
					SuitsSlots.SelectedItem = suit;
					break;
				}
			}
		}

		protected void SetWasReset(bool wasReset) {
			_wasReset = wasReset;

			NotModifiedStatusLabel.Content = _wasReset ? "No changes at all" : "No unsaved changes";
			ResetButton.Content = _wasReset ? "Undo reset" : "Reset";
		}

		#endregion
		#region event handlers

		protected void SuitsSlots_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e.AddedItems.Count > 0) {
				SuitSelected((SuitSlot)e.AddedItems[0]);
			} else {
				SuitDeselected();
			}
		}

		protected void SuitLoadoutComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e.AddedItems.Count <= 0) return;
			Debug.Assert(SuitsSlots.SelectedItem != null);

			var loadout = (LoadoutItem)e.AddedItems[0];
			var selectedSuit = (SuitSlot)SuitsSlots.SelectedItem;
			if (selectedSuit.LoadoutPath == loadout.Path) return;

			_hasChanges = true;
			SetWasReset(false);
			selectedSuit.LoadoutPath = loadout.Path;

			foreach (var suit in _customizedSuits) {
				if (suit.SuitId == selectedSuit.SuitId) {
					suit.LoadoutPath = selectedSuit.LoadoutPath;
					break;
				}
			}
		}

		protected void SuitIconComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e.AddedItems.Count <= 0) return;
			Debug.Assert(SuitsSlots.SelectedItem != null);

			var icon = (IconItem)e.AddedItems[0];
			var selectedSuit = (SuitSlot)SuitsSlots.SelectedItem;
			if (selectedSuit.IconPath == icon.Path) return;

			_hasChanges = true;
			SetWasReset(false);
			selectedSuit.IconPath = icon.Path;
			selectedSuit.Icon = GetIcon(icon.Path);

			foreach (var suit in _customizedSuits) {
				if (suit.SuitId == selectedSuit.SuitId) {
					suit.IconPath = selectedSuit.IconPath;
					// no need to sync Icon as _customizedSuits are not for display
					break;
				}
			}

			RefreshSelectedSuit(selectedSuit);
		}

		protected void SuitBigIconComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e.AddedItems.Count <= 0) return;
			Debug.Assert(SuitsSlots.SelectedItem != null);

			var icon = (IconItem)e.AddedItems[0];
			var selectedSuit = (SuitSlot)SuitsSlots.SelectedItem;
			if (selectedSuit.BigIconPath == icon.Path) return;

			_hasChanges = true;
			SetWasReset(false);
			selectedSuit.BigIconPath = icon.Path;
			selectedSuit.BigIcon = GetBigIcon(icon.Path);

			foreach (var suit in _customizedSuits) {
				if (suit.SuitId == selectedSuit.SuitId) {
					suit.BigIconPath = selectedSuit.BigIconPath;
					// no need to sync BigIcon as _customizedSuits are not for display
					break;
				}
			}

			RefreshSelectedSuit(selectedSuit);
		}

		protected void ToggleSuitDeleteButton_Click(object sender, RoutedEventArgs e) {
			Debug.Assert(SuitsSlots.SelectedItem != null);

			var selectedSuit = (SuitSlot)SuitsSlots.SelectedItem;

			_hasChanges = true;
			SetWasReset(false);
			selectedSuit.MarkedToDelete = !selectedSuit.MarkedToDelete;

			foreach (var suit in _customizedSuits) {
				if (suit.SuitId == selectedSuit.SuitId) {
					suit.MarkedToDelete = selectedSuit.MarkedToDelete;
					break;
				}
			}

			if (_showDeleted) {
				RefreshSelectedSuit(selectedSuit);
			} else {
				var i = _displayedSuits.IndexOf(selectedSuit);
				_displayedSuits.RemoveAt(i);

				SuitsSlots.SelectedItem = null;
			}
		}

		protected void ResetButtonClicked(object sender, RoutedEventArgs e) {
			Debug.Assert(_hasChanges == false);

			string previouslySelectedSuitId = GetCurrentlySelectedSuitId();
			if (_wasReset) {
				SetWasReset(false);

				StartReloadProfileThread(previouslySelectedSuitId);
			} else {
				SetWasReset(true);

				// fill customizedSuits with the exact copy of configSuits -- that is what "reset" / "no changes at all" means
				_customizedSuits.Clear();
				foreach (var suit in _configSuits) {
					_customizedSuits.Add(new SuitSlot() {
						SuitId = suit.SuitId,
						Name = suit.Name,
						Icon = suit.Icon,
						BigIcon = suit.BigIcon,
						IconPath = suit.IconPath,
						BigIconPath = suit.BigIconPath,
						LoadoutPath = suit.LoadoutPath,
						MarkedToDelete = suit.MarkedToDelete
					});
				}

				MakeDisplayedSuits();
				MakeLoadouts();
				MakeIconItems();

				SelectSuitWithId(previouslySelectedSuitId);
			}
		}

		protected void UndoChangesButtonClicked(object sender, RoutedEventArgs e) {
			_hasChanges = false;
			SetWasReset(false);
			StartReloadProfileThread(GetCurrentlySelectedSuitId());
		}

		protected void SaveChangesButtonClicked(object sender, RoutedEventArgs e) {
			_hasChanges = false;
			SetWasReset(false);

			List<string> deleted = new();
			List<string> order = new();
			Dictionary<string, JObject> modify = new();

			var originalSuits = new Dictionary<string, SuitSlot>();
			foreach (var suit in _configSuits) {
				originalSuits.Add(suit.SuitId, suit);
			}

			foreach (var suit in _customizedSuits) {
				order.Add(suit.SuitId);
				if (suit.MarkedToDelete) deleted.Add(suit.SuitId);

				JObject changes = new();
				bool suitHasChanges = false;
				if (originalSuits.ContainsKey(suit.SuitId)) {
					var originalSuit = originalSuits[suit.SuitId];
					if (originalSuit.IconPath != suit.IconPath) {
						changes["small_icon"] = suit.IconPath;
						suitHasChanges = true;
					}

					if (HasBigIcons) {
						if (originalSuit.BigIconPath != suit.BigIconPath) {
							changes["big_icon"] = suit.BigIconPath;
							suitHasChanges = true;
						}
					}

					if (originalSuit.LoadoutPath != suit.LoadoutPath) {
						changes["model"] = suit.LoadoutPath;
						suitHasChanges = true;
					}
				}

				if (suitHasChanges) modify.Add(suit.SuitId, changes);
			}

			_selectedProfile.Suits = new SuitsModifications(deleted, order, modify);
			_selectedProfile.Save();
		}

		#endregion
		#region drag and drop

		private System.Windows.Point _dragStartPosition;
		private System.Windows.Point _dragCurrentPosition;
		private DragAdorner _adorner;
		private AdornerLayer _layer;
		private static string DND_DATA_FORMAT = "SuitsMenuDragAndDropDataFormat";

		protected void SuitsSlots_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			_dragStartPosition = e.GetPosition(null);
		}

		protected void SuitsSlots_MouseMove(object sender, MouseEventArgs e) {
			if (e.LeftButton == MouseButtonState.Pressed) {
				_dragCurrentPosition = e.GetPosition(null);

				if (Math.Abs(_dragCurrentPosition.X - _dragStartPosition.X) > SystemParameters.MinimumHorizontalDragDistance ||
					Math.Abs(_dragCurrentPosition.Y - _dragStartPosition.Y) > SystemParameters.MinimumVerticalDragDistance) {
					BeginDrag(e);
				}
			}
		}

		protected void BeginDrag(MouseEventArgs e) {
			ListView listView = SuitsSlots;
			ListViewItem listViewItem = Utils.DragDrop.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

			if (listViewItem == null)
				return;

			// get the data for the ListViewItem
			SuitSlot item = listView.ItemContainerGenerator.ItemFromContainer(listViewItem) as SuitSlot;
			var indexBefore = _displayedSuits.IndexOf(item);

			// setup the drag adorner
			InitializeAdorner(listViewItem);

			// add handles to update the adorner
			SuitsSlots.PreviewDragOver += SuitsSlots_DragOver;
			SuitsSlots.DragLeave += SuitsSlots_DragLeave;
			SuitsSlots.DragEnter += SuitsSlots_DragEnter;

			var data = new DataObject(DND_DATA_FORMAT, item);
			DragDrop.DoDragDrop(SuitsSlots, data, DragDropEffects.Move);

			// cleanup
			SuitsSlots.PreviewDragOver -= SuitsSlots_DragOver;
			SuitsSlots.DragLeave -= SuitsSlots_DragLeave;
			SuitsSlots.DragEnter -= SuitsSlots_DragEnter;

			if (_adorner != null) {
				AdornerLayer.GetAdornerLayer(listView).Remove(_adorner);
				_adorner = null;
			}

			listView.SelectedItem = item;

			var indexAfter = _displayedSuits.IndexOf(item);
			if (indexBefore != indexAfter) {
				_hasChanges = true;
				SetWasReset(false);
			}
		}

		protected void InitializeAdorner(ListViewItem listViewItem) {
			VisualBrush brush = new VisualBrush(listViewItem);
			_adorner = new DragAdorner(listViewItem, listViewItem.RenderSize, brush);
			_adorner.Opacity = 0.5;
			_layer = AdornerLayer.GetAdornerLayer(SuitsSlots);
			_layer.Add(_adorner);
		}

		protected void SuitsSlots_DragEnter(object sender, DragEventArgs e) {
			if (!e.Data.GetDataPresent(DND_DATA_FORMAT) || sender == e.Source) {
				e.Effects = DragDropEffects.None;
			}
		}

		protected void SuitsSlots_DragOver(object sender, DragEventArgs e) {
			if (_adorner != null) {
				IInputElement element = Window.GetWindow(this);
				_adorner.OffsetLeft = e.GetPosition(element).X - _dragCurrentPosition.X;
				_adorner.OffsetTop = e.GetPosition(element).Y - _dragCurrentPosition.Y;
			}
		}

		protected void SuitsSlots_DragLeave(object sender, DragEventArgs e) {
			if (e.OriginalSource == SuitsSlots) {
				System.Windows.Point p = e.GetPosition(SuitsSlots);
				Rect r = VisualTreeHelper.GetContentBounds(SuitsSlots);
				if (!r.Contains(p)) {
					e.Handled = true;
				}
			}
		}

		protected void SuitsSlots_Drop(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DND_DATA_FORMAT)) {
				SuitSlot item = e.Data.GetData(DND_DATA_FORMAT) as SuitSlot;
				ListViewItem listViewItem = Utils.DragDrop.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

				if (listViewItem != null) {
					SuitSlot itemToReplace = SuitsSlots.ItemContainerGenerator.ItemFromContainer(listViewItem) as SuitSlot;
					int index = SuitsSlots.Items.IndexOf(itemToReplace);
					if (index >= 0) {
						_displayedSuits.Remove(item);
						_displayedSuits.Insert(index, item);
						SuitsSlots.ItemsSource = _displayedSuits;

						MoveCustomizedSuit(item, itemToReplace);
					}
				} else {
					_displayedSuits.Remove(item);
					_displayedSuits.Add(item);
					SuitsSlots.ItemsSource = _displayedSuits;

					MoveCustomizedSuit(item, null);
				}
			}
		}

		protected void MoveCustomizedSuit(SuitSlot slotToMove, SuitSlot slotToInsertBefore) {
			if (slotToMove == null) return;

			SuitSlot customizedSlotToMove = null;
			foreach (var slot in _customizedSuits) {
				if (slot.SuitId == slotToMove.SuitId) {
					customizedSlotToMove = slot;
					break;
				}
			}

			if (customizedSlotToMove == null) return;

			if (slotToInsertBefore == null) {
				_customizedSuits.Remove(customizedSlotToMove);
				_customizedSuits.Add(customizedSlotToMove);
				return;
			}

			int index = -1;
			for (int i = 0; i < _customizedSuits.Count; ++i) {
				if (_customizedSuits[i].SuitId == slotToInsertBefore.SuitId) {
					index = i;
					break;
				}
			}

			if (index >= 0) {
				_customizedSuits.Remove(customizedSlotToMove);
				_customizedSuits.Insert(index, customizedSlotToMove);
			}
		}

		#endregion
	}
}
