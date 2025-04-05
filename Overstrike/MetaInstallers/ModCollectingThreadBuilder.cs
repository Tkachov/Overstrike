// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Data;
using Overstrike.Games;
using Overstrike.Installers;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Overstrike.MetaInstallers {
	internal class ModCollectingThreadBuilder {
		private readonly Profile _selectedProfile;
		private readonly Dictionary<string, ModEntry> _availableMods;

		internal Action? OnStart;
		internal Action<int, int, string>? OnOperationStart;
		internal Action<string, Exception>? OnException;
		internal Action<List<ModEntry>>? OnSuccess;

		internal ModCollectingThreadBuilder(Profile selectedProfile, List<ModEntry> mods) {
			_selectedProfile = selectedProfile;
			
			_availableMods = new();
			foreach (var mod in mods) {
				_availableMods.Add(mod.Path, mod);
			}

			var selectedGameHasSuitsMenu = (_selectedProfile.Game == GameMSMR.ID || _selectedProfile.Game == GameMM.ID);
			if (selectedGameHasSuitsMenu) {
				var path = ModEntry.SUITS_MENU_PATH;
				var stub = new ModEntry("Suits Menu", path, ModEntry.ModType.SUITS_MENU);
				var suitsMenuEntry = new ModEntry(stub, true, mods.Count, null);
				_availableMods.Add(suitsMenuEntry.Path, suitsMenuEntry);
			}
		}

		internal Thread Build() {
			return new Thread(() => CollectModsToInstall(_selectedProfile, _availableMods));
		}

		private void CollectModsToInstall(Profile profile, Dictionary<string, ModEntry> availableMods) {
			OnStart?.Invoke();

			var modsToInstall = new List<ModEntry>();
			var ndx = 0;
			foreach (var mod in profile.Mods) {
				++ndx;
				if (!mod.Install) continue;
				if (!availableMods.ContainsKey(mod.Path)) continue; // TODO: should not happen?

				OnOperationStart?.Invoke(ndx, profile.Mods.Count, availableMods[mod.Path].Name);

				try {
					AddEntriesToInstall(modsToInstall, availableMods[mod.Path], mod);
				} catch (Exception ex) {
					OnException?.Invoke(availableMods[mod.Path].Name, ex);
					return;
				}
			}

			if (profile.Settings_Scripts_Enabled) {
				modsToInstall.Insert(0, new ScriptSupportModEntry(modsToInstall)); // TODO: pass callback(s) if needed
			}

			OnSuccess?.Invoke(modsToInstall);
		}

		private void AddEntriesToInstall(List<ModEntry> modsToInstall, ModEntry libraryMod, ModEntry profileMod) {
			if (ModEntry.IsTypeFamilyModular(libraryMod.Type)) {
				ModularInstaller.AddEntriesToInstall(modsToInstall, libraryMod, profileMod);
				return;
			}

			if (ModEntry.IsTypeFamilyScript(libraryMod.Type)) {
				if (!_selectedProfile.Settings_Scripts_Enabled) {
					return;
				}
			}

			modsToInstall.Add(libraryMod);
		}
	}
}
