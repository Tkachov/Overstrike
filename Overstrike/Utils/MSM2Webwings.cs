// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;

namespace Overstrike.Utils {
	// Every Webwings appearance MSM2 ships, and how the game selects it.
	//
	// The wings are one actor and one model shared by both heroes
	// ("characters/hero/hero_spiderman_webwings/hero_spiderman_webwings.actor", named by
	// configs/hero/hero_wingsuitconfig.config as DefaultWingsuitActor). What varies per suit is:
	//
	//  1. WingsuitLook -- a string on the suit's vanity (VanityTOR1) config picking one of the looks
	//     baked into hero_spiderman_webwings.model: "spiderman_advanced" (the global default),
	//     "spiderman_blacksuit" and "miles_webwing". Each look resolves one material slot
	//     (hero_spiderman_webwings_advanced / hero_spiderman_webwings_symbiote /
	//     hero_spiderman_miles_webwings). Vanilla only sets it on Peter's black-suit family; every
	//     other suit relies on the per-character default.
	//  2. A wings equipment config in the suit's item loadout, recoloring the wings through
	//     MaterialOverrides on those same material slots. Vanilla ships exactly three:
	//     equip_peter_itsvnoir_wings, equip_miles_evolve_wings and equip_miles_atsv_wings. (Suit
	//     *variant* equipment configs recolor the wings too, but inseparably from the variant's
	//     other overrides, so they are not offered here.)
	//  3. WingsuitActor -- also on the vanity config; replaces the whole wings actor. The Civil War
	//     suit is the only vanilla user, with its own solid-wing model, animset and ragdoll.
	//
	// A look and a material override have to agree on the same material slot, so each option below
	// states the complete result instead of letting the two be combined freely.
	internal sealed class MSM2WebwingsOption {
		public string Id { get; }
		public string DisplayName { get; }
		public MSM2Character Character { get; }
		// null = leave the suit's WingsuitLook untouched.
		public string? Look { get; }
		// null = leave WingsuitActor untouched; "" clears it back to the default wings actor, which
		// is exactly what vanilla's ITSV Noir suit stores.
		public string? Actor { get; }
		// null = this option wants no wings equipment config (any existing one is still removed).
		public string? Equipment { get; }

		public MSM2WebwingsOption(string id, string displayName, MSM2Character character, string? look, string? actor, string? equipment) {
			Id = id;
			DisplayName = displayName;
			Character = character;
			Look = look;
			Actor = actor;
			Equipment = equipment;
		}
	}

	internal static class MSM2Webwings {
		public const string CIVILWAR_WINGSUIT_ACTOR = "characters/hero/hero_spiderman_civilwar/hero_spiderman_civilwar_wings_art.actor";

		public const string LOOK_PETER_ADVANCED = "spiderman_advanced";
		public const string LOOK_PETER_BLACKSUIT = "spiderman_blacksuit";
		public const string LOOK_MILES = "miles_webwing";

		// The material slots of hero_spiderman_webwings.model. A loadout item that overrides only
		// these -- and contributes no models of its own -- is a pure wings recolor, so replacing a
		// suit's Webwings means dropping it. Everything else in the loadout is left alone.
		public static readonly HashSet<string> MATERIAL_SLOTS = new(System.StringComparer.OrdinalIgnoreCase) {
			"hero_spiderman_webwings_advanced",
			"hero_spiderman_webwings_symbiote",
			"hero_spiderman_miles_webwings"
		};

		public static readonly List<MSM2WebwingsOption> OPTIONS = new() {
			new MSM2WebwingsOption(
				"peter_advanced", "Advanced Suit 2.0",
				MSM2Character.Peter, LOOK_PETER_ADVANCED, "", null),
			new MSM2WebwingsOption(
				"peter_blacksuit", "Symbiote / Black Suit",
				MSM2Character.Peter, LOOK_PETER_BLACKSUIT, "", null),
			new MSM2WebwingsOption(
				"peter_itsvnoir", "Into the Spider-Verse Noir Suit",
				MSM2Character.Peter, LOOK_PETER_ADVANCED, "", "configs/equipment/equip_peter_itsvnoir_wings.config"),
			new MSM2WebwingsOption(
				"peter_civilwar", "Civil War Suit (solid wings)",
				MSM2Character.Peter, null, CIVILWAR_WINGSUIT_ACTOR, null),
			new MSM2WebwingsOption(
				"miles_default", "Miles' Webwings",
				MSM2Character.Miles, LOOK_MILES, "", null),
			new MSM2WebwingsOption(
				"miles_evolve", "Evolved Suit (teal)",
				MSM2Character.Miles, LOOK_MILES, "", "configs/equipment/equip_miles_evolve_wings.config"),
			new MSM2WebwingsOption(
				"miles_atsv", "Across the Spider-Verse Suit",
				MSM2Character.Miles, LOOK_MILES, "", "configs/equipment/equip_miles_atsv_wings.config")
		};

		public static MSM2WebwingsOption? Find(string? id) {
			if (string.IsNullOrEmpty(id)) return null;
			foreach (var option in OPTIONS) {
				if (option.Id == id) return option;
			}
			return null;
		}
	}
}
