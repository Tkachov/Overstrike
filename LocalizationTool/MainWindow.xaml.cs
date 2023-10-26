// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Files;
using LocalizationTool.Helpers;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Action = LocalizationTool.Helpers.Action;
using Localization = DAT1.Files.Localization;

namespace LocalizationTool {
    public partial class MainWindow : Window {
        // handle hotkeys
        public ICommand FileNewLocalizationCommand { get; set; } = null;
        public ICommand FileLoadLocalizationCommand { get; set; } = null;
        public ICommand FileSaveLocalizationCommand { get; set; } = null;
        public ICommand FileSaveAsLocalizationCommand { get; set; } = null;
        public ICommand FileUndo { get; set; } = null;
        public ICommand FileRedo { get; set; } = null;
        public ICommand LocalizationListRemoveEntry { get; set; } = null;

        // settings
        private List<string> _recentPaths = new();

        // loaded data
        private string _filePath = "";
        private string _fileName = "";
        private string _filePathWithName = "";
        private bool _fileLoaded = false;
        private Localization _localizationRaw = null;
        private ObservableCollection<LocalizationEntry> _displayedLocalizationList = new();

        // new localization action
        private bool _keyValueExist = false;
        private string _newKeyValue = "";
        private string _newStringValue = "";
        private uint _newFlagsValue = 0;

        // undo/redo manager
        private UndoRedoManager _undoRedoManager = null;

        // empty localization
        private static byte[] _emptyLocalization = {
            0xAB, 0xB0, 0x2B, 0x12, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x31, 0x54, 0x41, 0x44, 0xAB, 0xB0, 0x2B, 0x12, 0x10, 0x01, 0x00, 0x00,
            0x09, 0x00, 0x00, 0x00, 0x50, 0x80, 0xA5, 0x06, 0xB0, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
            0xE9, 0xCF, 0xD2, 0x0C, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xBD, 0xCE, 0x73, 0x4D,
            0xF0, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0xB8, 0x82, 0xA3, 0x70, 0x00, 0x01, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0xB2, 0x55, 0xEA, 0xA4, 0xC0, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x43, 0x32, 0x65, 0xB0, 0xE0, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xB5, 0x31, 0x37, 0xC4,
            0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0xA9, 0x40, 0xD5, 0xA0, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0xB4, 0xEE, 0x0D, 0xF8, 0xD0, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x4C, 0x6F, 0x63, 0x61, 0x6C, 0x69, 0x7A, 0x61, 0x74, 0x69, 0x6F, 0x6E, 0x20, 0x42, 0x75, 0x69,
            0x6C, 0x74, 0x20, 0x46, 0x69, 0x6C, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x3E, 0xDE, 0x8B, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x49, 0x4E, 0x56, 0x41, 0x4C, 0x49, 0x44, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };

        public MainWindow() {
            InitializeComponent();

            Closing += new CancelEventHandler(MainWindow_Closing);

            InitializeHotkeys();
            LoadSettings();

            if (_recentPaths.Count > 0) {
                LoadLocalization(_recentPaths[0]);
            }
            else {
                NewLocalization();
            }
        }

        #region init hotkeys

        private void InitializeHotkeys() {
            FileNewLocalizationCommand = new RelayCommand(ExecuteFileNewLocalization);
            FileLoadLocalizationCommand = new RelayCommand(ExecuteFileLoadLocalization);
            FileSaveLocalizationCommand = new RelayCommand(ExecuteFileSaveLocalization);
            FileSaveAsLocalizationCommand = new RelayCommand(ExecuteFileSaveAsLocalization);
            FileUndo = new RelayCommand(ExecuteFileUndo);
            FileRedo = new RelayCommand(ExecuteFileRedo);
            LocalizationListRemoveEntry = new RelayCommand(ExecuteLocalizationListRemoveEntry);
            DataContext = this; // Set the DataContext to enable data binding.
        }

        #endregion
        #region settings

        private void LoadSettings() {
            LoadRecentTxt();
        }

        private void LoadRecentTxt() {
            _recentPaths.Clear();

            var fn = "loc_recent.txt";
            if (File.Exists(fn)) {
                foreach (var line in File.ReadLines(fn)) {
                    if (line == null) continue;

                    var l = line.Trim();
                    if (l != "") _recentPaths.Add(l);
                }
            }
        }

        private void SaveRecentTxt() {
            using var f = File.OpenWrite("loc_recent.txt");
            using var w = new StreamWriter(f);
            foreach (var l in _recentPaths) {
                w.WriteLine(l);
            }
        }

        #endregion
        #region new localization file

        private void NewLocalization() {
            using var r = new BinaryReader(new MemoryStream(_emptyLocalization));
            _localizationRaw = new Localization(r);
            _fileName = "Untitled";
            _filePath = "";
            _filePathWithName = "";
            _undoRedoManager = new UndoRedoManager();
            _undoRedoManager.InitState();
            InitializeUI();
        }

        #endregion
        #region load localization file

        private void LoadLocalization(string path) {
            _fileLoaded = false;
            UpdateEntriesCount();
            _fileName = Path.GetFileNameWithoutExtension(path);
            _filePath = Path.GetDirectoryName(path);
            _filePathWithName = Path.Combine(_filePath, _fileName);
            _localizationRaw = LoadLocalizationFile(path);
            _undoRedoManager = new UndoRedoManager();
            _undoRedoManager.InitState();

            if (_localizationRaw == null) {
                return;
            }
            if (!Directory.Exists(_filePath)) {
                return;
            }

            _recentPaths.Remove(path);
            _recentPaths.Insert(0, path);
            SaveRecentTxt();

            var helper = new LocalizationHelper(_localizationRaw);
            var keys = helper.Keys;
            _displayedLocalizationList.Clear();
            foreach (var key in keys) {
                _displayedLocalizationList.Add(new LocalizationEntry(key, helper.GetValue(key), helper.GetUnknown(key)));
            }

            InitializeUI();
        }

        private void InitializeUI() {
            LocalizationList.Items.SortDescriptions.Add(new SortDescription("Key", ListSortDirection.Ascending));
            LocalizationList.ItemsSource = _displayedLocalizationList;
            LocalizationList.CurrentCellChanged += LocalizationList_CurrentCellChanged;
            File_Edit.Visibility = Visibility.Visible;
            File_Save.Visibility = Visibility.Visible;
            File_SaveAs.Visibility = Visibility.Visible;
            _fileLoaded = true;
            UpdateChangesLabels();
            UpdateEntriesCount();
        }

        private static Localization LoadLocalizationFile(string localizationPath) {
            var locPath = Path.Combine(localizationPath, "localization_all.localization");
            if (!File.Exists(locPath)) {
                locPath = Path.Combine(localizationPath, "localization_all");
            }
            if (!File.Exists(locPath)) {
                locPath = localizationPath;
            }
            if (!File.Exists(locPath)) {
                return null;
            }

            using var f = File.OpenRead(locPath);
            using var r = new BinaryReader(f);

            Localization localization = new(r);
            return localization;
        }

        #endregion
        #region save localization file

        private void SaveLocalization(string path) {
            using var r = new BinaryReader(new MemoryStream(_emptyLocalization));
            Localization localizationRaw = new Localization(r);
            LocalizationHelper helper = new LocalizationHelper(localizationRaw);

            foreach (var row in _displayedLocalizationList) {
                helper.Add(row.Key, row.Value, row.Flags);
            }

            helper.Apply(_localizationRaw);
            var file = _localizationRaw.Save();
            Handle_ClearChangesTracker();

            if (!Directory.Exists(Path.GetDirectoryName(path))) {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            if (Path.GetExtension(path) == "") {
                path += ".localization";
            }

            using FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            fileStream.Write(file, 0, file.Length);

            _fileName = Path.GetFileNameWithoutExtension(path);
            _filePath = Path.GetDirectoryName(path);
            _filePathWithName = Path.Combine(_filePath, _fileName);
            _undoRedoManager.SaveState();
            UpdateChangesLabels();
        }

        #endregion
        #region event handlers
        #region handle cell editing
        private void LocalizationList_CurrentCellChanged(object? sender, EventArgs e) {
            UpdateChangesLabels();
        }

        private void LocalizationList_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) {
            TextBox editedValue = (TextBox)e.EditingElement;
            LocalizationEntry currentCell = (LocalizationEntry)editedValue.DataContext;
            LocalizationEntry oldState = currentCell.DeepCopy();
            LocalizationEntry newState = currentCell.DeepCopy();
            string columnHeader = e.Column.Header.ToString();
            bool changed = false;

            switch (columnHeader) {
                case "Key":
                    if (editedValue.Text != "" && editedValue.Text != currentCell.Key && IsDuplicateKey(editedValue.Text)) {
                        MessageBox.Show("Duplicate Key value is not allowed.");
                        e.Cancel = true;
                    }
                    else {
                        newState.Key = editedValue.Text;
                        changed = true;
                    }
                    break;
                case "Value":
                    if (editedValue.Text != currentCell.Value) {
                        newState.Value = editedValue.Text;
                        changed = true; ;
                    }
                    break;
                case "Flags":
                    if (uint.TryParse(editedValue.Text, out uint flags) && flags != currentCell.Flags) {
                        newState.Flags = flags;
                        changed = true;
                    }
                    break;
            }
            if (changed) {
                _undoRedoManager.AddChange(new Action { OldEntry = oldState, Entry = newState, Type = ActionType.Edit });
            }
        }

        private bool IsDuplicateKey(string editedValue) {
            foreach (LocalizationEntry item in _displayedLocalizationList) {
                if (item.Key == editedValue) {
                    return true;
                }
            }
            return false;
        }
        #endregion
        #region menu

        private void File_SubmenuOpened(object sender, RoutedEventArgs e) {
            File_LoadRecent.Visibility = (_recentPaths.Count > 0 ? Visibility.Visible : Visibility.Collapsed);

            void UpdateItem(MenuItem item, int index) {
                item.Visibility = (_recentPaths.Count > index ? Visibility.Visible : Visibility.Collapsed);
                item.Header = (_recentPaths.Count > index ? _recentPaths[index] : "").Replace("_", "__");
            };

            UpdateItem(File_LoadRecent1, 0);
            UpdateItem(File_LoadRecent2, 1);
            UpdateItem(File_LoadRecent3, 2);
            UpdateItem(File_LoadRecent4, 3);
            UpdateItem(File_LoadRecent5, 4);
        }

        private void File_LoadRecentItem_Click(object sender, RoutedEventArgs e) {
            bool CheckItem(MenuItem item, int index) {
                if (sender == item) {
                    if (_recentPaths.Count > index) {
                        if (_undoRedoManager.IsCleanState) {
                            LoadLocalization(_recentPaths[index]);
                        }
                        else {
                            MessageBoxResult result = HandleUnsavedChangesAlert();
                            if (result == MessageBoxResult.Yes) {
                                SaveLocalization(_filePathWithName);
                                LoadLocalization(_recentPaths[index]);
                            }
                            if (result == MessageBoxResult.No) {
                                LoadLocalization(_recentPaths[index]);
                            }
                        }
                    }
                    return true;
                }
                return false;
            };

            if (CheckItem(File_LoadRecent1, 0)) { }
            else if (CheckItem(File_LoadRecent2, 1)) { }
            else if (CheckItem(File_LoadRecent3, 2)) { }
            else if (CheckItem(File_LoadRecent4, 3)) { }
            else if (CheckItem(File_LoadRecent5, 4)) { }
        }

        #endregion
        #region dialogs

        private void Handle_FileLoad_Dialog() {
            if (!_undoRedoManager.IsCleanState) {
                MessageBoxResult result = HandleUnsavedChangesAlert();
                if (result == MessageBoxResult.Cancel) {
                    return;
                }
                if (result == MessageBoxResult.Yes) {
                    SaveLocalization(_filePathWithName);
                }
            }

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "Select 'localization' to load...";
            dialog.Multiselect = false;
            dialog.RestoreDirectory = true;
            dialog.Filters.Add(new CommonFileDialogFilter("Localization", "localization") { ShowExtensions = true });

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) {
                return;
            }

            LoadLocalization(dialog.FileName);
        }

        private void Handle_File_SaveAsDialog() {
            CommonSaveFileDialog dialog = new CommonSaveFileDialog();
            dialog.Title = "Save 'localization'...";
            dialog.RestoreDirectory = true;
            dialog.AlwaysAppendDefaultExtension = true;
            dialog.DefaultExtension = ".localization";
            dialog.Filters.Add(new CommonFileDialogFilter("Localization", "localization") { ShowExtensions = true });

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) {
                return;
            }

            SaveLocalization(dialog.FileName);
        }

        #endregion
        #region filter handler

        private void LocalizationList_KeyStringValuesFilter(object sender, TextChangedEventArgs e) {
            ICollectionView view = CollectionViewSource.GetDefaultView(_displayedLocalizationList);
            string keyFilterText = KeyValuesFilter.Text.ToLower();
            string stringFilterText = StringValuesFilter.Text.ToLower();

            if (keyFilterText != "" || stringFilterText != "") {
                view.Filter = (item) => {
                    var localizationEntry = (LocalizationEntry)item;
                    return localizationEntry.Key.ToLower().Contains(keyFilterText) &&
                           localizationEntry.Value.ToLower().Contains(stringFilterText);
                };
            }
            else {
                view.Filter = null;
            }

            UpdateEntriesCount();
        }

        #endregion

        private void ShowFlags_Click(object sender, RoutedEventArgs e) {
            ColumnDefinitionCollection cd = AddNewLocalizationRow.ColumnDefinitions;
            if (sender is MenuItem item && !item.IsChecked) {
                FlagsColumn.Visibility = Visibility.Collapsed;
                AddNewFlagsValueInput.Visibility = Visibility.Collapsed;
                cd[2].Width = new GridLength(0, GridUnitType.Star);
            }
            else {
                FlagsColumn.Visibility = Visibility.Visible;
                AddNewFlagsValueInput.Visibility = Visibility.Visible;
                cd[2].Width = new GridLength(0.75, GridUnitType.Star);
            }
        }

        // command handlers

        private void AddNewEntry_Click(object sender, RoutedEventArgs e) {
            _keyValueExist = _displayedLocalizationList.Any(item => item.Key.ToLower() == AddNewKeyValueInput.Text.ToLower());

            if (!_fileLoaded) {
                return;
            }
            if (_newKeyValue == "" && !_keyValueExist) {
                MessageBox.Show("Key value cannot be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (_newKeyValue != "" && _keyValueExist) {
                MessageBox.Show("Key value already exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (_newKeyValue.Length > 0) {
                AddNewEntry();
            }
        }

        private void RemoveEntry(object sender, RoutedEventArgs e) {
            if (LocalizationList.SelectedItem is LocalizationEntry entry) {
                _undoRedoManager.AddChange(new Action { Entry = entry, Type = ActionType.Delete });
                _displayedLocalizationList.Remove(entry);

                UpdateEntriesCount();
                UpdateChangesLabels();
            }
        }

        private void ExecuteFileNewLocalization(object parameter) {
            _displayedLocalizationList = new();
            NewLocalization();
        }

        private void ExecuteFileLoadLocalization(object parameter) {
            Handle_FileLoad_Dialog();
        }

        private void ExecuteFileSaveLocalization(object parameter) {
            if (_filePathWithName != "") {
                SaveLocalization(_filePathWithName);
            }
            else {
                Handle_File_SaveAsDialog();
            }
        }

        private void ExecuteFileSaveAsLocalization(object parameter) {
            Handle_File_SaveAsDialog();
        }

        private void ExecuteFileUndo(object parameter) {
            Action action = _undoRedoManager.Undo();
            if (action != null) {
                HandleChangeCase(action);
            }

            UpdateChangesLabels();
            UpdateEntriesCount();
        }

        private void ExecuteFileRedo(object parameter) {
            Action action = _undoRedoManager.Redo();
            if (action != null) {
                HandleChangeCase(action);
            }

            UpdateChangesLabels();
            UpdateEntriesCount();
        }

        private void ExecuteLocalizationListRemoveEntry(object parameter) {
            RemoveEntry(null, null);
        }

        // actual logic

        private void HandleChangeCase(Action action) {
            LocalizationEntry localizationEntry = action.Entry;
            switch (action.Type) {
                case ActionType.Add:
                    _displayedLocalizationList.Add(localizationEntry);
                    HandleUpdateCollection(localizationEntry);
                    break;
                case ActionType.Edit:
                    foreach (LocalizationEntry entry in _displayedLocalizationList) {
                        if (entry.Key == action.OldEntry.Key) {
                            entry.Key = action.Entry.Key;
                            entry.Value = action.Entry.Value;
                            entry.Flags = action.Entry.Flags;
                            LocalizationList.Items.Refresh();
                            break;
                        }
                    }
                    break;
                case ActionType.Delete:
                    LocalizationEntry entryToRemove = _displayedLocalizationList.FirstOrDefault(entry => entry.Key == localizationEntry.Key);
                    if (entryToRemove != null) {
                        _displayedLocalizationList.Remove(entryToRemove);
                    }
                    break;
            }
        }

        private void HandleUpdateCollection(LocalizationEntry entry) {
            int newIndex = LocalizationList.Items.IndexOf(entry);
            LocalizationList.ScrollIntoView(LocalizationList.Items[newIndex]);
            LocalizationList.SelectedIndex = newIndex;
        }

        private void LocalizationList_SelectedCell(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count != 0) {
                if (e.AddedItems[0] is LocalizationEntry selectedEntry) {
                    if (selectedEntry.IsChanged) {
                        CurrentItem.Text = $"Selected key: *{selectedEntry.Key}";
                    }
                    else {
                        CurrentItem.Text = $"Selected key: {selectedEntry.Key}";
                    }
                }
            }
            else {
                CurrentItem.Text = "Select entry to edit/remove. Press RMB to open Context Menu.";
            }
        }

        private void UpdateEntriesCount() {
            if (_fileLoaded) {
                if (_displayedLocalizationList.Count != LocalizationList.Items.Count && (KeyValuesFilter.Text != "" || StringValuesFilter.Text != "")) {
                    EntriesCount.Text = $"Filtered entries count: {LocalizationList.Items.Count}";
                }
                else {
                    EntriesCount.Text = $"Entries count: {_displayedLocalizationList.Count}";
                }
            }
            else {
                EntriesCount.Text = "";
            }
        }

        private MessageBoxResult HandleUnsavedChangesAlert() {
            MessageBoxResult result = MessageBox.Show($"Would you like to save changes in file {_fileName}?", "Unsaved changes dialog", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            return result;
        }

        private void UpdateChangesLabels() {
            if (_fileLoaded) {
                File_Undo.IsEnabled = _undoRedoManager.UndoStack.Count > 0;
                File_Redo.IsEnabled = _undoRedoManager.RedoStack.Count > 0;
                Title = (!_undoRedoManager.IsCleanState ? "*" : "") + $"{_fileName} - Localization Tool";
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e) {
            if (!_undoRedoManager.IsCleanState) {
                MessageBoxResult result = HandleUnsavedChangesAlert();
                if (result == MessageBoxResult.Yes) {
                    SaveLocalization(_filePathWithName);
                    e.Cancel = false;
                }
                if (result == MessageBoxResult.Cancel) {
                    e.Cancel = true;
                }
                if (result == MessageBoxResult.No) {
                    e.Cancel = false;
                }
            }
        }

        private void Handle_ClearChangesTracker() {
            foreach (LocalizationEntry item in _displayedLocalizationList) {
                item.Clear();
            }
        }

        #region handle adding new localization entry

        private void AddNewEntry() {
            LocalizationEntry newEntry = new LocalizationEntry(_newKeyValue, _newStringValue, _newFlagsValue);

            _undoRedoManager.AddChange(new Action { Entry = newEntry, Type = ActionType.Add });
            _displayedLocalizationList.Add(newEntry);

            HandleUpdateCollection(newEntry);

            AddNewKeyValueInput.Text = "";
            AddNewStringValueInput.Text = "";
            AddNewFlagsValueInput.Text = "";

            UpdateChangesLabels();
            UpdateEntriesCount();
        }

        private void NewKeyValue_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox tb = (TextBox)e.OriginalSource;
            string input = AddNewKeyValueInput.Text;
            if (input != "") {
                AddNewKeyValueInput.Text = input.ToUpper();
                _keyValueExist = _displayedLocalizationList.Any(item => item.Key.ToLower() == input.ToLower());
                if (_keyValueExist) {
                    tb.Foreground = Brushes.Red;
                }
                else {
                    tb.Foreground = Brushes.Black;
                    _newKeyValue = input;
                }
            }
        }

        private void NewStringValue_TextChanged(object sender, TextChangedEventArgs e) {
            _newStringValue = AddNewStringValueInput.Text;
        }

        private void NewFlagsValue_TextChanged(object sender, TextChangedEventArgs e) {
            string input = AddNewFlagsValueInput.Text;
            if (input != "") {
                uint flags;
                if (uint.TryParse(input, out flags)) {
                    _newFlagsValue = flags;
                }
            }
            else {
                _newFlagsValue = 0;
            }
        }

        #endregion

        #endregion
    }
}
