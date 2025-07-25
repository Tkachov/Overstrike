using DAT1.Sections;
using DAT1.Sections.Actor;
using DAT1.Sections.Conduit;
using DAT1.Sections.Generic;
using DAT1.Sections.HibernateZone;
using DAT1.Sections.Level;
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
List<uint> SectionsToSkipLongDescriptionFor = new() { LevelLinkNamesSection.TAG };

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

	Register(typeof(ConduitBuiltSection), "Conduit Built");
	Register(typeof(ConduitReferencesSection), "Conduit Asset Refs");

	Register(typeof(ZoneHibernateObjectsSection), "Zone Hibernate Objects");
	Register(typeof(ZoneHibernateLightNamesSection), "Zone Hibernate Light Names");
	Register(typeof(ZoneHibernateModelAssetsSection), "Zone Hibernate Model Assets");
	Register(typeof(ZoneHibernateVFXAssetsSection), "Zone Hibernate VFX Assets");

	Register(typeof(LevelLinkNamesSection), "Level Link Names");
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
		case 0x35F7AFA5: assetType = "Conduit"; break;
		case 0xA23BC2E8: assetType = "HibernateZone"; break;
		case 0xD3188EE5: assetType = "Level"; break;
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
		if (SectionsToSkipLongDescriptionFor.Contains(tag)) continue;

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

	private static void PrintBytes(byte[] bytes, string indent, string headerPrefix, string headerSuffix) {
		if (bytes.Length > 0) {
			Console.WriteLine($"{indent}{headerPrefix}{bytes.Length}{headerSuffix}:");
			for (var i = 0; i < bytes.Length; i += 16) {
				var byteStr = $"{Convert.ToHexString(bytes, i, 4)}";
				if (i + 4 < bytes.Length) byteStr += $" {Convert.ToHexString(bytes, i + 4, 4)}";
				if (i + 8 < bytes.Length) byteStr += $" {Convert.ToHexString(bytes, i + 8, 4)}";
				if (i + 12 < bytes.Length) byteStr += $" {Convert.ToHexString(bytes, i + 12, 4)}";
				Console.WriteLine($"{indent}{byteStr}");
			}
			Console.WriteLine(indent);
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

		PrintBytes(section.Raw, "    ", "other ", " bytes");
	}

	public static string GetShortSectionDescription(this ConduitBuiltSection section, DAT1.DAT1 asset) {
		return $"Conduit Built";
	}

	public static void PrintLongSectionDescription(this SerializedSection_I30 section, DAT1.DAT1 asset) {
		Console.WriteLine(GetShortSectionDescription(section, asset));
		Console.WriteLine($"    data = {section.Data}");
		Console.WriteLine("    ");
	}

	public static string GetShortSectionDescription(this ConduitReferencesSection section, DAT1.DAT1 asset) {
		return $"Conduit Asset Refs, {section.Entries.Count} refs";
	}

	public static void PrintLongSectionDescription(this ReferencesSection section, DAT1.DAT1 asset) {
		var refs = section.Entries;

		var shortDescription = GetShortSectionDescription(section, asset);
		shortDescription = shortDescription.Substring(0, shortDescription.LastIndexOf(','));
		Console.WriteLine(shortDescription);
		Console.WriteLine($"    {refs.Count} refs:");
		foreach (var reference in refs) {
			var path = asset.GetStringByOffset(reference.AssetPathStringOffset);
			Console.WriteLine($"    {reference.ExtensionHash:X8} {reference.AssetId:X16} \"{path}\"");
		}
		Console.WriteLine("");
	}

	private static void PrintLongSectionDescription_StringOffsets(this UInt32ArraySection section, DAT1.DAT1 asset) {
		var items = section.Values;

		var shortDescription = GetShortSectionDescription(section, asset);
		shortDescription = shortDescription.Substring(0, shortDescription.LastIndexOf(','));
		Console.WriteLine(shortDescription);
		Console.WriteLine($"    {items.Count} items:");
		foreach (var offset in items) {
			var path = asset.GetStringByOffset(offset);
			Console.WriteLine($"    \"{path}\"");
		}
		Console.WriteLine("");
	}

	public static string GetShortSectionDescription(this ZoneHibernateLightNamesSection section, DAT1.DAT1 asset) {
		return $"Zone Hibernate Light Names, {section.Values.Count} items";
	}

	public static void PrintLongSectionDescription(this ZoneHibernateLightNamesSection section, DAT1.DAT1 asset) {
		PrintLongSectionDescription_StringOffsets(section, asset);
	}

	public static string GetShortSectionDescription(this ZoneHibernateModelAssetsSection section, DAT1.DAT1 asset) {
		return $"Zone Hibernate Model Assets, {section.Values.Count} items";
	}

	public static void PrintLongSectionDescription(this ZoneHibernateModelAssetsSection section, DAT1.DAT1 asset) {
		PrintLongSectionDescription_StringOffsets(section, asset);
	}

	public static string GetShortSectionDescription(this ZoneHibernateVFXAssetsSection section, DAT1.DAT1 asset) {
		return $"Zone Hibernate VFX Assets, {section.Values.Count} items";
	}

	public static void PrintLongSectionDescription(this ZoneHibernateVFXAssetsSection section, DAT1.DAT1 asset) {
		PrintLongSectionDescription_StringOffsets(section, asset);
	}

	public static string GetShortSectionDescription(this ZoneHibernateObjectsSection section, DAT1.DAT1 asset) {
		return $"Zone Hibernate Objects";
	}

	public static void PrintLongSectionDescription(this ZoneHibernateObjectsSection section, DAT1.DAT1 asset) {
		void PrintGroups(ref List<ZoneHibernateObjectsSection.ZoneHibernateObjectsGroup> list, string name) {
			if (list.Count == 0) return;

			Console.WriteLine($"    {list.Count} {name}:");
			foreach (var e in list) {
				Console.WriteLine($"        - {e.A:X8} {e.B:X8} {e.C:X8} {e.D:X8} {e.Flags:X8} {e.Count} {e.FirstItemIndex} {e.Count2}");
			}
			Console.WriteLine("    ");
		}

		void PrintItems(ref List<ZoneHibernateObjectsSection.ZoneHibernateObjectsItem> list, string name) {
			if (list.Count == 0) return;

			Console.WriteLine($"    {list.Count} {name}:");
			foreach (var e in list) {
				Console.WriteLine($"        - {e.A:X8} {e.B:X8} {e.C:X8} {e.D:X8} {e.E:X8} {e.F:X8} {e.G:X8} {e.H:X8} {e.I:X8} {e.J:X8}");
			}
			Console.WriteLine("    ");
		}

		Console.WriteLine("Zone Hibernate Objects");
		Console.WriteLine($"    models:");
		Console.WriteLine($"        {section.ModelsHeader.A:X8} {section.ModelsHeader.B:X8} {section.ModelsHeader.C:X8} {section.ModelsHeader.D:X8}");
		Console.WriteLine($"        offset1 = {section.ModelsHeader.Offset1:X8}, groups = {section.ModelsHeader.GroupsCount}");
		Console.WriteLine($"        offset2 = {section.ModelsHeader.Offset2:X8}, items = {section.ModelsHeader.ItemsCount}");
		Console.WriteLine($"    VFX:");
		Console.WriteLine($"        {section.VfxHeader.A:X8} {section.VfxHeader.B:X8} {section.VfxHeader.C:X8} {section.VfxHeader.D:X8}");
		Console.WriteLine($"        offset1 = {section.VfxHeader.Offset1:X8}, groups = {section.VfxHeader.GroupsCount}");
		Console.WriteLine($"        offset2 = {section.VfxHeader.Offset2:X8}, items = {section.VfxHeader.ItemsCount}");
		Console.WriteLine($"    lights:");
		Console.WriteLine($"        {section.LightsHeader.A:X8} {section.LightsHeader.B:X8} {section.LightsHeader.C:X8} {section.LightsHeader.D:X8}");
		Console.WriteLine($"        offset1 = {section.LightsHeader.Offset1:X8}, groups = {section.LightsHeader.GroupsCount}");
		Console.WriteLine($"        offset2 = {section.LightsHeader.Offset2:X8}, items = {section.LightsHeader.ItemsCount}");
		Console.WriteLine("    ");
		Console.WriteLine($"    some offset = {section.OffsetToPayload:X8}, some count = {section.UnknownCount}");
		Console.WriteLine("    ");

		PrintBytes(section.Raw, "    ", "", " bytes");

		PrintGroups(ref section.ModelGroups, "model groups");
		PrintItems(ref section.ModelItems, "model items");

		PrintGroups(ref section.VfxGroups, "VFX groups");
		PrintItems(ref section.VfxItems, "VFX items");

		PrintGroups(ref section.LightGroups, "light groups");
		PrintItems(ref section.LightItems, "light items");
	}

	public static string GetShortSectionDescription(this LevelLinkNamesSection section, DAT1.DAT1 asset) {
		return $"Level Link Names, {section.Values.Count} items";
	}

	public static void PrintLongSectionDescription(this LevelLinkNamesSection section, DAT1.DAT1 asset) {
		PrintLongSectionDescription_StringOffsets(section, asset);
	}
}
