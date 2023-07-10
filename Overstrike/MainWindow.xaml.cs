using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Overstrike {
	public partial class MainWindow: Window {
		private ObservableCollection<ModEntry> thumbnailListData = new ObservableCollection<ModEntry>();
		private Point _startPoint;
		private DragAdorner _adorner;
		private AdornerLayer _layer;
		private bool _dragIsOutOfScope = false;
		private Point position;

		private AppSettings _settings;
		private List<Profile> _profiles;
		private List<ModEntry> _mods;
		private Profile _selectedProfile;

		private class ProfileItem {
			public string Text { get; set; }
			public Profile Profile { get; set; }
		}

		private ObservableCollection<ProfileItem> _profilesItems = new ObservableCollection<ProfileItem>();

		public MainWindow(AppSettings settings, List<Profile> profiles, List<ModEntry> mods) {
			InitializeComponent();

			_settings = settings;
			_profiles = profiles;
			_mods = mods;

			MakeProfileItems();
			FirstSwitchToProfile();
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

			MakeModsItems();
		}

		private void SetupBanner() {
			// TODO: cache those images once and just set references instead of reloading them all the time
			if (_selectedProfile.Game == "MSMR") {
				GradientImage.Source = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.banner_msmr_back);
				LogoImage.Source = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.banner_msmr_logo);
				LogoImage2.Source = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.banner_msmr_logo2);
			} else {
				GradientImage.Source = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.banner_mm_back);
				LogoImage.Source = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.banner_mm_logo);
				LogoImage2.Source = Utils.Imaging.ConvertToBitmapImage(Properties.Resources.banner_mm_logo2);
			}
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
				if (!IsModCompatible(mod.Type)) continue;
				availableMods[mod.Path] = mod;
			}

			thumbnailListData.Clear();

			var index = 1;
			foreach (var mod in _selectedProfile.Mods) { // first, adding previously known mods
				if (availableMods.ContainsKey(mod.Path)) {
					var install = mod.Install && profileInstalled[mod.Path];
					thumbnailListData.Add(new ModEntry(availableMods[mod.Path], install, index));
					++index;
				}
			}
			foreach (var mod in _mods) {
				if (availableMods.ContainsKey(mod.Path) && !profileInstalled.ContainsKey(mod.Path)) { // then, adding new mods
					thumbnailListData.Add(new ModEntry(availableMods[mod.Path], mod.Install, index));
					++index;
				}
			}

			foreach (var mod in thumbnailListData) {
				mod.PropertyChanged += OnModPropertyChanged;
			}

			NoteThumbnailList.ItemsSource = new CompositeCollection {
				new CollectionContainer() { Collection = thumbnailListData }
			};
		}

		private bool IsModCompatible(ModEntry.ModType type) {
			if (_selectedProfile.Game == Profile.GAME_MSMR) {
				return (type == ModEntry.ModType.SMPC || type == ModEntry.ModType.SUIT_MSMR);
			}

			if (_selectedProfile.Game == Profile.GAME_MM) {
				return (type == ModEntry.ModType.MMPC || type == ModEntry.ModType.SUIT_MM || type == ModEntry.ModType.SUIT_MM_V2);
			}

			return false;
		}

		private void ProfileComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
			if (e.AddedItems.Count <= 0) return;
			Debug.Assert(ProfileComboBox.SelectedItem != null);

			// TODO: check if profile needs saving, and save it (or ask to save?)

			ProfileItem item = (ProfileItem)e.AddedItems[0];
			if (item.Profile == null) {
				var window = new CreateProfile();
				window.ShowDialog();

				var p = window.GetProfile();
				if (p != null) {
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
			Debug.Assert(_profiles.Count > 0);

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

		#region SheetList Drag and Drop

		private void NoteThumbnailList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			_startPoint = e.GetPosition(null);
		}

		private void NoteThumbnailList_MouseMove(object sender, MouseEventArgs e) {
			if (e.LeftButton == MouseButtonState.Pressed) {
				position = e.GetPosition(null);

				if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
					Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance) {
					BeginDrag(e);
				}
			}
		}

		private void BeginDrag(MouseEventArgs e) {

			ListView listView = this.NoteThumbnailList;
			ListViewItem listViewItem =
				FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);

			if (listViewItem == null)
				return;

			// get the data for the ListViewItem
			ModEntry name = listView.ItemContainerGenerator.ItemFromContainer(listViewItem) as ModEntry;
			int indexBefore = thumbnailListData.IndexOf(name);

			//setup the drag adorner.
			InitialiseAdorner(listViewItem);

			//add handles to update the adorner.
			NoteThumbnailList.PreviewDragOver += NoteThumbnailList_DragOver;
			NoteThumbnailList.DragLeave += NoteThumbnailList_DragLeave;
			NoteThumbnailList.DragEnter += NoteThumbnailList_DragEnter;


			var selitems = NoteThumbnailList.SelectedItems;
			List<ModEntry> list = new List<ModEntry>();
			foreach (ModEntry entry in selitems) {
				list.Add(entry);
			}
			list.Sort((x, y) => thumbnailListData.IndexOf(x) - thumbnailListData.IndexOf(y));

			DataObject data = new DataObject("dataFormat", list); // name);
			DragDropEffects de = DragDrop.DoDragDrop(this.NoteThumbnailList, data, DragDropEffects.Move);

			//cleanup
			NoteThumbnailList.PreviewDragOver -= NoteThumbnailList_DragOver;
			NoteThumbnailList.DragLeave -= NoteThumbnailList_DragLeave;
			NoteThumbnailList.DragEnter -= NoteThumbnailList_DragEnter;

			if (_adorner != null) {
				AdornerLayer.GetAdornerLayer(listView).Remove(_adorner);
				_adorner = null;
			}

			// SuitDeselected();
			//SuitSelected(name);
			listView.SelectedItem = name; // listViewItem;

			int indexAfter = thumbnailListData.IndexOf(name);
			// +TODO [REQUIRED]: determine if order changed => raise hasChanges
			if (indexBefore != indexAfter) {
				///hasChanges = true;
			}

			List<int> indexes = new List<int>();
			int index = 0;
			foreach (var item in thumbnailListData) {
				if (item.Order != index+1) {
					indexes.Add(index);
				}
				++index;
			}

			if (indexes.Count > 0) {
				foreach (var index2 in indexes) {
					var item = thumbnailListData[index2];
					item.Order = index2 + 1;
					thumbnailListData.RemoveAt(index2);
					thumbnailListData.Insert(index2, item);
				}
				NoteThumbnailList.ItemsSource = thumbnailListData;
			}

			OnModsOrderChanged();
		}

		private void NoteThumbnailList_DragEnter(object sender, DragEventArgs e) {
			if (!e.Data.GetDataPresent("dataFormat") ||
				sender == e.Source) {
				e.Effects = DragDropEffects.None;
			}
		}

		private void NoteThumbnailList_Drop(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent("dataFormat")) {
				//ModEntry name = e.Data.GetData("dataFormat") as ModEntry;
				IList<ModEntry> list = e.Data.GetData("dataFormat") as IList<ModEntry>;
				ListViewItem listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);

				if (listViewItem != null) {
					ModEntry nameToReplace = NoteThumbnailList.ItemContainerGenerator.ItemFromContainer(listViewItem) as ModEntry;
					int index = NoteThumbnailList.Items.IndexOf(nameToReplace);

					if (index >= 0) {

							// thumbnailListData.Remove(name);
							foreach (var item in list) {
								if (thumbnailListData.IndexOf(item) < index) --index;
								thumbnailListData.Remove(item);
							}

							// thumbnailListData.Insert(index, name);
							foreach (var item in list) {
								thumbnailListData.Insert(index, item);
								++index;
							}
							NoteThumbnailList.ItemsSource = thumbnailListData;
					}
				} else {
					// thumbnailListData.Remove(name);
					// thumbnailListData.Add(name);
					foreach (var item in list) {
						thumbnailListData.Remove(item);
					}
					foreach (var item in list) {
						thumbnailListData.Add(item);
					}
					NoteThumbnailList.ItemsSource = thumbnailListData;
				}
			}
		}

		private void InitialiseAdorner(ListViewItem listViewItem) {
			VisualBrush brush = new VisualBrush(listViewItem);
			_adorner = new DragAdorner((UIElement)listViewItem, listViewItem.RenderSize, brush);
			_adorner.Opacity = 0.5;
			_layer = AdornerLayer.GetAdornerLayer(NoteThumbnailList as Visual);
			_layer.Add(_adorner);
		}

		private void NoteThumbnailList_DragLeave(object sender, DragEventArgs e) {
			if (e.OriginalSource == NoteThumbnailList) {
				System.Windows.Point p = e.GetPosition(NoteThumbnailList);
				Rect r = VisualTreeHelper.GetContentBounds(NoteThumbnailList);
				if (!r.Contains(p)) {
					this._dragIsOutOfScope = true;
					e.Handled = true;
				}
			}
		}

		private void NoteThumbnailList_DragOver(object sender, DragEventArgs e) {
			if (_adorner != null) {
				//_adorner.OffsetLeft = e.GetPosition(NoteThumbnailList).X - position.X;
				//_adorner.OffsetTop = e.GetPosition(NoteThumbnailList).Y - position.Y;

				_adorner.OffsetLeft = e.GetPosition(Gradient).X - position.X;
				_adorner.OffsetTop = e.GetPosition(Gradient).Y - position.Y;
			}
		}

		// Helper to search up the VisualTree
		private static T FindAnchestor<T>(DependencyObject current)
			where T : DependencyObject {
			do {
				if (current is T) {
					return (T)current;
				}
				current = VisualTreeHelper.GetParent(current);
			}
			while (current != null);
			return null;
		}

		#endregion SheetList Drag and Drop

		private void NoteThumbnailList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (NoteThumbnailList.SelectedItems.Count > 0) {

			}
			if (e.AddedItems.Count > 0) {
				///SuitSelected((ModEntry)e.AddedItems[0]);
			} else {
				///SuitDeselected();
			}
		}

		private void RefreshThumbnail(ModEntry selectedThumb) {


			var i = thumbnailListData.IndexOf(selectedThumb);
			thumbnailListData.RemoveAt(i);
			thumbnailListData.Insert(i, selectedThumb);

			NoteThumbnailList.SelectedItem = selectedThumb;
		}

		private void OnModsOrderChanged() {
			SaveProfile();
		}

		private void OnModInstallChanged() {
			SaveProfile();
		}

		private void OnModPropertyChanged(object? sender, PropertyChangedEventArgs e) {
			OnModInstallChanged();
		}

		private void SaveProfile() {
			ApplyModsStateToProfile();

			_selectedProfile.Save(); // TODO: run the timer, so we don't write the file multiple times in a row if changes happened quickly
			// TODO: mark as dirty, so we don't forget to force save on switch or quit
		}

		private void ApplyModsStateToProfile() {
			var mods = new List<ModEntry>();
			foreach (var mod in thumbnailListData) {
				mods.Add(new ModEntry(mod.Path, mod.Install));
			}
			_selectedProfile.Mods = mods;
		}

		private void InstallModsButton_Click(object sender, RoutedEventArgs e) {
			ApplyModsStateToProfile();

			Dictionary<string, ModEntry> availableMods = new Dictionary<string, ModEntry>();
			foreach (var mod in _mods) {
				availableMods.Add(mod.Path, mod);
			}

			// TODO: show progressbar or something
			// TODO: run in a separate thread
			PrepareToInstallMods();

			var tocPath = Path.Combine(_selectedProfile.GamePath, "asset_archive", "toc");
			var toc = new TOC();
			toc.Load(tocPath);

			var index = 0;
			foreach (var mod in _selectedProfile.Mods) {
				if (!mod.Install) continue;
				if (!availableMods.ContainsKey(mod.Path)) continue; // TODO: should not happen?
				InstallMod(availableMods[mod.Path], toc, index++);
			}

			toc.Save(tocPath);
			// TODO: hide progressbar / show "done" message
		}

		private void PrepareToInstallMods() {
			var tocPath = Path.Combine(_selectedProfile.GamePath, "asset_archive", "toc");
			var tocBakPath = Path.Combine(_selectedProfile.GamePath, "asset_archive", "toc.BAK");

			if (!File.Exists(tocBakPath)) {
				File.Copy(tocPath, tocBakPath);
			} else {
				File.Copy(tocBakPath, tocPath, true);
			}

			var modsPath = Path.Combine(_selectedProfile.GamePath, "asset_archive", "mods");
			if (!Directory.Exists(modsPath)) {
				Directory.CreateDirectory(modsPath);
			}

			// TODO: suits
		}

		private void InstallMod(ModEntry mod, TOC toc, int index) {
			if (mod.Type == ModEntry.ModType.SMPC || mod.Type == ModEntry.ModType.MMPC) {
				InstallSMPCMod(mod, toc, index);
			}
		}

		private void InstallSMPCMod(ModEntry mod, TOC toc, int index) {
			var modsPath = Path.Combine(_selectedProfile.GamePath, "asset_archive", "mods");
			var modPath = Path.Combine(modsPath, "mod" + index);

			var newArchiveIndex = toc.AddNewArchive("mods\\mod" + index, TOC.ArchiveAddingImpl.SMPCTOOL); // TODO: switch to DEFAULT, it must be working fine

			var f = File.OpenWrite(modPath);
			var w = new BinaryWriter(f);

			var cwd = Directory.GetCurrentDirectory();
			var zipPath = Path.Combine(cwd, "Mods Library", mod.Path);
			using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Read)) {
				foreach (ZipArchiveEntry entry in zip.Entries) {
					if (entry.FullName.StartsWith("ModFiles/", StringComparison.OrdinalIgnoreCase)) {
						string[] parts = entry.FullName.Substring(9).Split("_");
						if (parts.Length == 2) {
							int archiveIndex = int.Parse(parts[0]);
							ulong assetId = ulong.Parse(parts[1], NumberStyles.HexNumber);

							long archiveOffset = w.BaseStream.Position;
							using (var stream = entry.Open()) {
								stream.CopyTo(w.BaseStream);
							}
							long fileSize = w.BaseStream.Position - archiveOffset;

							AssetEntry[] assetEntries = toc.FindAssetEntriesById(assetId);
							foreach (var assetEntry in assetEntries) {
								if (assetEntry.archive == archiveIndex) {									
									toc.UpdateAssetEntry(new AssetEntry() {
										index = assetEntry.index,
										id = assetEntry.id,
										archive = newArchiveIndex,
										offset = (uint)archiveOffset,
										size = (uint)fileSize
									});
									break;
								}
							}
						}
					}
				}
			}

			w.Close();
			w.Dispose();
		}
	}
}
