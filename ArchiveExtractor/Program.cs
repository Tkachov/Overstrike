// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using System.Globalization;

void Work() {
	var firstArg = args.Length > 0 ? args[0] : null;
	if (firstArg == null) {
		Console.WriteLine("Drag the game archive onto this .exe to extract all assets from it.");
		return;
	}

	if (!File.Exists(firstArg)) {
		Console.WriteLine($"Error: file '{firstArg}' doesn't exist!");
		return;
	}

	// read toc

	var assetArchiveDir = Path.GetDirectoryName(firstArg);
	var gameDir = Path.GetDirectoryName(assetArchiveDir);
	var tocPath = Path.Combine(assetArchiveDir, "toc.BAK");
	if (!File.Exists(tocPath)) {
		tocPath = Path.Combine(assetArchiveDir, "toc");
	}
	if (!File.Exists(tocPath)) {
		Path.Combine(gameDir, "toc.BAK");
	}
	if (!File.Exists(tocPath)) {
		tocPath = Path.Combine(gameDir, "toc");
	}

	if (!File.Exists(tocPath)) {
		Console.WriteLine($"Error: 'toc' was not found under '{gameDir}' or '{assetArchiveDir}'!");
		return;
	}

	TOCBase? toc = LoadTOC(tocPath);
	if (toc == null) {
		Console.WriteLine($"Error: failed to read 'toc'!");
		Console.WriteLine($"Verify game files to get clean one. Remove 'toc.BAK', if you have one.");
		return;
	}

	// load hashes

	var appdir = AppDomain.CurrentDomain.BaseDirectory;
	var hashes_fn = Path.Combine(appdir, "hashes.txt");
	var knownHashes = ReadHashes(hashes_fn);
	if (knownHashes.Count > 0) {
		Console.WriteLine($"hashes.txt: {knownHashes.Count} assets");
	}

	// find archive assets

	string tocType = "";
	if (toc is TOC_I20) tocType = " (MSMR/MM)";
	else if (toc is TOC_I29) tocType = " (RCRA)";
	int assetsCount = toc.AssetIdsSection.Ids.Count;
	Console.WriteLine($"TOC{tocType}: {assetsCount} assets");

	string archiveName = Path.GetRelativePath(Path.GetDirectoryName(tocPath), firstArg);
	int archiveIndex = -1;
	uint archivesCount = toc.GetArchivesCount();
	for (int i = 0; i < archivesCount; ++i) {
		if (string.Equals(toc.GetArchiveFilename((uint)i), archiveName, StringComparison.InvariantCultureIgnoreCase)) {
			archiveIndex = i;
			break;
		}
	}

	List<int> archiveAssetIndexes = new();
	for (int i = 0; i < assetsCount; ++i) {
		if (toc.GetArchiveIndexByAssetIndex(i) == archiveIndex) {
			archiveAssetIndexes.Add(i);
		}
	}
	Console.WriteLine($"{archiveName}: {archiveAssetIndexes.Count} assets");
	Console.WriteLine();

	// extract assets

	var extractedDir = Path.Combine(assetArchiveDir, "extracted");
	if (!Directory.Exists(extractedDir)) Directory.CreateDirectory(extractedDir);

	var extractedArchiveDir = Path.Combine(assetArchiveDir, "extracted", Path.GetFileName(firstArg));
	if (!Directory.Exists(extractedArchiveDir)) Directory.CreateDirectory(extractedArchiveDir);

	Console.WriteLine($"Extracting {archiveAssetIndexes.Count} assets...");
	Console.WriteLine();

	var n = 1;
	var padWidth = $"{archiveAssetIndexes.Count}".Length + 1;
	var success = 0;
	foreach (var index in archiveAssetIndexes) {
		var paddedN = $"{n}".PadLeft(padWidth);

		var spanIndex = toc.GetSpanIndexByAssetIndex(index);
		var assetId = toc.GetAssetIdByAssetIndex(index);
		if (spanIndex == null || assetId == null) {
			Console.WriteLine($"{paddedN}: failed");
			++n;
			continue;
		}

		var filename = Path.Combine(extractedArchiveDir, $"{spanIndex}", $"{assetId:X016}");
		var displayName = $"{assetId:X016}";
		if (knownHashes.ContainsKey((ulong)assetId)) {
			filename = Path.Combine(extractedArchiveDir, $"{spanIndex}", knownHashes[(ulong)assetId]);
			displayName += " (" + CropForDisplayName(filename) + ")";
		}

		var dirname = Path.GetDirectoryName(filename);
		if (!Directory.Exists(dirname)) Directory.CreateDirectory(dirname);

		var line = $"{paddedN}: {displayName}".PadRight(70);
		try {
			File.WriteAllBytes(filename, toc.ExtractAsset(index));
			Console.WriteLine($"{line} -- OK");
			++success;
		} catch {
			Console.WriteLine($"{line} -- failed");
			// TODO: provide some explanation here (catch common errors and display interpretation; or write exceptions to some file like `errors.log`)
		}
		++n;
	}

	Console.WriteLine();
	var verdict = $"{success}/{archiveAssetIndexes.Count} extracted";
	if (success < archiveAssetIndexes.Count)
		verdict += $", {archiveAssetIndexes.Count - success} failed";
	verdict += ".";
	Console.WriteLine(verdict);
}

TOCBase? LoadTOC(string tocPath) {
	TOC_I29 toc_i29 = new();
	if (toc_i29.Load(tocPath)) {
		return toc_i29;
	}

	TOC_I20 toc_i20 = new();
	if (toc_i20.Load(tocPath)) {
		return toc_i20;
	}

	return null;
}

Dictionary<ulong, string> ReadHashes(string hashes_fn) {
	Dictionary<ulong, string> result = new();

	if (File.Exists(hashes_fn)) {
		foreach (var line in File.ReadLines(hashes_fn)) {
			try {
				var firstComma = line.IndexOf(',');
				if (firstComma == -1) continue;

				var lastComma = line.LastIndexOf(',');
				var assetPath = (lastComma == -1 ? line.Substring(firstComma + 1) : line.Substring(firstComma + 1, lastComma - firstComma - 1));
				var assetId = ulong.Parse(line.Substring(0, firstComma), NumberStyles.HexNumber);

				if (assetPath.Trim().Length > 0) {
					result.Add(assetId, assetPath);
				}
			} catch {}
		}
	}

	return result;
}

string CropForDisplayName(string filename) {
	var name = Path.GetFileName(filename);
	if (name.Length < 44)
		return name;

	return name.Substring(0, 20) + "---" + name.Substring(name.Length - 20);
}

//

Work();

Console.WriteLine();
Console.WriteLine("Press Enter to quit.");
Console.ReadLine();
