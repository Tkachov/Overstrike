// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using Overstrike.Data;
using Overstrike.Games;
using Overstrike.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Overstrike.MetaInstallers {
	internal class ModInstallingThreadBuilder {
		private AppSettings _settings;
		private readonly Profile _selectedProfile;
		private readonly List<ModEntry> _modsToInstall;
		private readonly string _game;
		private readonly string _gamePath;
		private readonly bool _uninstalling;

		internal Action<int>? OnOperationsStarted;
		internal Action<int, int, string>? OnOperationStarts;
		internal Action<int, int>? OnOperationsFinalizing;
		internal Action<int>? OnOperationsFinished;
		internal Action? OnErrorOccurred_BeforeWritingTrace;
		internal Action? OnErrorOccurred_AfterTraceSaved;
		
		private GameBase _selectedGame => GameBase.GetGame(_selectedProfile.Game);

		internal ModInstallingThreadBuilder(AppSettings settings, Profile selectedProfile, List<ModEntry> modsToInstall, bool uninstalling = false) {
			_settings = settings;
			_selectedProfile = selectedProfile;
			_modsToInstall = modsToInstall;
			_game = selectedProfile.Game;
			_gamePath = selectedProfile.GamePath;
			_uninstalling = uninstalling;
		}

		internal Thread Build() {
			return new Thread(() => InstallMods(_modsToInstall, _game, _gamePath, _uninstalling));
		}

		private void InstallMods(List<ModEntry> modsToInstall, string game, string gamePath, bool uninstalling) {
			var errorOccurred = false;

			try {
				ErrorLogger.StartSession();
				ErrorLogger.WriteInfo($"Overstrike {Assembly.GetExecutingAssembly().GetName().Version}\n");
				ErrorLogger.WriteInfo(uninstalling ? $"Uninstalling mods at {DateTime.Now}\n" : $"Installing {modsToInstall.Count} mods at {DateTime.Now}\n");
				ErrorLogger.WriteInfo($"{game} located at {gamePath}\n");
				ErrorLogger.WriteInfo("\n");

				if (!uninstalling && modsToInstall.Count > 0) {
					ErrorLogger.WriteInfo("Mods to be installed:\n");
					foreach (var mod in modsToInstall) {
						ErrorLogger.WriteInfo($"- {mod.Name}\n");
					}
					ErrorLogger.WriteInfo("\n");
				}

				var operationsCount = modsToInstall.Count;
				OnOperationsStarted?.Invoke(operationsCount);

				var installer = GetMetaInstaller(game, gamePath);
				installer.Prepare();

				if (modsToInstall.Count > 0) {
					installer.Start();

					var index = 0;
					foreach (var mod in modsToInstall) {
						ErrorLogger.WriteInfo($"Installing '{mod.Name}'...");
						OnOperationStarts?.Invoke(index, operationsCount, mod.Name);

						installer.Install(mod, index++);
						ErrorLogger.WriteInfo(" OK!\n");
					}

					ErrorLogger.WriteInfo($"Saving 'toc'...");
					OnOperationsFinalizing?.Invoke(index, operationsCount);

					installer.Finish();
					ErrorLogger.WriteInfo(" OK!\n");
				}

				if (uninstalling)
					installer.Uninstall();

				OnOperationsFinished?.Invoke(operationsCount);
				ErrorLogger.WriteInfo("\nDone.\n");
			} catch (Exception ex) {
				errorOccurred = true;

				OnErrorOccurred_BeforeWritingTrace?.Invoke();

				ErrorLogger.WriteError("\n\nError occurred:\n");

				var stackTrace = $"{ex}\n";
				EnrichStackTrace(ref stackTrace, modsToInstall, game, gamePath);
				ErrorLogger.WriteError(stackTrace);

				ErrorLogger.WriteError($"{new StackTrace()}\n");
				ErrorLogger.WriteError("\n");
			}

			try { ErrorLogger.EndSession(); } catch {}

			if (errorOccurred)
				OnErrorOccurred_AfterTraceSaved?.Invoke();

			if (!errorOccurred) {
				try {
					UpdateTocSha(_selectedGame.GetTocPath(gamePath));
				} catch {}
			}
		}

		private MetaInstallerBase GetMetaInstaller(string game, string gamePath) {
			return _selectedGame.GetMetaInstaller(gamePath, _settings, _selectedProfile);
		}

		private static void EnrichStackTrace(ref string stackTrace, List<ModEntry> modsToInstall, string game, string gamePath) {
			try {
				var i = stackTrace.IndexOf("InstallMods");
				i = stackTrace.IndexOf(")", i);

				var version = $"{Assembly.GetExecutingAssembly().GetName().Version}";
				version = version.Replace(".", "");

				var suits = 0;
				var styles = 0;
				var stages = 0;
				var modulars = 0;
				var scripts = 0;
				var menu = 0;
				foreach (var mod in modsToInstall) {
					switch (mod.Type) {
						case ModEntry.ModType.SUIT_MSMR:
						case ModEntry.ModType.SUIT_MM:
						case ModEntry.ModType.SUIT_MM_V2:
						case ModEntry.ModType.SUIT2_MSM2:
							++suits;
							break;

						case ModEntry.ModType.SUIT_STYLE_MSM2:
							++styles;
							break;

						case ModEntry.ModType.STAGE_MSMR:
						case ModEntry.ModType.STAGE_MM:
						case ModEntry.ModType.STAGE_RCRA:
						case ModEntry.ModType.STAGE_RCRA_V2:
						case ModEntry.ModType.STAGE_I30:
						case ModEntry.ModType.STAGE_I33:
						case ModEntry.ModType.STAGE_MSM2:
						case ModEntry.ModType.STAGE_MSM2_V2:
							++stages;
							break;

						case ModEntry.ModType.MODULAR_MSMR:
						case ModEntry.ModType.MODULAR_MM:
						case ModEntry.ModType.MODULAR_RCRA:
						case ModEntry.ModType.MODULAR_I30:
						case ModEntry.ModType.MODULAR_I33:
						case ModEntry.ModType.MODULAR_MSM2:
							++modulars;
							break;

						case ModEntry.ModType.SCRIPT_MSM2:
							++scripts;
							break;

						case ModEntry.ModType.SUITS_MENU:
							++menu;
							break;
					}
				}

				var extra = $" {version} {GetTocArchivesCount(game, gamePath)} {modsToInstall.Count} {suits} {styles} {stages} {modulars} {scripts} {menu}";
				stackTrace = stackTrace.Substring(0, i + 1) + extra + stackTrace.Substring(i + 1);
			} catch {}
		}

		private static int GetTocArchivesCount(string gameId, string gamePath) {
			try {
				var game = GameBase.GetGame(gameId);
				var tocPath = game.GetTocPath(gamePath);

				if (gameId == GameMSMR.ID || gameId == GameMM.ID) {
					var toc = new TOC_I20();
					toc.Load(tocPath);
					return toc.ArchivesSection.Values.Count;
				} else {
					var toc = new TOC_I29();
					toc.Load(tocPath);
					return toc.ArchivesSection.Values.Count;
				}
			} catch {}

			return 0;
		}

		private static void UpdateTocSha(string tocPath) {
			var sha = Hashes.GetFileSha1(tocPath);
			var shaFilePath = tocPath + ".sha1";
			File.WriteAllText(shaFilePath, sha);
		}
	}
}
