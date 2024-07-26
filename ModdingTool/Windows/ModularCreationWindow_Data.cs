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

	public string IconPath;
	public BitmapSource Icon { get; set; }

	public CompositeCollection OptionPathCollection { get => Window.OptionPathCollection; }
	public ModularCreationWindow Window;
}
