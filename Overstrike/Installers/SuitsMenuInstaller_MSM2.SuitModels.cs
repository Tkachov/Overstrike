// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Files;
using Overstrike.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Overstrike.Installers {
	internal partial class SuitsMenuInstaller_MSM2 {
		private const string MILES_VENOM_XRAY_MODEL = "characters/hero/hero_spiderman_miles_venomxray/hero_spiderman_miles_venomxray.model";

		private sealed class SuitModelPaths {
			public List<string> BodyPaths { get; }
			public string? MaskPath { get; }
			public string? Error { get; }

			public SuitModelPaths(List<string> bodyPaths, string? maskPath, string? error = null) {
				BodyPaths = bodyPaths;
				MaskPath = maskPath;
				Error = error;
			}
		}

		private sealed class SuitModelRequest {
			public string SuitName { get; }
			public string TargetItemPath { get; }
			public string SourceItemPath { get; }

			public SuitModelRequest(string suitName, string targetItemPath, string sourceItemPath) {
				SuitName = suitName;
				TargetItemPath = targetItemPath;
				SourceItemPath = sourceItemPath;
			}
		}

		private sealed class ModelAssetLocation {
			public string Path { get; }
			public int AssetIndex { get; }
			public uint ArchiveIndex { get; }
			public uint ArchiveOffset { get; }
			public uint Size { get; }
			public byte[]? Header { get; }
			public byte[]? TextureMeta { get; }

			public ModelAssetLocation(string path, int assetIndex, uint archiveIndex, uint archiveOffset, uint size, byte[]? header, byte[]? textureMeta) {
				Path = path;
				AssetIndex = assetIndex;
				ArchiveIndex = archiveIndex;
				ArchiveOffset = archiveOffset;
				Size = size;
				Header = header;
				TextureMeta = textureMeta;
			}
		}

		private sealed class ModelTransfer {
			public ModelAssetLocation Target { get; }
			public ModelAssetLocation Source { get; }

			public ModelTransfer(ModelAssetLocation target, ModelAssetLocation source) {
				Target = target;
				Source = source;
			}
		}

		// Resolve every source against the current TOC before changing any target. This preserves
		// replacements installed earlier in the same run and prevents swaps from aliasing each other.
		private void ApplyForcedSuitModels(List<SuitModelRequest> requests) {
			var transfers = new Dictionary<int, ModelTransfer>();
			foreach (var request in requests) {
				PlanSuitModelRequest(request, transfers);
			}

			foreach (var transfer in transfers.Values) {
				var updater = new TOC_I29.AssetUpdater(transfer.Target.AssetIndex);
				updater
					.UpdateArchiveIndex(transfer.Source.ArchiveIndex)
					.UpdateArchiveOffset(transfer.Source.ArchiveOffset)
					.UpdateSize(transfer.Source.Size);
				updater.UpdateHeader(transfer.Source.Header);
				if (transfer.Source.TextureMeta != null) {
					updater.UpdateTextureMeta(CRC64.Hash(transfer.Target.Path), transfer.Source.TextureMeta);
				}
				updater.Apply(_toc);
			}
		}

		private void PlanSuitModelRequest(SuitModelRequest request, Dictionary<int, ModelTransfer> transfers) {
			var targetItemPath = DAT1.Utils.Normalize(request.TargetItemPath ?? "");
			var sourceItemPath = DAT1.Utils.Normalize(request.SourceItemPath ?? "");
			if (string.IsNullOrEmpty(targetItemPath) || string.IsNullOrEmpty(sourceItemPath) || targetItemPath == sourceItemPath) return;

			var targetCharacter = ResolveSuitCharacter(targetItemPath);
			var sourceCharacter = ResolveSuitCharacter(sourceItemPath);
			if (targetCharacter == null || sourceCharacter == null) {
				throw new InvalidDataException($"Could not determine the characters for suit model change '{request.SuitName}'");
			}
			if (targetCharacter != sourceCharacter && !_allowCrossCharacterSuitModels) {
				throw new InvalidDataException($"Cross-character suit model changes are disabled for '{request.SuitName}'");
			}

			var target = ResolveSuitModelPaths(targetItemPath);
			var source = ResolveSuitModelPaths(sourceItemPath);
			if (target.Error != null || source.Error != null || target.BodyPaths.Count == 0 || source.BodyPaths.Count == 0) {
				throw new InvalidDataException($"Could not resolve the body models for suit model change '{request.SuitName}'");
			}

			// Miles loadouts include a shared venom-xray overlay in addition to the suit body.
			// Redirecting that shared asset replaces the overlay for every Miles suit. Some DLC
			// loadouts list the overlay first, so exclude it when Miles is on either side.
			var sourceBodyPath = sourceCharacter == MSM2Character.Miles
				? ResolvePrimaryBodyPath(source.BodyPaths)
				: source.BodyPaths[0];
			if (sourceBodyPath == null) {
				throw new InvalidDataException($"Could not resolve the primary body models for suit model change '{request.SuitName}'");
			}

			if (targetCharacter == MSM2Character.Miles) {
				var targetBodyPath = ResolvePrimaryBodyPath(target.BodyPaths);
				if (targetBodyPath == null) {
					throw new InvalidDataException($"Could not resolve the primary body models for suit model change '{request.SuitName}'");
				}
				AddTransfer(targetBodyPath, sourceBodyPath, transfers);
			} else {
				// Preserve the original behavior for Peter and any future non-Miles character.
				foreach (var targetBodyPath in target.BodyPaths) {
					AddTransfer(targetBodyPath, sourceBodyPath, transfers);
				}
			}

			// Masks are optional. A missing mask never blocks an otherwise valid body redirect.
			if (!string.IsNullOrEmpty(target.MaskPath) && !string.IsNullOrEmpty(source.MaskPath)) {
				try {
					AddTransfer(target.MaskPath, source.MaskPath, transfers);
				} catch (InvalidDataException e) {
					ErrorLogger.WriteInfo($"Suit Menu: mask redirect skipped for '{request.SuitName}': {e.Message}\n");
				}
			}
		}

		private static string? ResolvePrimaryBodyPath(List<string> bodyPaths) {
			foreach (var path in bodyPaths) {
				if (!path.Equals(MILES_VENOM_XRAY_MODEL, StringComparison.OrdinalIgnoreCase)) return path;
			}
			return null;
		}

		private void AddTransfer(string targetPath, string sourcePath, Dictionary<int, ModelTransfer> transfers) {
			if (targetPath == sourcePath) return;

			var target = FindModelAsset(targetPath) ?? throw new InvalidDataException($"target model '{targetPath}' is not present in the TOC");
			var source = FindModelAsset(sourcePath) ?? throw new InvalidDataException($"source model '{sourcePath}' is not present in the TOC");
			transfers[target.AssetIndex] = new ModelTransfer(target, source);
		}

		private ModelAssetLocation? FindModelAsset(string path) {
			var id = CRC64.Hash(path);
			var index = _toc.FindFirstAssetIndexById(id);
			if (index < 0) return null;

			var archiveIndex = _toc.GetArchiveIndexByAssetIndex(index);
			var archiveOffset = _toc.GetOffsetInArchiveByAssetIndex(index);
			var size = _toc.GetSizeInArchiveByAssetIndex(index);
			if (archiveIndex == null || archiveOffset == null || size == null) return null;

			return new ModelAssetLocation(
				path,
				index,
				archiveIndex.Value,
				archiveOffset.Value,
				size.Value,
				_toc.GetHeaderByAssetIndex(index),
				_toc.GetTextureMetaByAssetIndex(index)
			);
		}

		private SuitModelPaths ResolveSuitModelPaths(string rewardLoadoutPath) {
			if (string.IsNullOrEmpty(rewardLoadoutPath)) return new SuitModelPaths(new(), null, "empty loadout path");
			rewardLoadoutPath = DAT1.Utils.Normalize(rewardLoadoutPath);
			if (_modelPathsCache.TryGetValue(rewardLoadoutPath, out var cached)) return cached;

			var bodyPaths = new List<string>();
			var bodyPathsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			string? mask = null;

			try {
				var reward = new Config_I30(_toc.GetAssetReader(rewardLoadoutPath));
				var rewardRoot = reward.ContentSection.Data;
				mask = (string)rewardRoot["DefaultMaskModel"]?["AssetPath"];
				if (!string.IsNullOrEmpty(mask)) {
					mask = DAT1.Utils.Normalize(mask);
					if (!mask.EndsWith(".model", StringComparison.OrdinalIgnoreCase)) mask = null;
				}

				var itemLoadoutPath = (string)rewardRoot["DefaultLoadoutConfig"]?["AssetPath"];
				if (!string.IsNullOrEmpty(itemLoadoutPath)) {
					var itemLoadout = new Config_I30(_toc.GetAssetReader(DAT1.Utils.Normalize(itemLoadoutPath)));
					var loadoutLists = itemLoadout.ContentSection.Data["Loadout"]?["ItemLoadoutLists"] as JArray;
					foreach (var listToken in loadoutLists ?? new JArray()) {
						if (listToken is not JObject list) continue;
						foreach (var entryToken in (list["Items"] as JArray) ?? new JArray()) {
							var itemValue = (string?)entryToken["Item"];
							if (string.IsNullOrEmpty(itemValue)) continue;
							try {
								var vanity = new Config_I30(_toc.GetAssetReader(DAT1.Utils.Normalize(itemValue)));
								foreach (var modelToken in (vanity.ContentSection.Data["ModelList"] as JArray) ?? new JArray()) {
									var assetPath = (string)modelToken["Model"]?["AssetPath"];
									if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".model", StringComparison.OrdinalIgnoreCase)) {
										assetPath = DAT1.Utils.Normalize(assetPath);
										if (bodyPathsSet.Add(assetPath)) bodyPaths.Add(assetPath);
									}
								}
							} catch {}
						}
					}
				}
			} catch {
				var failed = new SuitModelPaths(new(), null, "loadout config could not be read");
				_modelPathsCache[rewardLoadoutPath] = failed;
				return failed;
			}

			var result = new SuitModelPaths(bodyPaths, mask);
			_modelPathsCache[rewardLoadoutPath] = result;
			return result;
		}
	}
}
