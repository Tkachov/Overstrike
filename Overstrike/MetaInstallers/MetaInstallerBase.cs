// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Overstrike.Data;
using Overstrike.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace Overstrike.MetaInstallers {
	internal abstract class MetaInstallerBase {
		protected string _gamePath;
		protected AppSettings _settings;
		protected Profile _profile;

		public MetaInstallerBase(string gamePath, AppSettings settings, Profile profile) {
			_gamePath = gamePath;
			_settings = settings;
			_profile = profile;
		}

		public abstract void Prepare();
		public abstract void Start();
		public abstract void Install(ModEntry mod, int index);
		public abstract void Finish();

		public abstract void Uninstall(); // alternative to Start->(Install N times)->Finish

		protected static void RemoveReadOnlyAttribute(string path) {
			try {
				Utils.FileAccess.RemoveReadOnlyAttribute(path, true);
			} catch {
				ErrorLogger.WriteInfo($"Failed to remove read-only attribute from '{Path.GetFileName(path)}'!\n");
				throw;
			}
		}

		protected void SetupScripts() {
			var scriptsPath = Path.Combine(_gamePath, "scripts");
			var scriptsTxtPath = Path.Combine(_gamePath, "scripts.txt");
			var scriptsProxyPath = Path.Combine(_gamePath, "winmm.dll");
			var commandlineTxtPath = Path.Combine(_gamePath, "commandline.txt");

			// cleanup

			if (Directory.Exists(scriptsPath)) {
				ErrorLogger.WriteInfo("Deleting 'scripts' directory...");
				Directory.Delete(scriptsPath, true);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			if (File.Exists(scriptsTxtPath)) {
				ErrorLogger.WriteInfo("Deleting 'scripts.txt'...");
				File.Delete(scriptsTxtPath);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			if (File.Exists(scriptsProxyPath)) {
				ErrorLogger.WriteInfo("Deleting 'winmm.dll'...");
				File.Delete(scriptsProxyPath);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			RemoveScriptsFromCommandLine(commandlineTxtPath);

			//

			if (!_profile.Settings_Scripts_Enabled) return;

			if (!Directory.Exists(scriptsPath)) {
				ErrorLogger.WriteInfo("Creating 'scripts' directory...");
				Directory.CreateDirectory(scriptsPath);
				ErrorLogger.WriteInfo(" OK!\n");
			}

			ErrorLogger.WriteInfo("Creating 'scripts.txt'...");
			File.WriteAllText(scriptsTxtPath, "");
			ErrorLogger.WriteInfo(" OK!\n");

			ErrorLogger.WriteInfo("Creating 'winmm.dll'...");
			File.Copy("scripts_proxy.dll", scriptsProxyPath);
			ErrorLogger.WriteInfo(" OK!\n");

			if (_profile.Settings_Scripts_CommandLine) {
				AddScriptsToCommandLine(commandlineTxtPath);
			}
		}

		protected static void AddScriptsToCommandLine(string commandlineTxtPath) {
			try {
				if (File.Exists(commandlineTxtPath)) {
					RemoveReadOnlyAttribute(commandlineTxtPath);
					var text = File.ReadAllText(commandlineTxtPath);
					var tokens = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var token in tokens) {
						if (token.Equals("-scripts", StringComparison.OrdinalIgnoreCase)) {
							return;
						}
					}

					ErrorLogger.WriteInfo("Adding '-scripts' to 'commandline.txt'...");
					var separator = (text.Length > 0 && !text.EndsWith("\n") && !text.EndsWith("\r") && !text.EndsWith(" ")) ? " " : "";
					File.AppendAllText(commandlineTxtPath, $"{separator}-scripts");
					ErrorLogger.WriteInfo(" OK!\n");
				} else {
					ErrorLogger.WriteInfo("Creating 'commandline.txt'...");
					File.WriteAllText(commandlineTxtPath, "-scripts");
					ErrorLogger.WriteInfo(" OK!\n");
				}
			} catch (Exception ex) {
				ErrorLogger.WriteInfo($"Warning: failed to update 'commandline.txt': {ex.Message}\n");
			}
		}

		protected static void RemoveScriptsFromCommandLine(string commandlineTxtPath) {
			try {
				if (!File.Exists(commandlineTxtPath)) return;

				RemoveReadOnlyAttribute(commandlineTxtPath);
				var text = File.ReadAllText(commandlineTxtPath);
				var tokens = new List<string>(text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
				bool removed = false;
				for (int i = tokens.Count - 1; i >= 0; i--) {
					if (tokens[i].Equals("-scripts", StringComparison.OrdinalIgnoreCase)) {
						tokens.RemoveAt(i);
						removed = true;
					}
				}

				if (!removed) return;

				if (tokens.Count == 0) {
					ErrorLogger.WriteInfo("Deleting 'commandline.txt'...");
					File.Delete(commandlineTxtPath);
					ErrorLogger.WriteInfo(" OK!\n");
				} else {
					ErrorLogger.WriteInfo("Removing '-scripts' from 'commandline.txt'...");
					File.WriteAllText(commandlineTxtPath, string.Join(" ", tokens));
					ErrorLogger.WriteInfo(" OK!\n");
				}
			} catch (Exception ex) {
				ErrorLogger.WriteInfo($"Warning: failed to clean up 'commandline.txt': {ex.Message}\n");
			}
		}
	}
}
