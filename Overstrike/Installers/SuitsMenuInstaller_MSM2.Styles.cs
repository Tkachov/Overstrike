// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using Overstrike.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using static DAT1.Sections.Generic.ReferencesSection;

namespace Overstrike.Installers {
	internal partial class SuitsMenuInstaller_MSM2 {
		private sealed class StyleSourceRequest {
			public string SuitName { get; }
			public string SourceSuitName { get; }

			public StyleSourceRequest(string suitName, string sourceSuitName) {
				SuitName = suitName;
				SourceSuitName = sourceSuitName;
			}
		}

		// Styles only repaint the model they were authored for, so a slot wearing another suit's
		// model has no use for its own styles: their material names describe a model it no longer
		// wears. The donor is therefore never chosen -- it is always the suit the model came from.
		private static void ResolveAutomaticStyleSources(List<(string SuitName, string ModelSourcePath)> pending, List<JObject> allSuits, List<StyleSourceRequest> requests) {
			if (pending.Count == 0) return;

			var nameByItem = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var suit in allSuits) {
				var item = (string?)suit["Item"];
				var name = (string?)suit["Name"];
				if (string.IsNullOrEmpty(item) || string.IsNullOrEmpty(name)) continue;
				nameByItem.TryAdd(DAT1.Utils.Normalize(item), name);
			}

			foreach (var (suitName, modelSourcePath) in pending) {
				// A model can come from a loadout no slot in the menu owns -- a suit mod's own
				// config, for one. There is no donor slot to read styles off, so leave it alone.
				if (!nameByItem.TryGetValue(DAT1.Utils.Normalize(modelSourcePath), out var donor)) continue;
				if (donor == suitName) continue;

				requests.Add(new StyleSourceRequest(suitName, donor));
			}
		}

		// A suit's styles live in two places, and both have to agree:
		//
		//   system_progression.config  VariantGroup.Variants[]  -- the cards the menu draws
		//   ItemLoadout_<suit>_Variant_Group.config  Items[]     -- what equipping one actually applies
		//
		// Rewriting only the first swaps the icons and leaves the suit wearing its original styles,
		// which is exactly what the first build did. So the slots are filled in both places.
		//
		// Every path involved already exists and is already referenced by system_progression, so no
		// new asset is created here -- the item loadout is the one config that gets rewritten.
		private List<SuitsMenuArchiveAsset> ApplyStyleSources(List<StyleSourceRequest> requests, List<JObject> allSuits) {
			var assets = new List<SuitsMenuArchiveAsset>();
			if (requests.Count == 0) return assets;

			var suitsByName = new Dictionary<string, JObject>(StringComparer.Ordinal);
			foreach (var suit in allSuits) {
				var name = (string?)suit["Name"];
				if (!string.IsNullOrEmpty(name)) suitsByName[name] = suit;
			}

			// Every request here is the installer following a model change, not something the user
			// asked for on its own. A pair that cannot take part is passed over in silence rather
			// than failing an install nobody aimed at styles.
			var written = new Dictionary<ulong, string>();
			foreach (var request in requests) {
				if (!suitsByName.TryGetValue(request.SuitName, out var target)) continue;
				if (!suitsByName.TryGetValue(request.SourceSuitName, out var source)) continue;
				if (target["VariantGroup"] is not JObject targetGroup || targetGroup["Variants"] is not JArray targetVariants) continue;
				if (source["VariantGroup"] is not JObject sourceGroup || sourceGroup["Variants"] is not JArray sourceVariants) continue;
				if (sourceVariants.Count == 0 || targetVariants.Count == 0) continue;

				// Fill every slot the target owns, never adding one: the game does not show more
				// than three styles, and a donor with fewer styles just repeats its last one.
				//
				// Each slot keeps its own identity and keeps whatever gates it -- the Ultimate suits
				// carry a RequiredLevelIndex per style, and the save file knows those styles by the
				// names written here. Taking the donor's whole entry threw both away and the game
				// answered by hiding the suit's styles entirely. Only the look is borrowed.
				var equipPaths = new List<string>();
				for (var i = 0; i < targetVariants.Count; ++i) {
					var donor = (JObject)sourceVariants[Math.Min(i, sourceVariants.Count - 1)];

					var equipPath = (string?)donor["Item"];
					if (string.IsNullOrEmpty(equipPath)) {
						throw new InvalidDataException($"Style source: a style of '{request.SourceSuitName}' has no item config");
					}
					equipPaths.Add(equipPath);

					var slot = (JObject)((JObject)targetVariants[i]).DeepClone();
					slot["Item"] = equipPath;
					if (donor["Icon"] is JToken donorIcon) {
						slot["Icon"] = donorIcon.DeepClone();
					}
					targetVariants[i] = slot;
				}

				DropUltimateStyleFraming(target, targetGroup, "its styles are now borrowed");
				assets.Add(BuildStyleLoadoutAsset(request, targetGroup, equipPaths, written));
				ErrorLogger.WriteInfo($"[i] '{request.SuitName}' takes its {targetVariants.Count} style slots from '{request.SourceSuitName}'\n");
			}

			return assets;
		}

		// Black Suit, Symbiote and Anti-Venom are the only slots the game treats as owning "Ultimate"
		// styles, and that framing lives entirely in system_progression: HasUltimateStyles on the
		// slot, UnlockVariantMessageText on the group.
		//
		// The Ultimate panel draws its three styles off the per-style RequiredLevelIndex. Leave the
		// framing on after those levels stop describing the styles -- because they were borrowed, or
		// because they were unlocked outright -- and the panel draws nothing at all: the suit looks
		// like it never had styles. So whoever invalidates the levels also drops the framing, which
		// moves the slot to the ordinary styles panel and keeps the three styles visible.
		private static void DropUltimateStyleFraming(JObject suit, JObject group, string reason) {
			if ((bool?)suit["HasUltimateStyles"] != true) return;

			suit["HasUltimateStyles"] = false;
			group.Remove("UnlockVariantMessageText");
			ErrorLogger.WriteInfo($"[i] '{(string?)suit["Name"]}' drops its Ultimate styles framing, since {reason}\n");
		}

		private SuitsMenuArchiveAsset BuildStyleLoadoutAsset(StyleSourceRequest request, JObject targetGroup, List<string> equipPaths, Dictionary<ulong, string> written) {
			var itemLoadoutPath = ResolveStyleItemLoadoutPath(request, targetGroup);

			var assetIndex = _toc.FindFirstAssetIndexByPath(itemLoadoutPath);
			if (assetIndex < 0) throw new InvalidDataException($"Style source: item loadout '{itemLoadoutPath}' is not installed");
			var span = _toc.GetSpanIndexByAssetIndex(assetIndex);
			if (span == null) throw new InvalidDataException($"Style source: item loadout '{itemLoadoutPath}' has no span");

			var assetId = CRC64.Hash(itemLoadoutPath);
			if (written.TryGetValue(assetId, out var firstSuit)) {
				throw new InvalidDataException($"'{request.SuitName}' and '{firstSuit}' both change item loadout '{itemLoadoutPath}'");
			}
			written[assetId] = request.SuitName;

			var config = new Config_I30(_toc.GetAssetReader(itemLoadoutPath));
			var root = config.ContentSection.Data;
			if (root["Loadout"]?["ItemLoadoutLists"] is not JArray lists || lists.Count == 0 || lists[0] is not JObject list) {
				throw new InvalidDataException($"Style source: item loadout '{itemLoadoutPath}' has no loadout list");
			}

			var items = new JArray();
			foreach (var equipPath in equipPaths) {
				items.Add(new JObject {
					["AutoEquip"] = false,
					["Item"] = equipPath
				});
			}
			list["Items"] = items;
			config.ContentSection.Data = root;

			// This config references nothing but the styles it lists, so the donor's paths replace
			// the target's entirely instead of piling up alongside them.
			ReplaceConfigReferences(config, equipPaths);

			var bytes = config.Save();
			var header = PrepareConfigHeader(assetId, bytes.Length, $"Item loadout '{itemLoadoutPath}'");
			return new SuitsMenuArchiveAsset((byte)span, assetId, bytes, header);
		}

		// VariantGroup.Item is a VanityVariantGroupItemConfig, and the list of equippable styles
		// hangs off its ItemLoadoutConfig.
		private string ResolveStyleItemLoadoutPath(StyleSourceRequest request, JObject targetGroup) {
			var groupPath = (string?)targetGroup["Item"];
			if (string.IsNullOrEmpty(groupPath)) {
				throw new InvalidDataException($"Style source: '{request.SuitName}' has no variant group config");
			}
			groupPath = DAT1.Utils.Normalize(groupPath);

			var groupConfig = new Config_I30(_toc.GetAssetReader(groupPath));
			var itemLoadoutPath = (string?)groupConfig.ContentSection.Data["ItemLoadoutConfig"]?["AssetPath"];
			if (string.IsNullOrEmpty(itemLoadoutPath)) {
				throw new InvalidDataException($"Style source: variant group '{groupPath}' names no item loadout");
			}

			return DAT1.Utils.Normalize(itemLoadoutPath);
		}

		// Config_I30.Save() rebuilds the references section from the strings its entries point at,
		// so a new reference has to exist as a string before saving.
		private static void ReplaceConfigReferences(Config_I30 config, List<string> paths) {
			if (!config.HasSection(DAT1.Sections.Config.ConfigReferencesSection.TAG)) return;

			var entries = config.ReferencesSection.Entries;
			entries.Clear();

			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var path in paths) {
				if (!seen.Add(path)) continue;

				var extension = path[path.LastIndexOf('.')..];
				entries.Add(new ReferenceEntry() {
					AssetId = CRC64.Hash(path),
					AssetPathStringOffset = config.AddString(path, true),
					ExtensionHash = CRC32.Hash(extension)
				});
			}
		}
	}
}
