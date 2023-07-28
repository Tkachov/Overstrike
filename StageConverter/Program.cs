// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO.Compression;
using System.Text;

void Work() {
	const string DIRFILE = "assetArchiveDir.txt";

	var appdir = AppDomain.CurrentDomain.BaseDirectory;
	var dirfile = Path.Combine(appdir, DIRFILE);

	if (!File.Exists(dirfile)) {
		Console.WriteLine($"Error: '{DIRFILE}' was not found!");
		Console.WriteLine("Create one or move this .exe to a folder where it is.");
		return;
	}

	var path = File.ReadAllLines(dirfile)[0];
	var tocPath = Path.Combine(path, "toc.BAK");
	if (!File.Exists(tocPath)) {
		tocPath = Path.Combine(path, "toc");
	}
	if (!File.Exists(tocPath)) {
		Console.WriteLine($"Error: 'toc' was not found under '{path}'!");
		Console.WriteLine($"Check that your '{DIRFILE}' points to the right path, and verify game files.");
		return;
	}

	DAT1.TOC_I20 toc = new();
	if (!toc.Load(tocPath)) {
		Console.WriteLine($"Error: failed to read 'toc'!");
		Console.WriteLine($"Verify game files to get clean one. Remove 'toc.BAK', if you have one.");
		return;
	}

	var firstArg = args.Length > 0 ? args[0] : null;
	if (firstArg == null) {
		Console.WriteLine("Drag a .smpcmod onto this .exe to convert it to .stage.");
		return;
	}

	if (!File.Exists(firstArg)) {
		Console.WriteLine($"Error: file '{firstArg}' doesn't exist!");
		return;
	}

	List<Asset> assets = new();
	ModInfo? info = null;
	try {
		using (var zip = new ZipArchive(File.OpenRead(firstArg))) {
			foreach (ZipArchiveEntry entry in zip.Entries) {
				if (entry.FullName.StartsWith("ModFiles/", StringComparison.OrdinalIgnoreCase)) {
					var asset = ReadAsset(entry, toc);
					if (asset != null) assets.Add((Asset)asset);
				} else if (entry.FullName.Equals("SMPCMod.info", StringComparison.OrdinalIgnoreCase)) {
					info = ReadInfo(entry);
				}
			}
		}
	} catch {
		Console.WriteLine($"Error: failed to read '{firstArg}'!");
		return;
	}

	var newName = Path.Combine(Path.GetDirectoryName(firstArg), Path.GetFileNameWithoutExtension(firstArg) + ".stage");
	try {
		using (var f = new FileStream(newName, FileMode.Create, FileAccess.Write, FileShare.None)) {
			using (var zip = new ZipArchive(f, ZipArchiveMode.Create)) {
				foreach (var asset in assets) {
					var e = zip.CreateEntry($"{asset.Span}/{asset.Id:X016}");
					using (var ef = e.Open()) {
						ef.Write(asset.Data, 0, asset.Data.Length);
					}
				}

				{
					var e = zip.CreateEntry("info.json");
					using (var ef = e.Open()) {
						var name = "";
						if (info != null && info?.Name != null) {
							name = info?.Name;
						}

						var author = "";
						if (info != null && info?.Author != null) {
							author = info?.Author;
						}

						JObject j = new();
						j["game"] = (firstArg.EndsWith(".mmpcmod", StringComparison.OrdinalIgnoreCase) ? "MM" : "MSMR");
						j["name"] = name;
						j["author"] = author;

						var text = j.ToString();
						var data = Encoding.UTF8.GetBytes(text);
						ef.Write(data, 0, data.Length);
					}
				}
			}
		}
	} catch {
		Console.WriteLine($"Error: failed to write '{newName}'!");
		return;
	}

	Console.WriteLine("Done!");
}

Asset? ReadAsset(ZipArchiveEntry asset, TOC_I20 toc) {
	string[] parts = asset.Name.Split("_");
	if (parts.Length != 2) return null;

	int archiveIndex = int.Parse(parts[0]);
	ulong assetId = ulong.Parse(parts[1], NumberStyles.HexNumber);

	var ms = new MemoryStream();
	using (var stream = asset.Open()) {
		stream.CopyTo(ms);
	}
	ms.Seek(0, SeekOrigin.Begin);

	byte? span = null;
	int[] assetIndexes = toc.FindAssetIndexesById(assetId);
	foreach (var assetIndex in assetIndexes) {
		if (toc.GetArchiveIndexByAssetIndex(assetIndex) == archiveIndex) {
			span = toc.GetSpanIndexByAssetIndex(assetIndex);
			break;
		}
	}

	if (span == null) {
		Console.WriteLine($"Warning: couldn't find {assetId:X016} in 'toc'.");
		return null;
	}

	return new Asset() {
		Span = (byte)span,
		Id = assetId,
		Data = ms.ToArray()
	};
}

ModInfo? ReadInfo(ZipArchiveEntry entry) {
	string name = null;
	string author = null;

	using (var stream = entry.Open()) {
		using (StreamReader reader = new StreamReader(stream)) {
			var str = reader.ReadToEnd();
			var lines = str.Split("\n");
			foreach (var line in lines) {
				var sep = line.IndexOf("=");
				if (sep != -1) {
					var key = line.Substring(0, sep);
					var value = line.Substring(sep + 1);
					if (key.Equals("Title", StringComparison.OrdinalIgnoreCase)) {
						name = value;
					} else if (key.Equals("Author", StringComparison.OrdinalIgnoreCase)) {
						author = value;
					}
				}
			}
		}
	}

	if (name == null && author == null) return null;

	return new ModInfo() {
		Name = name,
		Author = author
	};
}

//

Work();

Console.WriteLine();
Console.WriteLine("Press Enter to quit.");
Console.ReadLine();

//

struct Asset {
	public byte Span;
	public ulong Id;

	public byte[] Data;
}

struct ModInfo {
	public string Name;
	public string Author;
}
