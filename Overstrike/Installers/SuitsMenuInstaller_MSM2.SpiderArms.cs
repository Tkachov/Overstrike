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
		private sealed class SpiderArmsRequest {
			public string SuitName { get; }
			public string TargetItemPath { get; }
			public string ArmsModel { get; }

			public SpiderArmsRequest(string suitName, string targetItemPath, string armsModel) {
				SuitName = suitName;
				TargetItemPath = targetItemPath;
				ArmsModel = armsModel;
			}
		}

		// Applies a slot's Spider-Arms choice by editing the reward loadout the slot already
		// points at, overwriting that existing asset in place.
		//
		// An earlier version wrote a modified *copy* to a new path and repointed the slot's
		// "Item" at it. The copy was present in the TOC, parsed correctly, held the right enum
		// value and was listed in system_progression's references with the right hashes -- and
		// the game still dropped every slot that used one. Overwriting an asset the game already
		// knows avoids introducing a new asset id at all; it is the same thing this installer
		// already does to system_progression.config on every install.
		//
		// The trade-off is that the loadout is shared: if more than one slot resolves to it, the
		// arms would change on all of them, so those requests are refused instead.
		private void ApplyForcedSpiderArms(List<SpiderArmsRequest> requests, List<JObject> allSuits) {
			if (requests.Count == 0) return;

			var owners = BuildLoadoutOwners(allSuits);
			var written = new Dictionary<ulong, string>();

			foreach (var request in requests) {
				if (!SPIDER_ARMS_MODELS.Contains(request.ArmsModel)) {
					Warn($"Spider-Arms: '{request.SuitName}' selected an unsupported arm model; skipped.");
					continue;
				}

				var targetItemPath = (string.IsNullOrEmpty(request.TargetItemPath) ? null : DAT1.Utils.Normalize(request.TargetItemPath));
				if (string.IsNullOrEmpty(targetItemPath)) {
					Warn($"Spider-Arms: '{request.SuitName}' has no reward loadout; skipped.");
					continue;
				}
				if (ResolveSuitCharacter(targetItemPath) != MSM2SuitCharacter.Peter) {
					Warn($"Spider-Arms: '{request.SuitName}' is not a Peter loadout; skipped.");
					continue;
				}

				var assetIndex = _toc.FindFirstAssetIndexByPath(targetItemPath);
				if (assetIndex < 0) {
					Warn($"Spider-Arms: reward loadout '{targetItemPath}' for '{request.SuitName}' is not installed; skipped.");
					continue;
				}
				var span = _toc.GetSpanIndexByAssetIndex(assetIndex);
				if (span == null) {
					Warn($"Spider-Arms: reward loadout '{targetItemPath}' for '{request.SuitName}' has no span; skipped.");
					continue;
				}
				var originalHeader = _toc.GetHeaderByAssetIndex(assetIndex);
				if (originalHeader == null) {
					Warn($"Spider-Arms: reward loadout '{targetItemPath}' for '{request.SuitName}' has no asset header; skipped.");
					continue;
				}
				var assetId = CRC64.Hash(targetItemPath);

				if (owners.TryGetValue(assetId, out var slots) && slots.Count > 1) {
					Warn($"Spider-Arms: '{request.SuitName}' uses reward loadout '{targetItemPath}', which is shared by {string.Join(", ", slots)}; skipped to keep suit slots isolated. Clear \"Change Suit Slot\" on this slot if you want its own arms.");
					continue;
				}
				if (written.TryGetValue(assetId, out var firstSuit)) {
					Warn($"Spider-Arms: '{request.SuitName}' and '{firstSuit}' both target reward loadout '{targetItemPath}'; only the first was applied.");
					continue;
				}

				byte[] bytes;
				try {
					var config = new Config_I30(_toc.GetAssetReader(targetItemPath));
					var root = config.ContentSection.Data;
					var armsValue = new JObject {
						["Dynamic_Enum_Value_Type"] = new JObject {
							["EnumAsset"] = "enums/hero_ironarmsmodel.dynamicenum",
							["EnumValue"] = request.ArmsModel
						}
					};
					root["DefaultIronArmsModel"] = armsValue;
					root["DamagedIronArmsModel"] = armsValue.DeepClone();
					config.ContentSection.Data = root;
					bytes = config.Save();
				} catch {
					Warn($"Spider-Arms: '{request.SuitName}' could not rewrite its reward loadout; skipped.");
					continue;
				}

				// File/TOC writes deliberately stay outside the catch above. If either fails,
				// abort the installation so Finish() cannot persist a partially updated TOC.
				if (WriteSpiderArmsArchive((byte)span, assetId, originalHeader, bytes)) {
					written[assetId] = request.SuitName;
				}
			}
		}

		// Which slots resolve to each reward loadout asset, so a shared one is never rewritten.
		private Dictionary<ulong, HashSet<string>> BuildLoadoutOwners(List<JObject> allSuits) {
			var owners = new Dictionary<ulong, HashSet<string>>();
			foreach (var suit in allSuits) {
				var itemPath = (string?)suit["Item"];
				if (string.IsNullOrEmpty(itemPath)) continue;

				var id = CRC64.Hash(DAT1.Utils.Normalize(itemPath));
				if (!owners.TryGetValue(id, out var slots)) {
					slots = new HashSet<string>(System.StringComparer.Ordinal);
					owners.Add(id, slots);
				}
				slots.Add((string?)suit["Name"] ?? "an unnamed slot");
			}
			return owners;
		}

		// One asset per archive at offset 0, matching how .suit mods write the configs they add.
		//
		// The header's first pair carries the asset's byte size in its low 28 bits, and the game
		// relies on it: every config asset in the game keeps header size == TOC size (verified
		// across 470 sampled configs, zero exceptions). Leaving a rewritten config's header at
		// the pre-edit size makes the game crash on startup. Only that size is touched -- the
		// rest of the pair (including the flags nibble) doesn't need to be preserved; zeroing it
		// loads fine, same as the recompute below does for system_progression.config.
		// Returns false (writes nothing) if the header has no size pair to update -- never observed
		// in practice (470/470 sampled configs had one), but writing a header we can't correct the
		// size on risks the exact "header size != TOC size" crash this function exists to prevent.
		private bool WriteSpiderArmsArchive(byte span, ulong assetId, byte[] originalHeader, byte[] bytes) {
			var header = new AssetHeaderHelper(originalHeader);
			if (header.Pairs.Count == 0) {
				Warn($"Spider-Arms: reward loadout header for asset {assetId:x16} has no size field to update; skipped to avoid writing a stale size.");
				return false;
			}
			header.Pairs[0].A = (uint)bytes.Length;

			var modsPath = Path.Combine(_gamePath, "d", "mods");
			var archivePath = Path.Combine(modsPath, $"sa_{span:x2}_{assetId:x16}");
			if (!RegisterGeneratedArchive(archivePath)) return false;

			var archiveIndex = GetArchiveIndex(Path.GetRelativePath(_gamePath, archivePath));
			File.WriteAllBytes(archivePath, bytes);
			OverwriteAsset(span, assetId, archiveIndex, /*offset=*/0, (uint)bytes.Length, header.Save(), null);
			return true;
		}
	}
}
