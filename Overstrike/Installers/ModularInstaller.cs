// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO.Compression;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Overstrike.Data;
using Overstrike.Detectors;
using Overstrike.Utils;
using System;

namespace Overstrike.Installers {
	internal class ModularInstaller {
		// profile

		public static ulong GetSelectedCombinationNumber(ModEntry mod) {
			if (mod.Extras != null && mod.Extras.ContainsKey("selections")) {
				return (ulong)mod.Extras["selections"];
			}
			return 1;
		}

		// data

		public static ZipArchive ReadModularFile(ModEntry mod) {
			return NestedFiles.GetNestedZip(mod.Path);
		}

		public static JObject GetInfo(ZipArchive zip) {
			var entry = NestedFiles.GetZipEntryByFullName(zip, "info.json");
			using var stream = entry.Open();
			using var reader = new StreamReader(stream);
			var str = reader.ReadToEnd();
			return JObject.Parse(str);
		}

		// installing

		public static void AddEntriesToInstall(List<ModEntry> modsToInstall, ModEntry libraryMod, ModEntry profileMod) {
			var current = GetSelectedCombinationNumber(profileMod);
			var baseName = $"{libraryMod.Name}' #{current}";

			using var modular = ReadModularFile(libraryMod);
			JObject info = GetInfo(modular);

			// go through layout & make entries for selected options

			var outMods = new List<ModEntry>();
			var outModsNames = new List<string>();

			var detectors = new List<DetectorBase>() {
				new SuitModDetector(),
				new StageModDetector()
			};
			var detectorsExtensions = new List<string[]>();
			foreach (var detector in detectors) {
				detectorsExtensions.Add(detector.GetExtensions());
			}

			current -= 1;

			var layout = (JArray)info["layout"];
			foreach (var entry in layout) {
				var entryType = (string)entry[0];
				if (entryType != "module") {
					continue;
				}

				var options = (JArray)entry[2];
				var optionsCount = (ulong)options.Count;

				ulong selectedOption = 0;
				if (optionsCount != 1) {
					selectedOption = current % optionsCount;
					current /= optionsCount;
				}

				var option = (JArray)options[(int)selectedOption];
				var optionPath = (string)option[2];
				if (optionPath == "") {
					continue;
				}

				var optionFileBytes = NestedFiles.GetZippedFileBytes(modular, optionPath);

				List<ModEntry> detectedMods = new();
				List<string> warnings = new(); // ignored
				for (int i = 0; i < detectors.Count; ++i) {
					var detector = detectors[i];
					var extensions = detectorsExtensions[i];

					var matchesExtension = false;
					foreach (var extension in extensions) {
						if (optionPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) {
							matchesExtension = true;
							break;
						}
					}

					if (matchesExtension) {
						var file = new MemoryStream(optionFileBytes);
						file.Seek(0, SeekOrigin.Begin);
						detector.Detect(file, libraryMod.Path + "||" + optionPath, detectedMods, warnings);
					}
				}

				foreach (var mod in detectedMods) {
					if (!ModTypeMatchesModularType(mod.Type, libraryMod.Type)) continue;

					outMods.Add(mod);
					outModsNames.Add(optionPath);
				}
			}

			// when all entries are made, add them with names tweaked

			var index = 0;
			foreach (var mod in outMods) {
				mod.Name = $"{baseName} ({index}/{outMods.Count}): '{outModsNames[index]}";
				++index;

				modsToInstall.Add(mod);
			}
		}

		private static bool ModTypeMatchesModularType(ModEntry.ModType innerModType, ModEntry.ModType modularType) {
			switch (modularType) {
				case ModEntry.ModType.MODULAR_MSMR:
					return (innerModType == ModEntry.ModType.SUIT_MSMR || innerModType == ModEntry.ModType.STAGE_MSMR);

				case ModEntry.ModType.MODULAR_MM:
					return (innerModType == ModEntry.ModType.SUIT_MM || innerModType == ModEntry.ModType.SUIT_MM_V2 || innerModType == ModEntry.ModType.STAGE_MM);

				case ModEntry.ModType.MODULAR_RCRA:
					return (innerModType == ModEntry.ModType.STAGE_RCRA || innerModType == ModEntry.ModType.STAGE_RCRA_V2);

				case ModEntry.ModType.MODULAR_I30:
					return (innerModType == ModEntry.ModType.STAGE_I30);

				case ModEntry.ModType.MODULAR_I33:
					return (innerModType == ModEntry.ModType.STAGE_I33);

				case ModEntry.ModType.MODULAR_MSM2:
					return (innerModType == ModEntry.ModType.STAGE_MSM2 || innerModType == ModEntry.ModType.STAGE_MSM2_V2 || innerModType == ModEntry.ModType.SCRIPT_MSM2);

				default: return false;
			}
		}
	}
}
