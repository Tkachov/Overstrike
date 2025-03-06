// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Globalization;

if (args.Length == 0) {
	Console.WriteLine("Config Converter");
	Console.WriteLine("");
	Console.WriteLine("Usage:");
	Console.WriteLine("converter.exe <input filename> [output filename]");
	Console.WriteLine("");
	Console.WriteLine("Converts input (either .config or extracted .json) to the opposite, saving to output.");
	Console.WriteLine("");
	Console.WriteLine("Press Enter to close.");
	Console.ReadLine();
	return;
}

var input = args[0];

var output = input + ".out";
if (input.EndsWith(".config")) {
	output = input.Replace(".config", ".json");
} else if (input.EndsWith(".json")) {
	output = input.Replace(".json", ".config");
}
if (args.Length > 1) {
	output = args[1];
}

try {
	var magic = ReadMagic(input);
	var isSTG = (magic[0] == 'S' && magic[1] == 'T' && magic[2] == 'G');
	var isDAT1 = (magic[0] == '1' && magic[1] == 'T' && magic[2] == 'A' && magic[3] == 'D');
	if (isSTG || isDAT1) {
		ConfigToJson(input, output);
	} else {
		JsonToConfig(input, output);
	}
} catch (Exception e) {
	Console.WriteLine("Exception happened:");
	Console.WriteLine(e);
	Console.WriteLine("");
	Console.WriteLine("Press Enter to close.");
	Console.ReadLine();
}

//

byte[] ReadMagic(string filename) {
	using var f = File.OpenRead(input);

	var buf = new byte[4];
	f.Read(buf);
	return buf;
}

void ConfigToJson(string input, string output) {
	var config = new OverstrikeShared.STG.Files.Config();
	config.Load(input);

	var json = new JObject {
		["TYPE"] = config.TypeSection.Data,
		["DATA"] = config.ContentSection.Data,
	};

	if (config.Dat1.HasSection(DAT1.Sections.Config.ConfigReferencesSection.TAG)) {
		var array = new JArray();
		foreach (var refEntry in config.ReferencesSection.Values) {
			array.Add(config.Dat1.GetStringByOffset(refEntry.AssetPathStringOffset));
		}
		json["REFS"] = array;
	}

	File.WriteAllText(output, JsonToString(json));
}

string JsonToString(JObject json) {
	using var sw = new StringWriter(CultureInfo.InvariantCulture);
	using var jw = new JsonTextWriter(sw);
	jw.Formatting = Formatting.Indented;
	jw.IndentChar = '\t';
	jw.Indentation = 1;
	jw.FloatFormatHandling = FloatFormatHandling.Symbol;

	var serializer = new JsonSerializer();
	serializer.Serialize(jw, json);

	return sw.ToString();
}

void JsonToConfig(string input, string output) {
	var json = JObject.Parse(File.ReadAllText(input));
	var configType = (string)json["TYPE"]["Type"];
	var hasRefs = json.ContainsKey("REFS");

	var config = OverstrikeShared.STG.Files.Config.Make(configType, hasRefs);
	config.ContentSection.Data = (JObject)json["DATA"];

	if (hasRefs) {
		foreach (var path in json["REFS"]) {
			config.AddReference((string)path);
		}
	}

	File.WriteAllBytes(output, config.Save());
}
