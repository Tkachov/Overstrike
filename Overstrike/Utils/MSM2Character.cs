// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Overstrike.Utils {
	internal enum MSM2Character {
		Peter,
		Miles
	}

	// Story-gated slots that can opt out of the normal suit progression rules.
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

	internal static class MSM2SuitCharacterResolver {
		public static MSM2Character? FromGameValue(string? value) {
			return value switch {
				"kSpiderManPeter" => MSM2Character.Peter,
				"kSpiderManMiles" => MSM2Character.Miles,
				_ => null
			};
		}

		public static MSM2Character? FromGameValues(JArray? values) {
			MSM2Character? result = null;
			foreach (var value in values ?? new JArray()) {
				var character = FromGameValue((string?)value);
				if (!character.HasValue) return null;
				if (result.HasValue && result.Value != character.Value) return null;
				result = character;
			}
			return result;
		}

		public static string DisplayName(MSM2Character character) {
			return (character == MSM2Character.Peter ? "Peter" : "Miles");
		}

		public static MSM2Character? TryResolve(TOC_I29 toc, string? rewardLoadoutPath) {
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
