// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Utils;
using OverstrikeShared.STG;
using System.Collections.Generic;
using System.IO;

namespace Overstrike.Installers {
	internal partial class SuitsMenuInstaller_MSM2 {
		// Recomputes the header the same way WriteSpiderArmsArchive does. This asset is rewritten
		// on every install with a byte count that depends on the profile's suit list (deletions,
		// reorders, edits), so it essentially never matches whatever header size it had before --
		// passing a null header here left the header at a stale value (from vanilla, or from
		// whatever installer touched this asset earlier in the same run) and crashed the game on
		// boot as soon as the mismatch was large enough to matter.
		private byte[] PrepareSuitsMenuHeader(ulong systemProgressionAssetId, int byteCount) {
			var assetIndex = _toc.FindFirstAssetIndexById(systemProgressionAssetId);
			var originalHeader = (assetIndex < 0 ? null : _toc.GetHeaderByAssetIndex(assetIndex));
			if (originalHeader == null) {
				ErrorLogger.WriteInfo("Change Suit Slot/Model: system_progression.config has no asset header; installation aborted before writing suit overrides.\n");
				throw new InvalidDataException("system_progression.config has no asset header");
			}

			var header = new AssetHeaderHelper(originalHeader);
			if (header.Pairs.Count == 0) {
				ErrorLogger.WriteInfo("Change Suit Slot/Model: system_progression.config's header has no size field; installation aborted before writing suit overrides.\n");
				throw new InvalidDataException("system_progression.config header has no size field");
			}

			header.Pairs[0].A = (uint)byteCount;
			return header.Save();
		}

		private void WriteSuitsMenuArchive(ulong systemProgressionAssetId, byte[] systemProgressionBytes, byte[] header) {
			var modsPath = Path.Combine(_gamePath, "d", "mods");
			var archivePath = Path.Combine(modsPath, "suits_menu");
			var archiveIndex = GetArchiveIndex(Path.GetRelativePath(_gamePath, archivePath));

			File.WriteAllBytes(archivePath, systemProgressionBytes);
			OverwriteAsset(0, systemProgressionAssetId, archiveIndex, 0, (uint)systemProgressionBytes.Length, header, null);
		}

		private void CleanGeneratedArchives() {
			_generatedArchives.Clear();
			var modsPath = Path.Combine(_gamePath, "d", "mods");
			if (!Directory.Exists(modsPath)) return;

			var manifestPath = Path.Combine(modsPath, GENERATED_ARCHIVES_MANIFEST);
			if (!File.Exists(manifestPath)) {
				// First run after upgrading from a build that predates this manifest: fall back to
				// the old pattern scan once, so archives it generated before the manifest existed
				// don't sit there forever -- unrecorded, and permanently refused by
				// RegisterGeneratedArchive because they "already exist but aren't owned by this
				// installer". Every run after this one has a manifest and skips this branch.
				CleanGeneratedArchivesByPattern(modsPath);
				return;
			}

			string[] filenames;
			try {
				filenames = File.ReadAllLines(manifestPath);
			} catch {
				Warn("Suit Menu: could not read the generated-archives manifest; stale archives were left untouched.");
				return;
			}

			foreach (var recordedName in filenames) {
				var filename = recordedName.Trim();
				if (filename != Path.GetFileName(filename) || !IsGeneratedArchiveName(filename)) continue;

				var archivePath = Path.Combine(modsPath, filename);
				try {
					if (File.Exists(archivePath)) {
						File.Delete(archivePath);
					}
				} catch {
					_generatedArchives.Add(filename);
					Warn($"Suit Menu: could not remove stale archive '{filename}'.");
				}
			}

			try {
				PersistGeneratedArchivesManifest(modsPath);
			} catch {
				Warn("Suit Menu: could not update the generated-archives manifest.");
			}
		}

		private void CleanGeneratedArchivesByPattern(string modsPath) {
			try {
				foreach (var filePath in Directory.EnumerateFiles(modsPath)) {
					var filename = Path.GetFileName(filePath);
					if (!IsGeneratedArchiveName(filename)) continue;
					try {
						File.Delete(filePath);
					} catch {
						Warn($"Suit Menu: could not remove stale archive '{filename}'.");
					}
				}
			} catch {
				Warn("Suit Menu: could not scan for archives generated before the manifest existed.");
			}
		}

		private bool RegisterGeneratedArchive(string archivePath) {
			var filename = Path.GetFileName(archivePath);
			if (!IsGeneratedArchiveName(filename)) {
				Warn($"Suit Menu: refused to register unsafe archive name '{filename}'.");
				return false;
			}
			if (File.Exists(archivePath) && !_generatedArchives.Contains(filename)) {
				Warn($"Suit Menu: generated archive '{filename}' already exists but is not owned by this installer; skipped to avoid overwriting it.");
				return false;
			}

			if (!_generatedArchives.Add(filename)) return true;

			var modsPath = Path.GetDirectoryName(archivePath);
			if (string.IsNullOrEmpty(modsPath)) {
				_generatedArchives.Remove(filename);
				Warn($"Suit Menu: could not resolve the directory for generated archive '{filename}'; the write was skipped.");
				return false;
			}

			try {
				PersistGeneratedArchivesManifest(modsPath);
				return true;
			} catch {
				_generatedArchives.Remove(filename);
				Warn($"Suit Menu: could not register generated archive '{filename}'; the write was skipped.");
				return false;
			}
		}

		private void PersistGeneratedArchivesManifest(string modsPath) {
			var manifestPath = Path.Combine(modsPath, GENERATED_ARCHIVES_MANIFEST);
			var temporaryPath = manifestPath + ".tmp";

			if (_generatedArchives.Count == 0) {
				if (File.Exists(temporaryPath)) {
					File.Delete(temporaryPath);
				}
				if (File.Exists(manifestPath)) {
					File.Delete(manifestPath);
				}
				return;
			}

			var filenames = new List<string>(_generatedArchives);
			filenames.Sort(System.StringComparer.OrdinalIgnoreCase);
			File.WriteAllLines(temporaryPath, filenames);
			File.Move(temporaryPath, manifestPath, true);
		}

		// "sm_" = a forced body/mask model, "sa_" = a rewritten Spider-Arms reward loadout.
		private static bool IsGeneratedArchiveName(string filename) {
			if (!filename.StartsWith("sm_", System.StringComparison.OrdinalIgnoreCase)
				&& !filename.StartsWith("sa_", System.StringComparison.OrdinalIgnoreCase)) return false;

			var hexStart = 3;
			if (filename.Length == 22 && filename[5] == '_') { // sm_XX_XXXXXXXXXXXXXXXX
				if (!IsHexDigit(filename[3]) || !IsHexDigit(filename[4])) return false;
				hexStart = 6;
			} else if (filename.Length != 19) return false; // sm_XXXXXXXXXXXXXXXX

			if (filename.Length - hexStart != 16) return false;
			for (var i = hexStart; i < filename.Length; ++i) {
				if (!IsHexDigit(filename[i])) return false;
			}
			return true;
		}

		private static bool IsHexDigit(char c) =>
			(c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
	}
}
