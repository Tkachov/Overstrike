using DAT1.Sections;
using DAT1.Sections.Config;
using DAT1.Sections.Localization;
using OverstrikeShared.STG;
using System.Reflection;

if (args.Length == 0) {
	Console.WriteLine("Asset Info");
	Console.WriteLine("");
	Console.WriteLine("Usage:");
	Console.WriteLine("info.exe <input filename>");
	Console.WriteLine("");
	Console.WriteLine("Shows info about asset's sections.");
	Console.WriteLine("");
	Console.WriteLine("Press Enter to close.");
	Console.ReadLine();
	return;
}

var input = args[0];
Dictionary<uint, KnownSectionsRegistryEntry> SectionsRegistry = new();

try {
	RegisterKnownSections();
	Main(input);
} catch (Exception e) {
	Console.WriteLine("Exception happened:");
	Console.WriteLine(e);
	Console.WriteLine("");
	Console.WriteLine("Press Enter to close.");
	Console.ReadLine();
}

//

void RegisterKnownSections() {
	void Register(Type type, string name) {
		var fieldInfo = type.GetField("TAG", BindingFlags.Public | BindingFlags.Static);
		if (fieldInfo == null) return;
		
		uint tag = (uint)fieldInfo.GetValue(null);
		SectionsRegistry[tag] = new KnownSectionsRegistryEntry() { Name = name, Class = type };
	}

	void RegisterName(uint tag, string name) {
		SectionsRegistry[tag] = new KnownSectionsRegistryEntry() { Name = name, Class = null };
	}

	Register(typeof(ActorBuiltSection), "Actor Built");
	Register(typeof(ActorPriusBuiltSection), "Actor Prius Built");
	Register(typeof(ActorReferencesSection), "Actor Asset Refs");
	Register(typeof(ActorObjectBuiltSection), "Actor Object Built");
	RegisterName(0x6D4301EF, "Actor Prius Built Data");
}

void Main(string input) {
	Console.WriteLine($"> \"{input}\"");
	Console.WriteLine($"    ");

	var magic = ReadMagic(input);
	var isSTG = (magic[0] == 'S' && magic[1] == 'T' && magic[2] == 'G');
	var isDAT1 = (magic[0] == '1' && magic[1] == 'T' && magic[2] == 'A' && magic[3] == 'D');
	
	if (!isSTG && !isDAT1) {
		Console.WriteLine($"Unknown magic: {Convert.ToHexString(magic)}");
		return;
	}

	var asset = new STG();
	asset.Load(input);

	if (isSTG) {
		PrintSTG(asset);
	}

	PrintDAT1(asset);
	PrintAsset(asset);
}

//

byte[] ReadMagic(string filename) {
	using var f = File.OpenRead(input);

	var buf = new byte[4];
	f.Read(buf);
	return buf;
}

void PrintSTG(STG asset) {
	Console.WriteLine($"STG");

	if (asset.HasFlag(STG.Flags.INSTALL_HEADER)) {
		Console.WriteLine($"    toc header ({asset.RawHeader.Length} bytes)");
		Console.WriteLine($"        magic = {asset.Header.Magic:X8}, unknown = {asset.Header.Unknown}");

		if (asset.Header.Pairs.Count > 0) {
			Console.WriteLine($"        ");
			Console.WriteLine($"        {asset.Header.Pairs.Count} pairs:");
			foreach (var pair in asset.Header.Pairs) {
				Console.WriteLine($"        - {pair.A:X8} {pair.B:X8}");
			}
		}

		if (asset.Header.Extra.Length > 0) {
			Console.WriteLine($"        ");
			Console.WriteLine($"        extra {asset.Header.Extra.Length} bytes: {Convert.ToHexString(asset.Header.Extra)}");
		}

		Console.WriteLine($"    ");
	}

	if (asset.HasFlag(STG.Flags.INSTALL_TEXUTRE_META)) {
		Console.WriteLine($"    TODO: texture meta");
		Console.WriteLine($"    ");
	}

	if (!asset.HasFlag(STG.Flags.INSTALL_HEADER) && !asset.HasFlag(STG.Flags.INSTALL_TEXUTRE_META)) {
		Console.WriteLine($"    ");
	}
}

void PrintDAT1(STG asset) {
	Console.WriteLine($"DAT1");
	Console.WriteLine($"    type/version = {asset.Dat1.TypeMagic:X8}");

	var unk = asset.Dat1.GetUnknowns();
	if (unk.Length > 0) {
		Console.WriteLine($"    ");
		Console.WriteLine($"    unknowns ({unk.Length} bytes): {Convert.ToHexString(unk)}");
	}

	var tags = asset.Dat1.GetSectionTags();
	Console.WriteLine($"    ");
	Console.WriteLine($"    {tags.Count} sections:");
	foreach (var tag in tags) {
		Console.WriteLine($"    - {tag:X8} ({asset.Dat1.GetRawSection(tag).Length} bytes)");
	}

	Console.WriteLine($"    ");
}

void PrintAsset(STG asset) {
	var assetType = $"Unknown {asset.Dat1.TypeMagic:X8}";
	switch (asset.Dat1.TypeMagic) {
		case 0x521BEEB8: assetType = "Actor"; break;
	}

	Console.WriteLine(assetType);

	var tags = asset.Dat1.GetSectionTags();
	Console.WriteLine($"    {tags.Count} sections:");
	foreach (var tag in tags) {
		var suffix = GetShortSectionDescription(tag, asset.Dat1);
		if (suffix != "")
			suffix = "\t-- " + suffix;

		Console.WriteLine($"    - {tag:X8} ({asset.Dat1.GetRawSection(tag).Length} bytes){suffix}");
	}
	Console.WriteLine($"    ");

	foreach (var tag in tags) {
		PrintLongSectionDescription(tag, asset.Dat1);
	}
}

//

static Section GetSection(DAT1.DAT1 dat1, uint tag, Type type) {
	var raw = dat1.GetRawSection(tag);
	var section = (Section)Activator.CreateInstance(type);
	section.Load(raw, dat1);
	return section;
}

string GetShortSectionDescription(uint tag, DAT1.DAT1 asset) {
	if (SectionsRegistry.TryGetValue(tag, out KnownSectionsRegistryEntry entry)) {
		if (entry.Class == null) {
			return entry.Name;
		}

		var section = GetSection(asset, tag, entry.Class);
		var desc = section.GetShortSectionDescription(asset);
		return (desc ?? entry.Name);
	}

	return "";
}

void PrintLongSectionDescription(uint tag, DAT1.DAT1 asset) {
	if (SectionsRegistry.TryGetValue(tag, out KnownSectionsRegistryEntry entry)) {
		if (entry.Class == null) {
			return;
		}

		var section = GetSection(asset, tag, entry.Class);
		section.PrintLongSectionDescription(asset);
	}
}

struct KnownSectionsRegistryEntry {
	public string Name;
	public Type Class;
}

static class SectionExtensions {
	public static string GetShortSectionDescription(this Section section, DAT1.DAT1 asset) {
		Type type = typeof(SectionExtensions);
		var method = type.GetMethod("GetShortSectionDescription", new Type[] { section.GetType(), typeof(DAT1.DAT1) });
		if (method != null) {
			var info = method.GetParameters();
			if (info.Length > 0 && info[0].ParameterType == typeof(Section)) { // found itself
				return null;
			}

			return (string)method.Invoke(null, new object[] { section, asset });
		}

		return null;
	}

	public static void PrintLongSectionDescription(this Section section, DAT1.DAT1 asset) {
		Type type = typeof(SectionExtensions);
		var method = type.GetMethod("PrintLongSectionDescription", new Type[] { section.GetType(), typeof(DAT1.DAT1) });
		if (method != null) {
			var info = method.GetParameters();
			if (info.Length > 0 && info[0].ParameterType == typeof(Section)) { // found itself
				return;
			}

			method.Invoke(null, new object[] { section, asset });
		}
	}

	//

	public static string GetShortSectionDescription(this ActorBuiltSection section, DAT1.DAT1 asset) {
		var path = asset.GetStringByOffset(section.StringOffset);
		if (path.Length > 30) path = path.Substring(0, 30) + "...";
		return $"Actor Built, \"{path}\"";
	}

	public static void PrintLongSectionDescription(this ActorBuiltSection section, DAT1.DAT1 asset) {
		var path = asset.GetStringByOffset(section.StringOffset);

		Console.WriteLine("Actor Built");
		Console.WriteLine($"    path = \"{path}\"");
		Console.WriteLine("");
	}

	public static string GetShortSectionDescription(this ActorReferencesSection section, DAT1.DAT1 asset) {
		return $"Actor Asset Refs, {section.Entries.Count} refs";
	}

	public static void PrintLongSectionDescription(this ActorReferencesSection section, DAT1.DAT1 asset) {
		var refs = section.Entries;

		Console.WriteLine("Actor Asset Refs");
		Console.WriteLine($"    {refs.Count} refs:");
		foreach (var reference in refs) {
			var path = asset.GetStringByOffset(reference.AssetPathStringOffset);
			Console.WriteLine($"    {reference.ExtensionHash:X8} {reference.AssetId:X16} \"{path}\"");
		}
		Console.WriteLine("");
	}

	public static string GetShortSectionDescription(this ActorPriusBuiltSection section, DAT1.DAT1 asset) {
		return $"Actor Prius Built, {section.Values.Count} entries";
	}

	public static void PrintLongSectionDescription(this ActorPriusBuiltSection section, DAT1.DAT1 asset) {
		var entries = section.Values;
		var priusData = asset.Section<ActorPriusBuiltDataSection>(ActorPriusBuiltDataSection.TAG);

		Console.WriteLine("Actor Prius Built");
		Console.WriteLine($"    {entries.Count} entries:");
		for (var i = 0; i < entries.Count; ++i) {
			var entry = entries[i];
			var path = asset.GetStringByOffset(entry.StringOffset);

			Console.WriteLine($"    - {i,3}: {entry.Unknown0:X16} {entry.StringHash:X8} \"{path}\"");
			Console.WriteLine($"           offset={entry.Offset} size={entry.Size} unk16={entry.Unknown16:X8} unk28={entry.Unknown28:X8}");
			if (entry.Size > 0) {
				var json = priusData.GetData(asset, (int)entry.Offset, (int)entry.Size);
				Console.WriteLine($"           data={json}");
			}
			Console.WriteLine("    ");
		}
	}

	public static void PrintLongSectionDescription(this ActorObjectBuiltSection section, DAT1.DAT1 asset) {
		Console.WriteLine("Actor Object Built");
		Console.WriteLine($"    matrix:");
		for (var row = 0; row < 4; ++row) {
			Console.WriteLine($"    | {section.Matrix[row, 0],10} {section.Matrix[row, 1],10} {section.Matrix[row, 2],10} {section.Matrix[row, 3],10} |");
		}
		Console.WriteLine("    ");
		Console.WriteLine($"    zeroes = {Convert.ToHexString(section.Zeroes64)}");
		Console.WriteLine($"    type = {section.Type:X8}");
		Console.WriteLine($"    zeroes = {Convert.ToHexString(section.Zeroes96)}");
		Console.WriteLine($"    X, Y, Z = ({section.X}, {section.Y}, {section.Z})");
		Console.WriteLine($"    section size = {section.SectionSize} (real size = {asset.GetRawSection(ActorObjectBuiltSection.TAG).Length})");
		Console.WriteLine("    ");

		var unknownBytes = section.Raw;
		if (unknownBytes.Length > 0) {
			Console.WriteLine($"    other {unknownBytes.Length} bytes:");
			for (var i = 0; i < unknownBytes.Length; i += 16) {
				var byteStr = $"{Convert.ToHexString(unknownBytes, i, 4)}";
				if (i + 4 < unknownBytes.Length) byteStr += $" {Convert.ToHexString(unknownBytes, i + 4, 4)}";
				if (i + 8 < unknownBytes.Length) byteStr += $" {Convert.ToHexString(unknownBytes, i + 8, 4)}";
				if (i + 12 < unknownBytes.Length) byteStr += $" {Convert.ToHexString(unknownBytes, i + 12, 4)}";
				Console.WriteLine($"    {byteStr}");
			}
			Console.WriteLine("    ");
		}
	}
}
