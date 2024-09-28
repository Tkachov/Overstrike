using System.Collections.Generic;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ModdingTool.Windows;

abstract internal class LayoutEntry {
	public virtual bool IsAddingEntriesButtonsEntry { get => false; }
}

internal class AddingEntriesButtonsEntry: LayoutEntry {
	public override bool IsAddingEntriesButtonsEntry { get => true; }
}

internal class HeaderEntry: LayoutEntry {
	public string Text { get; set; }
}

internal class SeparatorEntry: LayoutEntry {}

internal class ModuleEntry: LayoutEntry {
	public string Name { get; set; }
	public List<ModuleOption> Options = new();
	public CompositeCollection OptionsCollection { get; set; } = new();

	public string OptionsDescription {
		get {
			var suffix = "";
			if (Options.Count == 0) {
				//suffix = "| WARNING: won't exist in the file unless more options added!";
			} else if (Options.Count == 1) {
				suffix = "(internal)";
			}

			return $"{Options.Count} options {suffix}";
		}
	}

	public void UpdateOptions() {
		OptionsCollection = new CompositeCollection {
			new CollectionContainer() { Collection = Options }
		};
	}
}

class ModulePath {
	public string Name { get; set; }
	public string Path;
}

class IconPath {
	public string Name { get; set; }
	public string Path;
	public BitmapSource Icon { get; set; }
}

class ModuleOption {
	public string _path = "";

	public string Name { get; set; }

	public string File {
		get {
			if (_path == "") return "(no file)";
			return Path.GetFileName(_path);
		}
		set {
			_path = value;
		}
	}

	public string _iconPath = "";

	public ModularCreationWindow Window;
	public CompositeCollection OptionPathCollection { get => Window.OptionPathCollection; }
	public CompositeCollection OptionIconCollection { get => Window.OptionIconCollection; }

	public ModulePath SelectedPathItem { // surprisingly, this seems to work
		get {
			ModulePath first = null;

			foreach (CollectionContainer container in OptionPathCollection) {
				foreach (ModulePath option in container.Collection) {
					if (first == null) {
						first = option;
					}

					if (option.Path == _path) {
						return option;
					}
				}
			}

			return first;
		}

		set {
			_path = value.Path;
		}
	}

	public IconPath SelectedIconItem {
		get {
			IconPath first = null;

			foreach (CollectionContainer container in OptionIconCollection) {
				foreach (IconPath option in container.Collection) {
					if (first == null) {
						first = option;
					}

					if (option.Path == _iconPath) {
						return option;
					}
				}
			}

			return first;
		}

		set {
			_iconPath = value.Path;
		}
	}
}
