// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;

void Work() {
	var firstArg = args.Length > 0 ? args[0] : null;
	if (firstArg == null) {
		Console.WriteLine("Drag the game archive onto this .exe to extract all .wems from it.");
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

	int archiveAssets = 0;
	List<int> archiveWemIndexes = new();
	for (int i = 0; i < assetsCount; ++i) {
		if (toc.GetArchiveIndexByAssetIndex(i) == archiveIndex) {
			++archiveAssets;
			
			ulong? assetId = toc.GetAssetIdByAssetIndex(i);
			if (assetId != null) {
				if ((((ulong)assetId) & 0xFFFFFFFF00000000) == 0xE000000000000000) {
					archiveWemIndexes.Add(i);
				}
			}
		}
	}
	Console.WriteLine($"{archiveName}: {archiveAssets} assets, {archiveWemIndexes.Count} wems");
	Console.WriteLine();

	// extract assets

	var extractedDir = Path.Combine(assetArchiveDir, "extracted");
	if (!Directory.Exists(extractedDir)) Directory.CreateDirectory(extractedDir);

	var extractedArchiveDir = Path.Combine(assetArchiveDir, "extracted", Path.GetFileName(firstArg));
	if (!Directory.Exists(extractedArchiveDir)) Directory.CreateDirectory(extractedArchiveDir);

	Console.WriteLine($"Extracting {archiveWemIndexes.Count} wems...");
	Console.WriteLine();

	var n = 1;
	var padWidth = $"{archiveWemIndexes.Count}".Length + 1;
	var success = 0;
	foreach (var index in archiveWemIndexes) {
		var paddedN = $"{n}".PadLeft(padWidth);

		var spanIndex = toc.GetSpanIndexByAssetIndex(index);
		var assetId = toc.GetAssetIdByAssetIndex(index);
		if (spanIndex == null || assetId == null) {
			Console.WriteLine($"{paddedN}: failed");
			++n;
			continue;
		}

		var wemNumber = assetId & 0xFFFFFFFF;
		var filename = Path.Combine(extractedArchiveDir, $"{spanIndex}", $"{wemNumber}.wem");
		var displayName = $"{wemNumber}.wem";

		var dirname = Path.GetDirectoryName(filename);
		if (!Directory.Exists(dirname)) Directory.CreateDirectory(dirname);

		var line = $"{paddedN}: {displayName}".PadRight(25);
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
	var verdict = $"{success}/{archiveWemIndexes.Count} extracted";
	if (success < archiveWemIndexes.Count)
		verdict += $", {archiveWemIndexes.Count - success} failed";
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

//

Work();

Console.WriteLine();
Console.WriteLine("Press Enter to quit.");
Console.ReadLine();
