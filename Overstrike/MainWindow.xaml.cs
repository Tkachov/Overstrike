// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Microsoft.WindowsAPICodePack.Dialogs;
using Overstrike.Data;
using Overstrike.Games;
using Overstrike.Installers;
using Overstrike.MetaInstallers;
using Overstrike.Tabs;
using Overstrike.Utils;
using Overstrike.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Overstrike {
	public partial class MainWindow: Window {
		private AppSettings _settings;
		private List<Profile> _profiles;
		private List<ModEntry> _mods;
		private Profile _selectedProfile;

		private GameBase _selectedGame => GameBase.GetGame(_selectedProfile.Game);
		private bool _selectedGameHasSuitsMenu => (_selectedProfile.Game == GameMSMR.ID || _selectedProfile.Game == GameMM.ID);

		private class ProfileItem {
			public string Text { get; set; }
			public Profile Profile { get; set; }
		}

		private ObservableCollection<ProfileItem> _profilesItems = new ObservableCollection<ProfileItem>();
		private ObservableCollection<ModEntry> _modsList = new ObservableCollection<ModEntry>();
		private ModEntry _suitsMenuEntry; // has to be the last in list

		private Point _dragStartPosition;
		private DragAdorner _adorner;
		private AdornerLayer _layer;
		private Point _dragCurrentPosition;

		private bool _statusMessageErrorShown = false;

		private Thread _tickThread;
		private List<Thread> _taskThreads = new List<Thread>();

		#region settings data binding

		public bool Settings_CacheModsLibrary {
			get => _settings.CacheModsLibrary;
			set {
				_settings.CacheModsLibrary = value;
				SaveSettings();
			}
		}

		public bool Settings_PreferCachedModsLibrary {
			get => _settings.PreferCachedModsLibrary;
			set {
				_settings.PreferCachedModsLibrary = value;
				SaveSettings();
			}
		}

		public bool Settings_CheckUpdates {
			get => _settings.CheckUpdates;
			set {
				_settings.CheckUpdates = value;
				SaveSettings();
			}
		}

		public bool Settings_OpenErrorLog {
			get => _settings.OpenErrorLog;
			set {
				_settings.OpenErrorLog = value;
				SaveSettings();
			}
		}

		private class LanguageItem {
			public string Name { get; set; }
			public string InternalName { get; set; }
		}

		private ObservableCollection<LanguageItem> _suitLanguageItems = new();

		#endregion

		public bool MSMRSuitsMenuContent_ShowDeleted {
			get => MSMRSuitsMenuContent.ShowDeleted;
			set { MSMRSuitsMenuContent.ShowDeleted = value; }
		}

		public bool MMSuitsMenuContent_ShowDeleted {
			get => MMSuitsMenuContent.ShowDeleted;
			set { MMSuitsMenuContent.ShowDeleted = value; }
		}

		public MainWindow(AppSettings settings, List<Profile> profiles, List<ModEntry> mods) {
			InitializeComponent();

			DetectWinmm();

			_settings = settings;
			_profiles = profiles;
			_mods = mods;

			AddModsIcon.Source = Imaging.ConvertToBitmapImage(Properties.Resources.add_icon);
			RefreshIcon.Source = Imaging.ConvertToBitmapImage(Properties.Resources.reload_icon);

			MakeProfileItems();
			FirstSwitchToProfile();
			StartTickThread();
			ScheduleUpdateCheck();

			MSMRSuitsMenuContent.Init(AddTaskThread, SetOverlayLabels);
			MMSuitsMenuContent.Init(AddTaskThread, SetOverlayLabels);

			DataContext = this;
		}

		private void SaveSettings() {
			// TODO: run a timer so multiple calls in the row are aggregated into one actual file write
			((App)App.Current).WriteSettings();
		}

		private void FirstSwitchToProfile() {
			foreach (Profile p in _profiles) {
				if (p.Name == _settings.CurrentProfile) {
					SwitchToProfile(p);
					return;
				}
			}

			SwitchToProfile(_profiles[0]);
		}

		private void SwitchToProfile(Profile profile) {
			_selectedProfile = profile;
			UpdateSelectedProfileItem();

			_settings.CurrentProfile = profile.Name;
			SaveSettings();

			SetupBanner();
			ProfileGamePath.Content = profile.GamePath;

			UpdateSuitMenuTabs();
			MakeModsItems();
		}

		private bool _reactToScriptSettingsChange = true;

		private void SetupBanner() {
			GradientImage.Source = _selectedGame.BannerBackground;
			LogoImage.Source = _selectedGame.BannerLogoLeft;
			LogoImage2.Source = _selectedGame.BannerLogoRight;

			if (_selectedGame.HasSuitsSettingsSection) {
				SuitModsSettings.Visibility = Visibility.Visible;
				MakeSuitLanguageItems();
			} else {
				SuitModsSettings.Visibility = Visibility.Collapsed;
			}

			if (_selectedGame.HasScriptsSettingsSection) {
				ScriptSettings.Visibility = Visibility.Visible;
				_reactToScriptSettingsChange = false;
				ScriptSettings_EnableScripting.IsChecked = _selectedProfile.Settings_Scripts_Enabled;
				_reactToScriptSettingsChange = true;
			} else {
				ScriptSettings.Visibility = Visibility.Collapsed;
			}

			UpdateRunModdedButtonVisibility();
		}

		private void UpdateRunModdedButtonVisibility() {
			if (_selectedGame.HasScriptsSettingsSection && _selectedProfile.Settings_Scripts_Enabled) {
				RunModdedButton.Visibility = Visibility.Visible;
			} else {
				RunModdedButton.Visibility = Visibility.Collapsed;
			}
		}

		private void UpdateSuitMenuTabs() {
			MSMRSuitsMenuContent.SetProfile(_selectedProfile);
			MMSuitsMenuContent.SetProfile(_selectedProfile);

			MSMRSuitsMenuTab.Visibility = (_selectedProfile.Game == GameMSMR.ID ? Visibility.Visible : Visibility.Collapsed);
			MMSuitsMenuTab.Visibility = (_selectedProfile.Game == GameMM.ID ? Visibility.Visible : Visibility.Collapsed);

			// if currently open tab is no longer available (while switching between profiles), go to the first one
			// if it's available, "reopen" (since profile changed)

			if (MainTabs.SelectedItem == MSMRSuitsMenuTab) {
				if (MSMRSuitsMenuTab.Visibility != Visibility.Visible) {
					MainTabs.SelectedIndex = 0;
				} else {
					MSMRSuitsMenuContent.Reopen();
				}
			} else if (MainTabs.SelectedItem == MMSuitsMenuTab) {
				if (MMSuitsMenuTab.Visibility != Visibility.Visible) {
					MainTabs.SelectedIndex = 0;
				} else {
					MMSuitsMenuContent.Reopen();
				}
			}
		}

		private static readonly Dictionary<string, string> USERFRIENDLY_LANGUAGE_NAMES = new() {
			//{"en", "English"},
			{"us", "English"},
			{"uk", "English (UK)"},
			{"da", "Danish"},
			{"nl", "Dutch"},
			{"fi", "Finnish"},
			{"fr", "French"},
			{"de", "German"},
			{"it", "Italian"},
			{"jp", "Japanese"},
			{"ko", "Korean"},
			{"no", "Norwegian"},
			{"pl", "Polish"},
			{"pt", "Brazilian"},
			{"ru", "Russian"},
			{"es", "Spanish"},
			{"sv", "Swedish"},
			{"br", "Portuguese"},
			{"ar", "Arabic"},
			{"la", "Latin Spanish"},
			{"zh_s", "Simplified Chinese"},
			{"zh", "Traditional Chinese"},
			{"cs", "Czech"},
			{"hu", "Hungarian"},
			{"el", "Greek"},
		};

		private bool _reactToSuitLanguageSelectionChange = true;

		private void MakeSuitLanguageItems() {
			_suitLanguageItems.Clear();

			Dictionary<string, byte>.KeyCollection gameLanguages = null;
			if (_selectedProfile.Game == GameMSMR.ID) gameLanguages = MSMRSuitInstaller.LANGUAGES.Keys;
			else if (_selectedProfile.Game == GameMM.ID) gameLanguages = MMSuit1Installer.LANGUAGES.Keys;
			if (gameLanguages != null) {
				foreach (var lang in gameLanguages) {
					_suitLanguageItems.Add(new LanguageItem() { Name = USERFRIENDLY_LANGUAGE_NAMES[lang], InternalName = lang });
				}
			}
			_suitLanguageItems.Add(new LanguageItem() { Name = "<don't install names>", InternalName = "" });

			SettingsSuitLanguageComboBox.ItemsSource = new CompositeCollection {
				new CollectionContainer() { Collection = _suitLanguageItems }
			};

			_reactToSuitLanguageSelectionChange = false;
			LanguageItem selectedItem = null;
			foreach (var item in _suitLanguageItems) {
				if (item.InternalName == _selectedProfile.Settings_Suit_Language) {
					selectedItem = item;
					break;
				}
			}
			SettingsSuitLanguageComboBox.SelectedItem = selectedItem;
			_reactToSuitLanguageSelectionChange = true;
		}

		private void SettingsSuitLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (!_reactToSuitLanguageSelectionChange) return;
			if (e.AddedItems.Count <= 0) return;
			DAT1.Utils.Assert(SettingsSuitLanguageComboBox.SelectedItem != null);

			LanguageItem item = (LanguageItem)e.AddedItems[0];
			_selectedProfile.Settings_Suit_Language = item.InternalName;
			SaveProfile();
		}

		private void UpdateSelectedProfileItem() {
			ProfileItem profile = null;
			foreach (var item in _profilesItems) {
				if (item.Profile == _selectedProfile) {
					profile = item;
					break;
				}
			}
			ProfileComboBox.SelectedItem = profile;
		}

		private void MakeProfileItems() {
			_profilesItems.Clear();

			foreach (var profile in _profiles) {
				_profilesItems.Add(new ProfileItem() { Text = profile.Name, Profile = profile });
			}
			_profilesItems.Add(new ProfileItem() { Text = "Add new profile...", Profile = null });
			
			ProfileComboBox.ItemsSource = new CompositeCollection {
				new CollectionContainer() { Collection = _profilesItems }
			};
		}

		private void MakeModsItems() {
			Dictionary<string, bool> profileInstalled = new Dictionary<string, bool>();
			foreach (var mod in _selectedProfile.Mods) {
				profileInstalled[mod.Path] = mod.Install;
			}

			Dictionary<string, ModEntry> availableMods = new Dictionary<string, ModEntry>();
			foreach (var mod in _mods) {
				if (!_selectedGame.IsCompatible(mod)) continue;
				availableMods[mod.Path] = mod;
			}

			_modsList.Clear();

			var index = 1;
			foreach (var mod in _selectedProfile.Mods) { // first, adding previously known mods
				if (availableMods.ContainsKey(mod.Path)) {
					var install = mod.Install && profileInstalled[mod.Path];
					_modsList.Add(new ModEntry(availableMods[mod.Path], install, index, mod.Extras));
					++index;
				}
			}
			foreach (var mod in _mods) {
				if (availableMods.ContainsKey(mod.Path) && !profileInstalled.ContainsKey(mod.Path)) { // then, adding new mods
					_modsList.Add(new ModEntry(availableMods[mod.Path], mod.Install, index, null));
					++index;
				}
			}

			if (_selectedGameHasSuitsMenu) {
				var path = ModEntry.SUITS_MENU_PATH;
				bool suitsMenuInstalled;
				if (!profileInstalled.TryGetValue(path, out suitsMenuInstalled)) {
					suitsMenuInstalled = true;
				}

				var stub = new ModEntry("Suits Menu", path, ModEntry.ModType.SUITS_MENU);
				_suitsMenuEntry = new ModEntry(stub, suitsMenuInstalled, index, null);
				_modsList.Add(_suitsMenuEntry);
				++index;
			}

			foreach (var mod in _modsList) {
				mod.PropertyChanged += OnModPropertyChanged;
			}

			ModsList.ItemsSource = new CompositeCollection {
				new CollectionContainer() { Collection = _modsList }
			};
		}

		private void ProfileComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
			if (e.AddedItems.Count <= 0) return;
			DAT1.Utils.Assert(ProfileComboBox.SelectedItem != null);

			// TODO: check if profile needs saving, and save it (or ask to save?)

			ProfileItem item = (ProfileItem)e.AddedItems[0];
			if (item.Profile == null) {
				var window = new CreateProfile();
				window.ShowDialog();

				var p = window.GetProfile();
				if (p != null) {
					p.Save();
					SwitchToNewProfile(p);
				} else {
					UpdateSelectedProfileItem();
				}

				return;
			}

			if (item.Profile == _selectedProfile) return;
			
			SwitchToProfile(item.Profile);
		}

		private void SwitchToNewProfile(Profile newProfile) {
			_profiles = ((App)App.Current).ReloadProfiles();
			DAT1.Utils.Assert(_profiles.Count > 0);

			MakeProfileItems();

			// try switching to this new profile
			foreach (var profile in _profiles) {
				if (profile.Name == newProfile.Name) {
					SwitchToProfile(profile);
					return;
				}
			}

			// if couldn't find it, try switching back to what was selected before
			foreach (var profile in _profiles) {
				if (profile.Name == _selectedProfile.Name) {
					SwitchToProfile(profile);
					return;
				}
			}

			// last resort: do default profile loading
			FirstSwitchToProfile();
		}

		#region Tick

		private void StartTickThread() {
			_tickThread = new Thread(TickThread);
			_tickThread.Start();
		}

		private void TickThread() {
			try {
				while (true) {
					Thread.Sleep(16);
					Tick();
				}
			} catch (Exception ex) { }
		}

		private void Tick() {
			List<Thread> threadsToRemove = new List<Thread>();
			foreach (var thread in _taskThreads) {
				if (!thread.IsAlive) {
					threadsToRemove.Add(thread);
				}
			}
			foreach (Thread thread in threadsToRemove) {
				_taskThreads.Remove(thread);
			}

			bool hasTasks = _taskThreads.Count > 0;
			Dispatcher.Invoke(() => {
				Overlay.Visibility = (hasTasks ? Visibility.Visible : Visibility.Collapsed);

				if (MainTabs.SelectedItem == MSMRSuitsMenuTab) {
					MSMRSuitsMenuContent.TickInvoke();
				} else if (MainTabs.SelectedItem == MMSuitsMenuTab) {
					MMSuitsMenuContent.TickInvoke();
				}
			});
		}

		private void AddTaskThread(Thread thread) {
			_taskThreads.Add(thread);
		}

		private void SetOverlayLabels(string header, string operation) {
			if (header != null) OverlayHeaderLabel.Text = header;
			if (operation != null) OverlayOperationLabel.Text = operation;
		}

		#endregion

		#region update check

		private Timer _updateTimer;

		private void ScheduleUpdateCheck() {
			if (!_settings.CheckUpdates) return;

			_updateTimer = new Timer(OnUpdateTimer, null, 2000, Timeout.Infinite);
		}

		private void OnUpdateTimer(Object o) {
			_updateTimer.Dispose();
			_updateTimer = null;

			try {
				Process.Start("Check for updates.exe", "--silent");
			} catch {}
		}

		#endregion

		private void DetectWinmm() {
			try {
				var scriptsProxyName = "winmm.dll";
				var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
				var scriptsProxy = Path.Combine(exeDir, scriptsProxyName);

				if (File.Exists(scriptsProxy)) {
					MessageBox.Show($"'{scriptsProxyName}' found in Overstrike folder!\nBecause of this, mods installation WILL FAIL!\n\nClose Overstrike and move its files to another folder.", "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			} catch {}
		}

		#region Drag and Drop

		private void ModsList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			_dragStartPosition = e.GetPosition(null);
		}

		private void ModsList_MouseMove(object sender, MouseEventArgs e) {
			if (e.LeftButton == MouseButtonState.Pressed) {
				_dragCurrentPosition = e.GetPosition(null);

				if (Math.Abs(_dragCurrentPosition.X - _dragStartPosition.X) > SystemParameters.MinimumHorizontalDragDistance ||
					Math.Abs(_dragCurrentPosition.Y - _dragStartPosition.Y) > SystemParameters.MinimumVerticalDragDistance) {
					BeginDrag(e);
				}
			}
		}

		private void BeginDrag(MouseEventArgs e) {
			ListView listView = ModsList;
			ListViewItem listViewItem = Utils.DragDrop.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

			if (listViewItem == null)
				return;

			// get the data for the ListViewItem
			ModEntry name = listView.ItemContainerGenerator.ItemFromContainer(listViewItem) as ModEntry;

			// setup the drag adorner
			InitialiseAdorner(listViewItem);

			// add handles to update the adorner
			ModsList.PreviewDragOver += ModsList_DragOver;
			ModsList.DragLeave += ModsList_DragLeave;
			ModsList.DragEnter += ModsList_DragEnter;

			var selitems = ModsList.SelectedItems;
			var list = new List<ModEntry>();
			foreach (ModEntry entry in selitems) {
				list.Add(entry);
			}
			list.Sort((x, y) => _modsList.IndexOf(x) - _modsList.IndexOf(y));

			DataObject data = new DataObject("dataFormat", list);
			System.Windows.DragDrop.DoDragDrop(ModsList, data, DragDropEffects.Move);

			// cleanup
			ModsList.PreviewDragOver -= ModsList_DragOver;
			ModsList.DragLeave -= ModsList_DragLeave;
			ModsList.DragEnter -= ModsList_DragEnter;

			if (_adorner != null) {
				AdornerLayer.GetAdornerLayer(listView).Remove(_adorner);
				_adorner = null;
			}

			listView.SelectedItem = name;

			var indexes = new List<int>();
			int index = 0;
			foreach (var item in _modsList) {
				if (item.Order != index+1) {
					indexes.Add(index);
				}
				++index;
			}

			if (indexes.Count > 0) {
				foreach (var index2 in indexes) {
					var item = _modsList[index2];
					item.Order = index2 + 1;
					_modsList.RemoveAt(index2);
					_modsList.Insert(index2, item);
				}
				ModsList.ItemsSource = _modsList;
			}

			OnModsOrderChanged();
		}

		private void ModsList_DragEnter(object sender, DragEventArgs e) {
			if (!e.Data.GetDataPresent("dataFormat") ||
				sender == e.Source) {
				e.Effects = DragDropEffects.None;
			}
		}

		private void ModsList_Drop(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent("dataFormat")) {
				//ModEntry name = e.Data.GetData("dataFormat") as ModEntry;
				IList<ModEntry> list = e.Data.GetData("dataFormat") as IList<ModEntry>;
				ListViewItem listViewItem = Utils.DragDrop.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

				if (listViewItem != null) {
					ModEntry nameToReplace = ModsList.ItemContainerGenerator.ItemFromContainer(listViewItem) as ModEntry;
					int index = ModsList.Items.IndexOf(nameToReplace);

					if (index >= 0) {

							// _modsList.Remove(name);
							foreach (var item in list) {
								if (_modsList.IndexOf(item) < index) --index;
								_modsList.Remove(item);
							}

							// _modsList.Insert(index, name);
							foreach (var item in list) {
								_modsList.Insert(index, item);
								++index;
							}
							ModsList.ItemsSource = _modsList;
					}
				} else {
					// _modsList.Remove(name);
					// _modsList.Add(name);
					foreach (var item in list) {
						_modsList.Remove(item);
					}
					foreach (var item in list) {
						_modsList.Add(item);
					}
					ModsList.ItemsSource = _modsList;
				}
			}
		}

		private void InitialiseAdorner(ListViewItem listViewItem) {
			VisualBrush brush = new VisualBrush(listViewItem);
			_adorner = new DragAdorner((UIElement)listViewItem, listViewItem.RenderSize, brush);
			_adorner.Opacity = 0.5;
			_layer = AdornerLayer.GetAdornerLayer(ModsList as Visual);
			_layer.Add(_adorner);
		}

		private void ModsList_DragLeave(object sender, DragEventArgs e) {
			if (e.OriginalSource == ModsList) {
				System.Windows.Point p = e.GetPosition(ModsList);
				Rect r = VisualTreeHelper.GetContentBounds(ModsList);
				if (!r.Contains(p)) {
					e.Handled = true;
				}
			}
		}

		private void ModsList_DragOver(object sender, DragEventArgs e) {
			if (_adorner != null) {
				_adorner.OffsetLeft = e.GetPosition(Gradient).X - _dragCurrentPosition.X;
				_adorner.OffsetTop = e.GetPosition(Gradient).Y - _dragCurrentPosition.Y;
			}
		}

		#endregion Drag and Drop

		private void ModsList_SelectionChanged(object sender, SelectionChangedEventArgs e) {}

		private void OnModsOrderChanged() {
			FixSuitsMenuItemOrder();
			SaveProfile();
		}

		private void OnModInstallChanged() {
			SaveProfile();
		}

		private void FixSuitsMenuItemOrder() {
			if (!_selectedGameHasSuitsMenu) return;
			if (_suitsMenuEntry == null) return;

			var foundAt = -1;
			for (var i = 0; i < _modsList.Count; ++i) {
				var mod = _modsList[i];
				if (mod == _suitsMenuEntry) {
					foundAt = i;
					mod.Order = _modsList.Count;
					continue;
				}
				if (foundAt != -1) {
					--mod.Order;
				}
			}
			if (foundAt != -1) {
				_modsList.Move(foundAt, _modsList.Count-1);
			}
		}

		private bool _propagatingInstallChanged = false;

		private void OnModPropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (true) { // TODO: make a setting
				if (!_propagatingInstallChanged) {
					_propagatingInstallChanged = true;

					ModEntry changedMod = (ModEntry)sender;
					if (changedMod != null) {
						var items = ModsList.SelectedItems;
						if (items.Contains(changedMod)) {
							foreach (ModEntry item in items) {
								item.Install = changedMod.Install;
							}
						}
					}

					_propagatingInstallChanged = false;
				}
			}

			if (!_propagatingInstallChanged) { // TODO: if OnModInstallChanged() doesn't save profile instantly and just runs a timer, it can be done without this check
				OnModInstallChanged();
			}
		}

		private void SaveProfile() {
			ApplyModsStateToProfile();

			_selectedProfile.Save(); // TODO: run the timer, so we don't write the file multiple times in a row if changes happened quickly
			// TODO: mark as dirty, so we don't forget to force save on switch or quit
		}

		private void ApplyModsStateToProfile() {
			var mods = new List<ModEntry>();
			foreach (var mod in _modsList) {
				mods.Add(new ModEntry(mod.Path, mod.Install, mod.Extras));
			}
			_selectedProfile.Mods = mods;
		}

		private void InstallModsButton_Click(object sender, RoutedEventArgs e) {
			ApplyModsStateToProfile();
			PrepareToInstallMods();
		}

		private void PrepareToInstallMods() {
			var gamePath = _selectedProfile.GamePath;
			var tocPath = _selectedGame.GetTocPath(gamePath);
			var shaFilePath = tocPath + ".sha1";

			var thread = new Thread(() => CheckTocSha(tocPath, shaFilePath));
			_taskThreads.Add(thread);
			thread.Start();
		}

		private void CheckTocSha(string tocPath, string shaFilePath) {
			Dispatcher.Invoke(() => {
				OverlayHeaderLabel.Text = "Checking 'toc'...";
				OverlayOperationLabel.Text = "";
			});

			bool Check() {
				if (!File.Exists(shaFilePath)) {
					return false; // failed to check, proceed as before when there was no file
				}

				var currentSha = Hashes.GetFileSha1(tocPath);

				var rememberedSha = File.ReadAllText(shaFilePath);
				if (rememberedSha.Length < currentSha.Length) {
					return false; // failed to check, proceed as if there was no file
				}

				rememberedSha = rememberedSha[..currentSha.Length];
				if (currentSha.Equals(rememberedSha, StringComparison.OrdinalIgnoreCase)) {
					return false; // check successful, nothing needs to be done
				}

				// hash differs, so 'toc' changed
				// see if there's 'toc.BAK' and what's its hash
				// if it's absent or has the same hash, nothing to suggest
				// but if it has different, this could be due to auto-update, and this is where dialog is needed

				var tocBakPath = tocPath + ".BAK";
				if (!File.Exists(tocBakPath)) {
					return false;
				}

				var backupSha = Hashes.GetFileSha1(tocBakPath);
				if (currentSha.Equals(backupSha, StringComparison.OrdinalIgnoreCase)) {
					return false; // 'toc.BAK' has the same contents, so no sense suggesting user to choose which one to use
				}
				
				var cancelled = false;
				Dispatcher.Invoke(() => {
					var w = new TocMismatchDialog(tocPath, currentSha, tocBakPath, backupSha, _selectedGame);
					cancelled = !(bool)w.ShowDialog();
				});

				return cancelled;
			}

			var cancelled = Check();
			if (cancelled) {
				return;
			}

			Dispatcher.Invoke(StartCollectModsThread);
		}

		private void StartCollectModsThread() {
			var builder = new ModCollectingThreadBuilder(_selectedProfile, _mods);

			builder.OnStart = () => {
				Dispatcher.Invoke(() => {
					OverlayHeaderLabel.Text = "Collecting mods to install...";
					OverlayOperationLabel.Text = "";
				});
			};

			builder.OnOperationStart = (int ndx, int count, string modName) => {
				Dispatcher.Invoke(() => {
					OverlayOperationLabel.Text = $"{ndx}/{count}: '{modName}'...";
				});
			};

			builder.OnException = (string modName, Exception ex) => {
				var message = $"There was an error processing '{modName}' mod.\nPress Ctrl+C to copy this message.\n\n{ex}";
				MessageBoxResult result = MessageBox.Show(message, "Error", MessageBoxButton.OK);
			};

			builder.OnSuccess = (List<ModEntry> modsToInstall) => {
				Dispatcher.Invoke(() => {
					StartInstallModsThread(modsToInstall);
				});
			};

			var thread = builder.Build();
			_taskThreads.Add(thread);
			thread.Start();
		}

		private void StartInstallModsThread(List<ModEntry> modsToInstall, bool uninstalling = false) {
			var builder = new ModInstallingThreadBuilder(_settings, _selectedProfile, modsToInstall, uninstalling);

			builder.OnOperationsStarted = (int operationsCount) => {
				Dispatcher.Invoke(() => {
					if (uninstalling)
						OverlayHeaderLabel.Text = "Uninstalling mods...";
					else
						OverlayHeaderLabel.Text = "Installing mods (0/" + operationsCount + " done)...";
					OverlayOperationLabel.Text = "Loading 'toc.BAK'...";
				});
			};

			builder.OnOperationStarts = (int index, int operationsCount, string modName) => {
				Dispatcher.Invoke(() => {
					OverlayHeaderLabel.Text = "Installing mods (" + index + "/" + operationsCount + " done)...";
					OverlayOperationLabel.Text = "Installing '" + modName + "'...";
				});
			};

			builder.OnOperationsFinalizing = (int index, int operationsCount) => {
				Dispatcher.Invoke(() => {
					if (uninstalling)
						OverlayHeaderLabel.Text = "Uninstalling mods...";
					else
						OverlayHeaderLabel.Text = "Installing mods (" + index + "/" + operationsCount + " done)...";
					OverlayOperationLabel.Text = "Saving 'toc'...";
				});
			};

			builder.OnOperationsFinished = (int operationsCount) => {
				Dispatcher.Invoke(() => {
					if (uninstalling)
						ShowStatusMessage("Done! Mods uninstalled.");
					else
						ShowStatusMessage("Done! " + operationsCount + " mods installed.");

					if (_selectedGameHasSuitsMenu) {
						if (_selectedProfile.Game == GameMSMR.ID) {
							MSMRSuitsMenuContent.RequestReload();
						} else {
							MMSuitsMenuContent.RequestReload();
						}
					}
				});
			};

			builder.OnErrorOccurred_BeforeWritingTrace = () => {
				Dispatcher.Invoke(() => {
					ShowStatusMessageError("Error occurred. See 'errors.log' for details.");
				});
			};

			builder.OnErrorOccurred_AfterTraceSaved = () => {
				if (Settings_OpenErrorLog) {
					Dispatcher.Invoke(() => {
						ShowLatestErrorLogWindow();
					});
				}
			};

			var thread = builder.Build();
			_taskThreads.Add(thread);
			thread.Start();
		}

		private void ShowStatusMessage(string text) {
			StatusMessage.Content = text;
			_statusMessageErrorShown = false;
			PlayAnimation("ShowStatusMessage");
		}

		private void ShowStatusMessageError(string text) {
			StatusMessage.Content = text;
			_statusMessageErrorShown = true;
			PlayAnimation("ShowStatusMessageError");
		}

		private void HideStatusMessageError() {
			_statusMessageErrorShown = false;
			PlayAnimation("HideStatusMessageError");
		}

		private void PlayAnimation(string name) {
			BeginStoryboard((System.Windows.Media.Animation.Storyboard)this.FindResource(name));
		}

		private void ShowLatestErrorLogWindow(bool copyErrorToClipboard = false) {
			var w = new ErrorLogWindow();
			if (copyErrorToClipboard) {
				w.CopyErrorText();
			}
			w.ShowDialog();
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e) {
			RefreshMods(true);
		}

		private void RefreshMods(bool forceSync = false) {
			_mods = ((App)App.Current).ReloadMods(forceSync);
			MakeModsItems();
		}

		private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			ICollectionView view = CollectionViewSource.GetDefaultView(_modsList);

			string filter = FilterTextBox.Text.Trim();
			if (filter == "") {
				view.Filter = null;
			} else {
				string[] words = filter.Split(' ');
				view.Filter = (item) => {
					foreach (var word in words) {
						if (!((ModEntry)item).Name.Contains(word, StringComparison.OrdinalIgnoreCase)) {
							return false;
						}
					}
					return true;
				};
			}
		}

		private void UninstallMods(object sender, RoutedEventArgs e) {
			List<ModEntry> modsToInstall = new List<ModEntry>();
			StartInstallModsThread(modsToInstall, true);
		}

		private void LaunchGame(object sender, RoutedEventArgs e) {
			LaunchGame(false);
		}

		private void LaunchGame(bool modded) {
			try {
				var path = _selectedProfile.GamePath;
				var arguments = "";

				if (modded) {
					if (_selectedProfile.Settings_Scripts_Enabled)
						arguments += "-scripts ";
				}

				Process.Start(new ProcessStartInfo() {
					WorkingDirectory = path,
					FileName = _selectedGame.GetExecutablePath(path),
					Arguments = arguments
				});
			} catch {}
		}

		private void ResetToc(object sender, RoutedEventArgs e) {
			try {
				MessageBoxResult result = MessageBox.Show("Are you sure you want to reset 'toc'?\nYou'd need Steam or EGS to verify files.", "Reset 'toc'", MessageBoxButton.YesNo);
				if (result != MessageBoxResult.Yes) {
					return;
				}

				var path = _selectedProfile.GamePath;
				var exePath = _selectedGame.GetExecutablePath(path);
				var tocPath = _selectedGame.GetTocPath(path);
				var tocBakPath = tocPath + ".BAK";

				try {
					if (File.Exists(exePath)) {
						File.Delete(exePath);
					}
				} catch {
					// return early if .exe is still running, so 'toc' and 'toc.BAK' are intact
					MessageBox.Show("Failed to reset 'toc'!\nMake sure the game isn't running and try again.", "Reset 'toc'", MessageBoxButton.OK);
					return;
				}

				try {
					File.Delete(tocPath);
				} catch {}

				try {
					File.Delete(tocBakPath);
				} catch {}

				if (File.Exists(exePath) || File.Exists(tocPath) || File.Exists(tocBakPath)) {
					// sanity check, warn that something could've been deleted though
					MessageBox.Show("Failed to reset 'toc'!\nMake sure the game isn't running and try again.\nSome files might've been deleted.", "Reset 'toc'", MessageBoxButton.OK);
					return;
				}

				MessageBox.Show("Old 'toc' is successfully removed! Open Steam or EGS and run the game to download the working one.\n\nIn Steam, it'll show you the missing executable warning first. When you press Play again, it'll download the missing files.", "Reset 'toc'", MessageBoxButton.OK);
			} catch {}
		}

		private void AddMods_Click(object sender, RoutedEventArgs e) {
			CommonOpenFileDialog dialog = new CommonOpenFileDialog();
			dialog.Title = "Select mods to add...";
			dialog.Multiselect = true;
			dialog.RestoreDirectory = true;

			var supportedModFiles = "*.smpcmod;*.mmpcmod;*.suit;*.stage;*.modular;*.suit_style";
			if (_selectedProfile.Settings_Scripts_Enabled) {
				supportedModFiles += ";*.script";
			}
			var archives = "*.zip;*.rar;*.7z";
			var allSupportedFiles = supportedModFiles + ";" + archives;

			dialog.Filters.Add(new CommonFileDialogFilter("All supported files", allSupportedFiles) { ShowExtensions = true });
			dialog.Filters.Add(new CommonFileDialogFilter("All supported mod files", supportedModFiles) { ShowExtensions = true });
			dialog.Filters.Add(new CommonFileDialogFilter("Archives", archives) { ShowExtensions = true });
			dialog.Filters.Add(new CommonFileDialogFilter("All files", "*") { ShowExtensions = true });

			if (dialog.ShowDialog() != CommonFileDialogResult.Ok) {
				return;
			}

			List<string> addedFiles = new();
			foreach (var filename in dialog.FileNames) {
				var libraryPath = AddMod(filename);
				if (libraryPath != null) { addedFiles.Add(libraryPath); }
			}

			Dictionary<string, bool> previousMods = new Dictionary<string, bool>();
			foreach (var mod in _mods) {
				previousMods[mod.Path] = true;
			}

			// reload mods
			_mods = ((App)App.Current).ReloadModsOnlyForFiles(addedFiles);
			MakeModsItems();
			SaveProfile();

			var newModsCount = 0;
			foreach (var mod in _mods) {
				if (!previousMods.ContainsKey(mod.Path))
					++newModsCount;
			}

			ShowStatusMessage("Done! " + newModsCount + " mods added.");
		}

		private string AddMod(string filename) {
			bool renameInsteadOverwriting = true; // TODO: make a setting

			try {
				var basename = Path.GetFileName(filename);
				var cwd = Directory.GetCurrentDirectory();
				var path = Path.Combine(cwd, "Mods Library", basename);

				if (renameInsteadOverwriting) {
					var index = 1;
					var name = Path.GetFileNameWithoutExtension(basename);
					var ext = Path.GetExtension(basename);
					while (File.Exists(path)) {
						path = Path.Combine(cwd, "Mods Library", name + " (" + index + ")" + ext);
						++index;

						if (index > 1000) break;
					}
				}

				File.Copy(filename, path, true);
				return path;
			} catch {}

			return null;
		}

		private void Window_KeyUp(object sender, KeyEventArgs e) {
			if (e.Key == Key.PrintScreen || e.SystemKey == Key.PrintScreen) {
				if (_statusMessageErrorShown) {
					ShowLatestErrorLogWindow(true);
				}
			}
		}

		private void ModsList_KeyUp(object sender, KeyEventArgs e) {
			if (e.Key == Key.Delete) {
				var cwd = Directory.GetCurrentDirectory();

				string GetFilePath(string path) {
					var index = path.IndexOf("||");
					if (index != -1) {
						path = path.Substring(0, index);
					}

					return Path.Combine(cwd, "Mods Library", path);
				};

				List<string> filesToDelete = new();
				foreach (ModEntry mod in ModsList.SelectedItems) {
					if (mod.Type == ModEntry.ModType.SUITS_MENU) continue;

					var path = GetFilePath(mod.Path);
					if (!filesToDelete.Contains(path)) {
						filesToDelete.Add(path);
					}
				}

				var deletingModsCount = 0;
				foreach (ModEntry mod in ModsList.Items) {
					if (mod.Type == ModEntry.ModType.SUITS_MENU) continue;

					var path = GetFilePath(mod.Path);
					if (filesToDelete.Contains(path)) {
						++deletingModsCount;
					}
				}

				if (filesToDelete.Count > 0) {
					string message = "Delete " + filesToDelete.Count + " files from 'Mods Library' folder?";
					if (filesToDelete.Count == 1) {
						message = "Delete '" + Path.GetFileName(filesToDelete[0]) + "' from 'Mods Library' folder?";
					}

					if (ModsList.SelectedItems.Count < deletingModsCount) {
						message += $"\nThat'll delete {deletingModsCount} mods (only {ModsList.SelectedItems.Count} of them are selected)!";
					}

					MessageBoxResult result = MessageBox.Show(message, "Warning", MessageBoxButton.YesNo);
					if (result == MessageBoxResult.Yes) {
						foreach (var file in filesToDelete) {
							try { File.Delete(file); } catch {}
						}

						RefreshMods(); // if PreferCachedModsLibrary, mods disappear even without force sync, because cache entries for files not present on disk are ignored
					}
				}
			}
		}

		private void ProfileGamePath_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			try {
				var path = _selectedProfile.GamePath;

				if (e.ChangedButton == MouseButton.Right) {
					path = Directory.GetCurrentDirectory();
				}
				
				Process.Start("explorer.exe", path);
			} catch {}
		}

		private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e.Source == MainTabs) {
				if (MainTabs.SelectedItem == MSMRSuitsMenuTab) {
					MSMRSuitsMenuContent.OnOpen();
				} else if (MainTabs.SelectedItem == MMSuitsMenuTab) {
					MMSuitsMenuContent.OnOpen();
				}
			}
		}

		private void StatusMessage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (!_statusMessageErrorShown) return;

			try {
				Process.Start("notepad.exe", "errors.log");
			} catch {}
			
			HideStatusMessageError();
		}

		private void ModsList_ModEntry_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
			var showMenu = false;

			var grid = (Grid)sender;
			if (grid != null) {
				var mod = (ModEntry)grid.DataContext;
				if (mod != null) {
					showMenu = ModEntry.IsTypeFamilyModular(mod.Type);
				}
			}

			if (!showMenu) {
				e.Handled = true;
				return;
			}
		}

		private void EditModules_Click(object sender, RoutedEventArgs e) {
			var menuItem = (MenuItem)sender;
			if (menuItem == null) return;

			var mod = (ModEntry)menuItem.DataContext;
			if (mod == null) return;

			new ModularWizard(mod, this).ShowDialog();
			SaveProfile();
			MakeModsItems();
		}

		private void ScriptSettings_EnableScripting_Changed(object sender, RoutedEventArgs e) {
			if (!_reactToScriptSettingsChange) return;
			_selectedProfile.Settings_Scripts_Enabled = (bool)ScriptSettings_EnableScripting.IsChecked;
			SaveProfile();

			UpdateRunModdedButtonVisibility();
		}

		private void RunModdedButton_Click(object sender, RoutedEventArgs e) {
			LaunchGame(true);
		}
	}
}
