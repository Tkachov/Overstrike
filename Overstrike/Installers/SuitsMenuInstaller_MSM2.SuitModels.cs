// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using Overstrike.Utils;
using OverstrikeShared.STG;
using System.Collections.Generic;
using System.IO;

namespace Overstrike.Installers {
	internal partial class SuitsMenuInstaller_MSM2 {
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
			public string SourceItemPath { get; set; }
			public bool ForceMask { get; }

			public SuitModelRequest(string suitName, string targetItemPath, string sourceItemPath, bool forceMask) {
				SuitName = suitName;
				TargetItemPath = targetItemPath;
				SourceItemPath = sourceItemPath;
				ForceMask = forceMask;
			}
		}

		private sealed class ModelAssetLocation {
			public string Path { get; }
			public byte Span { get; }
			public ulong Id { get; }
			public byte[]? Header { get; }
			public byte[]? TextureMeta { get; }

			public ModelAssetLocation(string path, byte span, ulong id, byte[]? header, byte[]? textureMeta) {
				Path = path;
				Span = span;
				Id = id;
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

		private sealed class SuitModelPlan {
			public string SuitName { get; }
			public List<ModelTransfer> Transfers { get; } = new();

			public SuitModelPlan(string suitName) {
				SuitName = suitName;
			}
		}

		private sealed class ModelAssetData {
			public byte[] Raw { get; }
			public byte[]? Header { get; }
			public byte[]? TextureMeta { get; }

			public ModelAssetData(byte[] raw, byte[]? header, byte[]? textureMeta) {
				Raw = raw;
				Header = header;
				TextureMeta = textureMeta;
			}
		}

		// Plans all transfers and snapshots source bytes before changing the TOC. This prevents one
		// forced slot from becoming another forced slot's source during the same installation.
		private void ApplyForcedSuitModels(List<SuitModelRequest> requests, List<JObject> allSuits) {
			if (requests.Count == 0) return;

			var owners = BuildModelOwners(allSuits);
			var candidates = new List<SuitModelPlan>();
			foreach (var request in requests) {
				var plan = PlanSuitModelRequest(request);
				if (plan != null) {
					candidates.Add(plan);
				}
			}

			var plans = ValidatePlans(candidates, owners);
			var transfers = new List<ModelTransfer>();
			foreach (var plan in plans) {
				transfers.AddRange(plan.Transfers);
			}
			var sourceAssets = SnapshotSourceAssets(transfers);

			foreach (var plan in plans) {
				var hasEverySourceAsset = true;
				foreach (var transfer in plan.Transfers) {
					if (!sourceAssets.ContainsKey((transfer.Source.Span, transfer.Source.Id))) {
						hasEverySourceAsset = false;
						break;
					}
				}
				if (!hasEverySourceAsset) {
					Warn($"Change Suit Model: '{plan.SuitName}' could not read every source model; the whole swap was skipped.");
					continue;
				}

				var registeredEveryArchive = true;
				foreach (var transfer in plan.Transfers) {
					if (RegisterGeneratedArchive(GetForcedModelArchivePath(transfer.Target))) continue;
					registeredEveryArchive = false;
					break;
				}
				if (!registeredEveryArchive) {
					Warn($"Change Suit Model: '{plan.SuitName}' could not reserve every generated archive; the whole swap was skipped.");
					continue;
				}

				foreach (var transfer in plan.Transfers) {
					WriteForcedModelAsset(transfer.Target, sourceAssets[(transfer.Source.Span, transfer.Source.Id)]);
				}
			}
		}

		private SuitModelPlan? PlanSuitModelRequest(SuitModelRequest request) {
			var targetItemPath = (string.IsNullOrEmpty(request.TargetItemPath) ? null : DAT1.Utils.Normalize(request.TargetItemPath));
			var sourceItemPath = (string.IsNullOrEmpty(request.SourceItemPath) ? null : DAT1.Utils.Normalize(request.SourceItemPath));
			if (string.IsNullOrEmpty(targetItemPath) || string.IsNullOrEmpty(sourceItemPath)) {
				Warn($"Change Suit Model: '{request.SuitName}' has an empty target or source loadout; skipped.");
				return null;
			}

			if (targetItemPath == sourceItemPath) {
				Warn($"Change Suit Model: '{request.SuitName}' already uses the selected model; no files changed.");
				return null;
			}

			var targetCharacter = ResolveSuitCharacter(targetItemPath);
			var sourceCharacter = ResolveSuitCharacter(sourceItemPath);
			if (targetCharacter == null || sourceCharacter == null) {
				Warn($"Change Suit Model: '{request.SuitName}' could not verify the target and source characters; skipped for safety.");
				return null;
			}
			if (targetCharacter != sourceCharacter && !_allowCrossCharacterSuitModels) {
				Warn($"Change Suit Model: '{request.SuitName}' tried to use a {MSM2SuitCharacterResolver.DisplayName(sourceCharacter.Value)} model on a {MSM2SuitCharacterResolver.DisplayName(targetCharacter.Value)} slot; skipped.");
				return null;
			}

			var target = ResolveSuitModelPaths(targetItemPath);
			var source = ResolveSuitModelPaths(sourceItemPath);
			if (target.Error != null || source.Error != null) {
				Warn($"Change Suit Model: '{request.SuitName}' could not read the target or source loadout config; skipped.");
				return null;
			}

			if (target.BodyPaths.Count == 0 || source.BodyPaths.Count == 0) {
				Warn($"Change Suit Model: '{request.SuitName}' has no resolvable body models; skipped.");
				return null;
			}
			if (target.BodyPaths.Count != source.BodyPaths.Count) {
				Warn($"Change Suit Model: '{request.SuitName}' has {target.BodyPaths.Count} target body model(s) but the source has {source.BodyPaths.Count}; skipped to avoid a partial suit.");
				return null;
			}

			var targetHasMask = !string.IsNullOrEmpty(target.MaskPath);
			var sourceHasMask = !string.IsNullOrEmpty(source.MaskPath);
			if (request.ForceMask && targetHasMask != sourceHasMask) {
				Warn($"Change Suit Model: '{request.SuitName}' has incompatible mask geometry; skipped to avoid mixing the source body with the target mask.");
				return null;
			}

			var plan = new SuitModelPlan(request.SuitName);
			var changedBodyModels = 0;
			for (var i = 0; i < target.BodyPaths.Count; ++i) {
				if (target.BodyPaths[i] != source.BodyPaths[i]) {
					++changedBodyModels;
				}
			}
			if (changedBodyModels > 1) {
				Warn($"Change Suit Model: '{request.SuitName}' would need to map multiple distinct body models; skipped because their roles cannot be verified safely.");
				return null;
			}
			for (var i = 0; i < target.BodyPaths.Count; ++i) {
				if (!TryAddTransfer(plan, $"body model {i + 1}", target.BodyPaths[i], source.BodyPaths[i])) return null;
			}
			if (request.ForceMask && targetHasMask && !TryAddTransfer(plan, "mask", target.MaskPath!, source.MaskPath!)) return null;

			if (plan.Transfers.Count == 0) {
				Warn($"Change Suit Model: '{request.SuitName}' already matches the selected model; no files changed.");
				return null;
			}
			return plan;
		}

		private bool TryAddTransfer(SuitModelPlan plan, string part, string targetPath, string sourcePath) {
			if (targetPath == sourcePath) return true;

			var target = FindModelAsset(targetPath);
			if (target == null) {
				Warn($"Change Suit Model: target {part} '{targetPath}' is not present in the TOC; skipped.");
				return false;
			}
			var source = FindModelAsset(sourcePath);
			if (source == null) {
				Warn($"Change Suit Model: source {part} '{sourcePath}' is not installed; skipped.");
				return false;
			}
			if ((target.Header == null) != (source.Header == null)) {
				Warn($"Change Suit Model: '{plan.SuitName}' has incompatible header data for {part}; skipped.");
				return false;
			}
			if ((target.TextureMeta == null) != (source.TextureMeta == null)) {
				Warn($"Change Suit Model: '{plan.SuitName}' has incompatible texture metadata for {part}; skipped.");
				return false;
			}

			foreach (var existing in plan.Transfers) {
				if (existing.Target.Span != target.Span || existing.Target.Id != target.Id) continue;
				if (existing.Source.Span == source.Span && existing.Source.Id == source.Id) return true;

				Warn($"Change Suit Model: '{plan.SuitName}' maps one target model to two different source models; skipped.");
				return false;
			}

			plan.Transfers.Add(new ModelTransfer(target, source));
			return true;
		}

		private ModelAssetLocation? FindModelAsset(string path) {
			var id = CRC64.Hash(path);
			var index = _toc.FindFirstAssetIndexById(id);
			if (index < 0) return null;

			var span = _toc.GetSpanIndexByAssetIndex(index);
			return (span == null ? null : new ModelAssetLocation(
				path,
				(byte)span,
				id,
				_toc.GetHeaderByAssetIndex(index),
				_toc.GetTextureMetaByAssetIndex(index)
			));
		}

		private Dictionary<(byte Span, ulong Id), HashSet<string>> BuildModelOwners(List<JObject> allSuits) {
			var owners = new Dictionary<(byte Span, ulong Id), HashSet<string>>();
			foreach (var suit in allSuits) {
				var itemPath = (string)suit["Item"];
				if (string.IsNullOrEmpty(itemPath)) continue;

				var paths = ResolveSuitModelPaths(itemPath);
				var suitName = (string)suit["Name"] ?? "an unnamed slot";
				foreach (var bodyPath in paths.BodyPaths) {
					AddModelOwner(FindModelAsset(bodyPath), suitName, owners);
				}
				if (!string.IsNullOrEmpty(paths.MaskPath)) {
					AddModelOwner(FindModelAsset(paths.MaskPath), suitName, owners);
				}
			}
			return owners;
		}

		private static void AddModelOwner(ModelAssetLocation? asset, string suitName, Dictionary<(byte Span, ulong Id), HashSet<string>> owners) {
			if (asset == null) return;
			var key = (asset.Span, asset.Id);
			if (!owners.TryGetValue(key, out var slots)) {
				slots = new HashSet<string>(System.StringComparer.Ordinal);
				owners.Add(key, slots);
			}
			slots.Add(suitName);
		}

		// Two passes on purpose. A plan can carry several transfers (body parts + mask); if one of
		// them was rejected for a shared target, the plan is dead as a whole, but the old
		// single-pass version kept scanning its *other* transfers anyway and let them occupy
		// targetTransfers -- so an unrelated, otherwise-valid plan could get rejected for
		// "conflicting" with a transfer that was never going to be written in the first place.
		// Resolving all shared-target rejections first, then only ever comparing surviving plans
		// against each other, means a plan can no longer fail because of a rejection that has
		// nothing to do with it.
		private List<SuitModelPlan> ValidatePlans(List<SuitModelPlan> candidates, Dictionary<(byte Span, ulong Id), HashSet<string>> owners) {
			var blockedPlans = new HashSet<SuitModelPlan>();

			foreach (var plan in candidates) {
				foreach (var transfer in plan.Transfers) {
					var targetKey = (transfer.Target.Span, transfer.Target.Id);
					if (owners.TryGetValue(targetKey, out var slots) && slots.Count > 1) {
						blockedPlans.Add(plan);
						Warn($"Change Suit Model: '{plan.SuitName}' targets '{transfer.Target.Path}', which is shared by {string.Join(", ", slots)}; skipped to keep suit slots isolated.");
					}
				}
			}

			var targetTransfers = new Dictionary<(byte Span, ulong Id), (SuitModelPlan Plan, ModelTransfer Transfer)>();
			foreach (var plan in candidates) {
				if (blockedPlans.Contains(plan)) continue;

				foreach (var transfer in plan.Transfers) {
					var targetKey = (transfer.Target.Span, transfer.Target.Id);

					// A stale entry from a plan blocked earlier in this same pass doesn't count
					// as an active conflict -- it was never going to be written either.
					if (targetTransfers.TryGetValue(targetKey, out var existing) && !blockedPlans.Contains(existing.Plan)) {
						if (existing.Transfer.Source.Span == transfer.Source.Span && existing.Transfer.Source.Id == transfer.Source.Id) continue;

						blockedPlans.Add(existing.Plan);
						blockedPlans.Add(plan);
						Warn($"Change Suit Model: '{existing.Plan.SuitName}' and '{plan.SuitName}' request different replacements for '{transfer.Target.Path}'; both swaps were skipped.");
						break; // this plan is dead; stop letting its remaining transfers occupy targets
					}

					targetTransfers[targetKey] = (plan, transfer);
				}
			}

			var result = new List<SuitModelPlan>();
			foreach (var plan in candidates) {
				if (!blockedPlans.Contains(plan)) {
					result.Add(plan);
				}
			}
			return result;
		}

		private Dictionary<(byte Span, ulong Id), ModelAssetData> SnapshotSourceAssets(List<ModelTransfer> transfers) {
			var sourceAssets = new Dictionary<(byte Span, ulong Id), ModelAssetData>();
			foreach (var transfer in transfers) {
				var sourceKey = (transfer.Source.Span, transfer.Source.Id);
				if (sourceAssets.ContainsKey(sourceKey)) continue;

				try {
					var stg = new STG();
					stg.Load(_toc, transfer.Source.Span, transfer.Source.Id);
					stg.ClearDat1(); // Copy original bytes verbatim; do not reserialize the model.
					sourceAssets.Add(sourceKey, new ModelAssetData(
						stg.Raw,
						(stg.HasFlag(STG.Flags.INSTALL_HEADER) ? stg.RawHeader : null),
						(stg.HasFlag(STG.Flags.INSTALL_TEXUTRE_META) ? stg.TextureMeta : null)
					));
				} catch {
					Warn($"Change Suit Model: source model '{transfer.Source.Path}' could not be read; skipped.");
				}
			}
			return sourceAssets;
		}

		// Walks a suit's reward-loadout config (the "Item" field of a SuitList.Suits entry) to find
		// every body model and its cutscene-mask .model asset path:
		//   reward loadout .config
		//     -> DefaultMaskModel.AssetPath                                  (mask .model, direct)
		//     -> DefaultLoadoutConfig.AssetPath -> item loadout .config
		//       -> Loadout.ItemLoadoutLists[].Items[].Item
		//         -> ModelList[].Model.AssetPath                             (body .model(s))
		private SuitModelPaths ResolveSuitModelPaths(string rewardLoadoutPath) {
			if (string.IsNullOrEmpty(rewardLoadoutPath)) return new SuitModelPaths(new(), null, "empty loadout path");
			rewardLoadoutPath = DAT1.Utils.Normalize(rewardLoadoutPath);
			if (_modelPathsCache.TryGetValue(rewardLoadoutPath, out var cached)) return cached;

			var bodyPaths = new List<string>();
			var bodyPathsSet = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
			string? mask = null;

			try {
				var reward = new Config_I30(_toc.GetAssetReader(rewardLoadoutPath));
				var rewardRoot = reward.ContentSection.Data;

				mask = (string)rewardRoot["DefaultMaskModel"]?["AssetPath"];
				if (!string.IsNullOrEmpty(mask)) {
					mask = DAT1.Utils.Normalize(mask);
					if (!mask.EndsWith(".model", System.StringComparison.OrdinalIgnoreCase)) {
						var failed = new SuitModelPaths(new(), null, "DefaultMaskModel is not a .model asset");
						_modelPathsCache[rewardLoadoutPath] = failed;
						return failed;
					}
				}

				var itemLoadoutPath = (string)rewardRoot["DefaultLoadoutConfig"]?["AssetPath"];
				if (!string.IsNullOrEmpty(itemLoadoutPath)) {
					itemLoadoutPath = DAT1.Utils.Normalize(itemLoadoutPath);
					var itemLoadout = new Config_I30(_toc.GetAssetReader(itemLoadoutPath));
					var loadoutLists = itemLoadout.ContentSection.Data["Loadout"]?["ItemLoadoutLists"] as JArray;

					// Vanilla suits carry models in vanity configs. SuitTool can point at a custom
					// vanity config instead, so inspect every loadout item without relying on a
					// particular path prefix.
					foreach (var listToken in loadoutLists ?? new JArray()) {
						if (listToken is not JObject list) continue;

						foreach (var entryToken in (list["Items"] as JArray) ?? new JArray()) {
							if (entryToken is not JObject entry) continue;

							var itemValue = (string?)entry["Item"];
							if (string.IsNullOrEmpty(itemValue)) continue;
							var itemPath = DAT1.Utils.Normalize(itemValue);

							try {
								var vanity = new Config_I30(_toc.GetAssetReader(itemPath));
								var modelLists = vanity.ContentSection.Data["ModelList"] as JArray;
								foreach (var modelToken in modelLists ?? new JArray()) {
									if (modelToken is not JObject ml) continue;
									var assetPath = (string)ml["Model"]?["AssetPath"];
									if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".model", System.StringComparison.OrdinalIgnoreCase)) {
										assetPath = DAT1.Utils.Normalize(assetPath);
										if (bodyPathsSet.Add(assetPath)) {
											bodyPaths.Add(assetPath);
										}
									}
								}
							} catch {} // this item isn't a body-carrying vanity config (or isn't present) -- try the next one
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

		private string GetForcedModelArchivePath(ModelAssetLocation target) {
			// TOC_I29 archive filenames are stored in a fixed 40-byte buffer (39 chars + null
			// terminator, see TOC_I29.AddNewArchive) -- keep this short.
			var modsPath = Path.Combine(_gamePath, "d", "mods");
			return Path.Combine(modsPath, $"sm_{target.Span:x2}_{target.Id:x16}");
		}

		private void WriteForcedModelAsset(ModelAssetLocation target, ModelAssetData sourceAsset) {
			var archivePath = GetForcedModelArchivePath(target);
			var archiveIndex = GetArchiveIndex(Path.GetRelativePath(_gamePath, archivePath));
			File.WriteAllBytes(archivePath, sourceAsset.Raw);
			OverwriteAsset(
				target.Span, target.Id,
				archiveIndex, /*offset=*/0, (uint)sourceAsset.Raw.Length,
				sourceAsset.Header,
				sourceAsset.TextureMeta
			);
		}
	}
}
