// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using Overstrike.Utils;
using System.Collections.Generic;
using System.IO;

namespace Overstrike.Installers {
	internal partial class SuitsMenuInstaller_MSM2 {
		private sealed class WebwingsRequest {
			public string SuitName { get; }
			public string TargetItemPath { get; }
			public string OptionId { get; }

			public WebwingsRequest(string suitName, string targetItemPath, string optionId) {
				SuitName = suitName;
				TargetItemPath = targetItemPath;
				OptionId = optionId;
			}
		}

		// Where a slot's Webwings appearance lives: the item loadout listing the wings equipment
		// config, and the vanity config carrying WingsuitLook/WingsuitActor.
		private sealed class WebwingsTargets {
			public string ItemLoadoutPath { get; }
			public List<string> VanityPaths { get; }

			public WebwingsTargets(string itemLoadoutPath, List<string> vanityPaths) {
				ItemLoadoutPath = itemLoadoutPath;
				VanityPaths = vanityPaths;
			}
		}

		private readonly Dictionary<string, WebwingsTargets?> _webwingsTargetsCache = new(System.StringComparer.OrdinalIgnoreCase);

		// Rewrites each selected slot's own item loadout and vanity config, the same way
		// ApplyForcedSpiderArms rewrites its reward loadout: the edited bytes go into the single
		// suits_menu archive, overwriting assets the game already knows instead of adding new ones.
		//
		// Both configs are refused whenever another suit slot also resolves to them, so one slot's
		// Webwings can never leak into another's. That check is what keeps the shared vanity
		// configs -- VanityBody_*, VanityHED* and Miles' venom x-ray overlay -- untouched.
		private List<SuitsMenuArchiveAsset> ApplyForcedWebwings(List<WebwingsRequest> requests, List<JObject> allSuits) {
			var result = new List<SuitsMenuArchiveAsset>();
			if (requests.Count == 0) return result;

			var itemLoadoutOwners = BuildWebwingsOwners(allSuits, /*vanity=*/false);
			var vanityOwners = BuildWebwingsOwners(allSuits, /*vanity=*/true);
			var written = new Dictionary<ulong, string>();

			foreach (var request in requests) {
				var option = MSM2Webwings.Find(request.OptionId)
					?? throw new InvalidDataException($"Unsupported Webwings selected for '{request.SuitName}'");

				var targetItemPath = DAT1.Utils.Normalize(request.TargetItemPath ?? "");
				if (string.IsNullOrEmpty(targetItemPath)) {
					throw new InvalidDataException($"Webwings target for '{request.SuitName}' has no reward loadout");
				}

				var character = ResolveSuitCharacter(targetItemPath)
					?? throw new InvalidDataException($"Could not determine the character of the Webwings target for '{request.SuitName}'");
				if (!MSM2Webwings.IsAvailableFor(option, character)) {
					throw new InvalidDataException($"'{option.DisplayName}' Webwings are not available on the {MSM2SuitCharacterResolver.DisplayName(character)} slot '{request.SuitName}'");
				}

				var targets = ResolveWebwingsTargets(targetItemPath)
					?? throw new InvalidDataException($"Could not resolve the item loadout of the Webwings target for '{request.SuitName}'");

				// Sharing an item loadout means the slot has no suit data of its own to change.
				if (itemLoadoutOwners.TryGetValue(CRC64.Hash(targets.ItemLoadoutPath), out var loadoutSlots) && loadoutSlots.Count > 1) {
					throw new InvalidDataException($"Webwings for '{request.SuitName}' would change item loadout '{targets.ItemLoadoutPath}', which is shared by {string.Join(", ", loadoutSlots)}");
				}

				var loadoutAsset = BuildWebwingsLoadoutAsset(request, option, targets.ItemLoadoutPath, written);
				if (loadoutAsset != null) result.Add(loadoutAsset);

				var vanityAsset = BuildWebwingsVanityAsset(request, option, targets.VanityPaths, vanityOwners, written);
				if (vanityAsset != null) result.Add(vanityAsset);
			}

			return result;
		}

		private SuitsMenuArchiveAsset? BuildWebwingsLoadoutAsset(WebwingsRequest request, MSM2WebwingsOption option, string itemLoadoutPath, Dictionary<ulong, string> written) {
			var config = new Config_I30(_toc.GetAssetReader(itemLoadoutPath));
			var root = config.ContentSection.Data;
			if (!RewriteWebwingsEquipment(root, option.Equipment)) return null; // already exactly what was asked for

			config.ContentSection.Data = root;
			var location = LocateWebwingsAsset(request, "item loadout", itemLoadoutPath, written);
			var bytes = config.Save();
			var header = PrepareConfigHeader(location.Id, bytes.Length, $"Item loadout '{itemLoadoutPath}'");
			return new SuitsMenuArchiveAsset(location.Span, location.Id, bytes, header);
		}

		private SuitsMenuArchiveAsset? BuildWebwingsVanityAsset(WebwingsRequest request, MSM2WebwingsOption option, List<string> vanityPaths, Dictionary<ulong, HashSet<string>> owners, Dictionary<ulong, string> written) {
			if (option.Look == null && option.Actor == null) return null;

			// One suit, one vanity config. Anything else cannot be attributed to this slot alone,
			// so the request is refused instead of guessing which config owns the wings.
			var candidates = SelectOwnWebwingsVanityPaths(vanityPaths, owners);
			if (candidates.Count != 1) {
				throw new InvalidDataException(candidates.Count == 0
					? $"Webwings for '{request.SuitName}' found no vanity config belonging to that slot alone"
					: $"Webwings for '{request.SuitName}' found {candidates.Count} vanity configs of its own and cannot tell which one carries the wings");
			}
			var vanityPath = candidates[0];

			var config = new Config_I30(_toc.GetAssetReader(vanityPath));
			var root = config.ContentSection.Data;

			var changed = false;
			if (option.Look != null && (string?)root["WingsuitLook"] != option.Look) {
				root["WingsuitLook"] = option.Look;
				changed = true;
			}
			if (option.Actor != null && (string?)root["WingsuitActor"] != option.Actor) {
				root["WingsuitActor"] = option.Actor;
				changed = true;
			}
			if (!changed) return null;

			config.ContentSection.Data = root;
			var location = LocateWebwingsAsset(request, "vanity config", vanityPath, written);
			var bytes = config.Save();
			var header = PrepareConfigHeader(location.Id, bytes.Length, $"Vanity config '{vanityPath}'");
			return new SuitsMenuArchiveAsset(location.Span, location.Id, bytes, header);
		}

		// VanityBody_*, VanityHED* and Miles' venom x-ray overlay all carry a ModelList, but each of
		// them belongs to dozens of slots. Only a config no other slot resolves to can describe this
		// suit -- and only such a config may be rewritten at all.
		private static List<string> SelectOwnWebwingsVanityPaths(List<string> vanityPaths, Dictionary<ulong, HashSet<string>> owners) {
			var result = new List<string>();
			foreach (var path in vanityPaths) {
				if (owners.TryGetValue(CRC64.Hash(path), out var slots) && slots.Count > 1) continue;
				result.Add(path);
			}
			return result;
		}

		// Drops every pure wings recolor already in the loadout, then adds the requested one.
		// Returns whether the item list actually changed.
		private bool RewriteWebwingsEquipment(JObject root, string? equipment) {
			var wanted = (equipment == null ? null : DAT1.Utils.Normalize(equipment));
			var changed = false;
			var hasWanted = false;

			if (root["Loadout"]?["ItemLoadoutLists"] is not JArray lists) return false;

			JArray? lastItems = null;
			foreach (var listToken in lists) {
				if (listToken is not JObject list) continue;
				if (list["Items"] is not JArray items) continue;
				lastItems = items;

				for (var i = items.Count - 1; i >= 0; --i) {
					if (items[i] is not JObject entry) continue;
					var itemValue = (string?)entry["Item"];
					if (string.IsNullOrEmpty(itemValue)) continue;

					var itemPath = DAT1.Utils.Normalize(itemValue);
					if (wanted != null && itemPath == wanted) {
						hasWanted = true;
						continue;
					}
					if (!IsWebwingsEquipment(itemPath)) continue;

					items.RemoveAt(i);
					changed = true;
				}
			}

			if (wanted != null && !hasWanted && lastItems != null) {
				lastItems.Add(new JObject { ["Item"] = equipment });
				changed = true;
			}
			return changed;
		}

		// A loadout item counts as a wings recolor only when every material override it carries
		// targets a Webwings material slot and it contributes no models of its own. That matches the
		// three wings equipment configs the game ships -- and the ones suit mods bring with them --
		// while never matching a suit body, a head, or the shared venom x-ray overlay.
		private bool IsWebwingsEquipment(string itemPath) {
			try {
				var config = new Config_I30(_toc.GetAssetReader(itemPath));
				var data = config.ContentSection.Data;

				if (data["ModelList"] is JArray models && models.Count > 0) return false;
				if (data["MaterialOverrides"] is not JArray overrides || overrides.Count == 0) return false;

				foreach (var overrideToken in overrides) {
					if (overrideToken is not JObject entry) return false;
					var slot = (string?)entry["MaterialMappingName"];
					if (string.IsNullOrEmpty(slot) || !MSM2Webwings.MATERIAL_SLOTS.Contains(slot)) return false;
				}
				return true;
			} catch {
				return false;
			}
		}

		private (byte Span, ulong Id) LocateWebwingsAsset(WebwingsRequest request, string kind, string path, Dictionary<ulong, string> written) {
			var assetIndex = _toc.FindFirstAssetIndexByPath(path);
			if (assetIndex < 0) throw new InvalidDataException($"{kind} '{path}' is not installed");
			var span = _toc.GetSpanIndexByAssetIndex(assetIndex);
			if (span == null) throw new InvalidDataException($"{kind} '{path}' has no span");

			var assetId = CRC64.Hash(path);
			if (written.TryGetValue(assetId, out var firstSuit)) {
				throw new InvalidDataException($"'{request.SuitName}' and '{firstSuit}' both change {kind} '{path}'");
			}
			written[assetId] = request.SuitName;

			return ((byte)span, assetId);
		}

		// Which slots resolve to each item loadout / vanity config, so a shared one is never
		// rewritten. Deleted slots are excluded by the caller, matching how the rest of the
		// installer ignores slots the menu will not show.
		private Dictionary<ulong, HashSet<string>> BuildWebwingsOwners(List<JObject> allSuits, bool vanity) {
			var owners = new Dictionary<ulong, HashSet<string>>();
			foreach (var suit in allSuits) {
				var itemPath = (string?)suit["Item"];
				if (string.IsNullOrEmpty(itemPath)) continue;

				var targets = ResolveWebwingsTargets(DAT1.Utils.Normalize(itemPath));
				if (targets == null) continue;

				var suitName = (string?)suit["Name"] ?? "an unnamed slot";
				if (vanity) {
					foreach (var vanityPath in targets.VanityPaths) {
						AddWebwingsOwner(owners, vanityPath, suitName);
					}
				} else {
					AddWebwingsOwner(owners, targets.ItemLoadoutPath, suitName);
				}
			}
			return owners;
		}

		private static void AddWebwingsOwner(Dictionary<ulong, HashSet<string>> owners, string path, string suitName) {
			var id = CRC64.Hash(path);
			if (!owners.TryGetValue(id, out var slots)) {
				slots = new HashSet<string>(System.StringComparer.Ordinal);
				owners.Add(id, slots);
			}
			slots.Add(suitName);
		}

		// reward loadout .config
		//   -> DefaultLoadoutConfig.AssetPath -> item loadout .config   (holds the wings equipment)
		//     -> Loadout.ItemLoadoutLists[].Items[].Item
		//       -> any item carrying a ModelList                        (candidate vanity configs)
		//
		// Narrowing those candidates to the one config belonging to this slot alone is left to
		// SelectOwnWebwingsVanityPaths, which needs the ownership map of every slot.
		private WebwingsTargets? ResolveWebwingsTargets(string rewardLoadoutPath) {
			if (string.IsNullOrEmpty(rewardLoadoutPath)) return null;
			rewardLoadoutPath = DAT1.Utils.Normalize(rewardLoadoutPath);
			if (_webwingsTargetsCache.TryGetValue(rewardLoadoutPath, out var cached)) return cached;

			WebwingsTargets? result = null;
			try {
				var reward = new Config_I30(_toc.GetAssetReader(rewardLoadoutPath));
				var itemLoadoutPath = (string?)reward.ContentSection.Data["DefaultLoadoutConfig"]?["AssetPath"];
				if (!string.IsNullOrEmpty(itemLoadoutPath)) {
					itemLoadoutPath = DAT1.Utils.Normalize(itemLoadoutPath);

					var itemLoadout = new Config_I30(_toc.GetAssetReader(itemLoadoutPath));
					var lists = itemLoadout.ContentSection.Data["Loadout"]?["ItemLoadoutLists"] as JArray;

					var vanityPaths = new List<string>();
					var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
					foreach (var listToken in lists ?? new JArray()) {
						if (listToken is not JObject list) continue;

						foreach (var entryToken in (list["Items"] as JArray) ?? new JArray()) {
							if (entryToken is not JObject entry) continue;
							var itemValue = (string?)entry["Item"];
							if (string.IsNullOrEmpty(itemValue)) continue;

							var itemPath = DAT1.Utils.Normalize(itemValue);
							try {
								var item = new Config_I30(_toc.GetAssetReader(itemPath));
								if (item.ContentSection.Data["ModelList"] is not JArray models || models.Count == 0) continue;
								if (seen.Add(itemPath)) vanityPaths.Add(itemPath);
							} catch {} // not a readable vanity config -- try the next item
						}
					}

					result = new WebwingsTargets(itemLoadoutPath, vanityPaths);
				}
			} catch {
				result = null;
			}

			_webwingsTargetsCache[rewardLoadoutPath] = result;
			return result;
		}
	}
}
