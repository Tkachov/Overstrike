// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Overstrike.Utils {
	internal enum MSM2SuitCharacter {
		Peter,
		Miles
	}

	// The only suit slots whose body/mask geometry is shown in a forced story cutscene: the
	// starting suits (no unlock gate) and the suits gated behind story missions rather than
	// being ordinary collectibles. Confirmed against toc.BAK: these are exactly SuitList.Suits[0..5],
	// in this order, and each has a progression pattern (no gate, HideAfterMissionName, or
	// ScriptedRequirement-only) that no ordinary collectible suit has.
	//
	// "Change Suit Model" (force_model/force_mask) exists to fix a forced cutscene rendering the
	// wrong body/mask on these slots. Applying it to any other slot has no cutscene to fix, so the
	// swap is refused there for both the UI and the installer.
	internal static class MSM2CutsceneSuits {
		public static readonly HashSet<string> SLOT_NAMES = new(System.StringComparer.Ordinal) {
			"i30_Advanced_Suit",     // Peter: Advanced Suit 2.0 (starting suit)
			"SUIT_BLACK",            // Peter: Black Suit
			"SUIT_SYMBIOTE",         // Peter: Symbiote Suit
			"AntiVenom_Suit",        // Peter: Anti-Venom Suit
			"SUIT_MILES_UPDATED",    // Miles: Updated Suit (starting suit)
			"SUIT_MILES_EVOLVE"      // Miles: Evolved Suit
		};

		public static bool IsEligible(string suitSlotName) => !string.IsNullOrEmpty(suitSlotName) && SLOT_NAMES.Contains(suitSlotName);
	}

	// Anti-Venom remains blocked because its signature suit power replaces the mechanical
	// Spider-Arms with tendrils. Symbiote Suit is allowed, but the UI explains that its selected
	// Spider-Arms only appear outside Rage Mode while that suit slot is active in-game.
	internal static class MSM2SpiderArmsBlockedSuits {
		public static readonly HashSet<string> SLOT_NAMES = new(System.StringComparer.Ordinal) {
			"AntiVenom_Suit"     // Peter: Anti-Venom Suit
		};

		public static bool IsBlocked(string suitSlotName) => !string.IsNullOrEmpty(suitSlotName) && SLOT_NAMES.Contains(suitSlotName);
	}

	internal static class MSM2SuitCharacterResolver {
		public static MSM2SuitCharacter? FromGameValue(string? value) {
			return value switch {
				"kSpiderManPeter" => MSM2SuitCharacter.Peter,
				"kSpiderManMiles" => MSM2SuitCharacter.Miles,
				_ => null
			};
		}

		public static MSM2SuitCharacter? FromGameValues(JArray? values) {
			MSM2SuitCharacter? result = null;
			foreach (var value in values ?? new JArray()) {
				var character = FromGameValue((string?)value);
				if (!character.HasValue) return null;
				if (result.HasValue && result.Value != character.Value) return null;
				result = character;
			}
			return result;
		}

		public static string DisplayName(MSM2SuitCharacter character) {
			return (character == MSM2SuitCharacter.Peter ? "Peter" : "Miles");
		}

		public static MSM2SuitCharacter? TryResolve(TOC_I29 toc, string? rewardLoadoutPath) {
			if (string.IsNullOrEmpty(rewardLoadoutPath)) return null;

			try {
				rewardLoadoutPath = DAT1.Utils.Normalize(rewardLoadoutPath);
				var reward = new Config_I30(toc.GetAssetReader(rewardLoadoutPath));
				var characters = reward.ContentSection.Data["ValidCharacters"] as JArray;
				return FromGameValues(characters);
			} catch {
				return null;
			}
		}
	}
}
