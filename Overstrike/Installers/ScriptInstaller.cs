// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO.Compression;
using System.IO;
using Overstrike.Data;
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Overstrike.Utils;

namespace Overstrike.Installers {
	internal class ScriptSupportInstaller: InstallerBase {
		public ScriptSupportInstaller(string gamePath): base(gamePath) {}

		class ScriptDefinition {
			public string Name;
			public string Version;
			public string Type;
			public List<string> Dependencies;
			public List<string> ResolvedDependencies;
			public string Dll;
		}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			var scriptSupportMod = (ScriptSupportModEntry)mod;
			var modsToInstall = scriptSupportMod.Mods;
			var scriptDefinitions = new List<ScriptDefinition>();
			foreach (var modToInstall in modsToInstall) {
				if (!ModEntry.IsTypeFamilyScript(modToInstall.Type)) continue;

				var def = GetScriptDefinition(modToInstall);
				if (def != null) {
					scriptDefinitions.Add(def);
				}
			}

			if (!AllDependenciesMet(scriptDefinitions)) {
				return;
			}

			TopologicalSort(ref scriptDefinitions);

			var scriptsTxtPath = Path.Combine(_gamePath, "scripts.txt");
			var order = "";
			foreach (var def in scriptDefinitions) {
				if (order != "") order += "\r\n";
				order += $"{def.Dll}";
			}
			File.WriteAllText(scriptsTxtPath, order);
		}

		private static ScriptDefinition GetScriptDefinition(ModEntry mod) {
			try {
				using var zip = NestedFiles.GetNestedZip(mod.Path);

				var dll = ""; // TODO: better way to determine main .dll?
				foreach (var entry in zip.Entries) {
					if (entry.FullName.StartsWith("resources", StringComparison.OrdinalIgnoreCase)) continue;
					if (entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) {
						dll = entry.Name;
						break;
					}
				}

				foreach (var entry in zip.Entries) {
					if (!entry.FullName.Equals("info.json", StringComparison.OrdinalIgnoreCase)) continue;

					using var stream = entry.Open();
					using var reader = new StreamReader(stream);
					var str = reader.ReadToEnd();
					var info = JObject.Parse(str);

					var scriptDefinition = new ScriptDefinition() {
						Name = (string)info["name"],
						Version = (string)info["version"],
						Type = (string)info["type"],
						Dependencies = new(),
						ResolvedDependencies = new(),
						Dll = dll
					};
					foreach (var dep in info["dependencies"]) {
						scriptDefinition.Dependencies.Add((string)dep);
					}
					return scriptDefinition;
				}
			} catch (Exception) {}

			return null;
		}

		private static bool AllDependenciesMet(List<ScriptDefinition> scriptDefinitions) {
			var allDependenciesMet = true;

			var availableVersions = new Dictionary<string, List<string>>(); // TODO: do we allow multiple versions of the same mod installed at once?
			foreach (var def in scriptDefinitions) {
				if (availableVersions.TryGetValue(def.Name, out List<string>? value)) {
					value.Add(def.Version);
				} else {
					availableVersions[def.Name] = new List<string>() { def.Version };
				}
			}

			ErrorLogger.WriteInfo("\n\tChecking scripts dependencies...\n\n");
			foreach (var def in scriptDefinitions) {
				ErrorLogger.WriteInfo($"\t- '{def.Name}:{def.Version}':\n");
				if (def.Dependencies.Count == 0) {
					ErrorLogger.WriteInfo($"\t\tno dependencies\n");
				}

				def.ResolvedDependencies.Clear();
				foreach (var dep in def.Dependencies) {				
					var name = dep;
					var versionRule = "*";
					var i = dep.LastIndexOf(':');
					if (i != -1) {
						name = dep[..i];
						versionRule = dep[(i + 1)..];
					}

					if (availableVersions.TryGetValue(name, out List<string>? versions)) {
						var foundMatchingVersion = false;
						foreach (var availableVersion in versions) {
							if (MatchesVersionRule(availableVersion, versionRule)) {
								ErrorLogger.WriteInfo($"\t\t'{dep}' -- found matching version '{availableVersion}'\n");
								foundMatchingVersion = true;
								def.ResolvedDependencies.Add($"{name}:{availableVersion}");
								break;
							}
						}

						if (!foundMatchingVersion) {
							var list = "[";
							foreach (var availableVersion in versions) {
								if (list.Length > 1) list += ", ";
								list += $"'{availableVersion}'";
							}
							list += "]";

							ErrorLogger.WriteError($"\t\t'{dep}' -- no matches found in {list}!\n");
							allDependenciesMet = false;
						}
					} else {
						ErrorLogger.WriteError($"\t\t'{dep}' -- missing!\n");
						allDependenciesMet = false;
					}
				}

				ErrorLogger.WriteInfo($"\n");
			}

			if (allDependenciesMet) {
				ErrorLogger.WriteInfo("\tDone.\n\n");
			} else {
				throw new Exception("Not all script dependencies met.");
			}

			return allDependenciesMet;
		}

		private static bool MatchesVersionRule(string version, string rule) {
			if (rule == "*") return true;
			return (version == rule); // TODO: more complex rules
		}

		private static void TopologicalSort(ref List<ScriptDefinition> scriptDefinitions) {
			var circularDependencyDetected = false;
			var visited = new Dictionary<string, int>();
			var definitionByName = new Dictionary<string, ScriptDefinition>();
			foreach (var def in scriptDefinitions) {
				var qName = $"{def.Name}:{def.Version}";
				visited[qName] = 0;
				definitionByName[qName] = def;
			}

			var order = new List<string>();

			void DFS(string name) {
				visited[name] = 1;

				var def = definitionByName[name];
				foreach (var dep in def.ResolvedDependencies) {
					if (visited[dep] == 0) {
						DFS(dep);
					} else if (visited[dep] == 1) {
						circularDependencyDetected = true;
					}
				}

				order.Add(name);
				visited[name] = 2;
			}
			
			foreach (var def in scriptDefinitions) {
				var qName = $"{def.Name}:{def.Version}";
				if (visited[qName] == 0) {
					DFS(qName);
				}
			}

			if (circularDependencyDetected) {
				ErrorLogger.WriteInfo($"\tWarning: circular dependency detected! Script installation order might not be correct.\n");
			}

			scriptDefinitions.Sort((x, y) => {
				return order.IndexOf(x.Name).CompareTo(order.IndexOf(y.Name));
			});

			// kinda stable sort by lib/script type here:

			var libs = new List<ScriptDefinition>();
			var scripts = new List<ScriptDefinition>();
			foreach (var def in scriptDefinitions) {
				if (def.Type == "lib") libs.Add(def);
				else scripts.Add(def);
			}

			scriptDefinitions = new();
			foreach (var def in libs) {
				scriptDefinitions.Add(def);
			}
			foreach (var def in scripts) {
				scriptDefinitions.Add(def);
			}

			ErrorLogger.WriteInfo($"\tScript installation order:\n");
			foreach (var def in scriptDefinitions) {
				ErrorLogger.WriteInfo($"\t- '{def.Name}:{def.Version}' ({def.Type})\n");
			}
		}
	}

	internal class ScriptInstaller: InstallerBase {
		public ScriptInstaller(string gamePath): base(gamePath) {}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			var scriptsPath = Path.Combine(_gamePath, "scripts");

			using var zip = ReadModFile();
			foreach (ZipArchiveEntry entry in zip.Entries) {
				if (entry.Name == "" && entry.FullName.EndsWith("/")) continue; // directory

				if (entry.FullName.Equals("info.json", StringComparison.OrdinalIgnoreCase)) continue;

				var path = Path.Combine(scriptsPath, entry.FullName);
				try { Directory.CreateDirectory(Path.GetDirectoryName(path)); } catch {}

				using var f = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
				using var data = entry.Open();		
				data.CopyTo(f);
			}
		}
	}
}
