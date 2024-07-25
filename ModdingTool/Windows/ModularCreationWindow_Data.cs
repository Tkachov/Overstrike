using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace ModdingTool.Windows;

abstract internal class LayoutEntry {}

internal class AddingEntriesButtonsEntry: LayoutEntry {
	public bool IsAddingEntriesButtonsEntry { get => true; } // TODO: have it in all entries so xaml binding warning does not happen?
}

internal class HeaderEntry: LayoutEntry {
	public string Text { get; set; }
}

internal class SeparatorEntry: LayoutEntry {}

internal class ModuleEntry: LayoutEntry {
	public string Name { get; set; }
	public List<ModuleOption> Options = new();

	/*
	public override string Description {
		get {
			var suffix = "";
			if (Options.Count == 0) {
				suffix = "| WARNING: NO OPTIONS!";
			} else if (Options.Count == 1) {
				suffix = "(internal)";
			} else {
				suffix = $"({Options.Count} options)";
			}

			return $"Module: {Name} {suffix}";
		}
	}
	*/
}

class ModuleOption {
	public string _name = "";
	public string _path = "";

	public string Name {
		get {
			if (_name == "") return "(no name)";
			return _name;
		}
		set {
			_name = value;
		}
	}

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
}
