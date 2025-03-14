// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using System.IO;

namespace SuitTool.Data {
	public class RecentProjects {
		public List<RecentProject> Projects;

		public RecentProjects() {
			Projects = new();
		}

		public RecentProjects(string filename): this() {
			JObject json = JObject.Parse(File.ReadAllText(filename));

			var projects = (JArray)json["recent_projects"];
			if (projects == null) { return; }

			foreach (var project in projects) {
				try {
					Projects.Add(new RecentProject((string)project["path"], (long)project["modification_date"])); // TODO: pinned status
				} catch {}
			}
		}

		public bool Save(string filename) {
			try {
				var j = new JObject();

				var projects = new JArray();
				foreach (var project in Projects) {
					projects.Add(new JObject {
						["path"] = project.ProjectPath,
						["modification_date"] = ((DateTimeOffset)project.ModificationDate).ToUnixTimeSeconds()
					});
				}
				j["recent_projects"] = projects;

				File.WriteAllText(filename, j.ToString());
				return true;
			} catch {}

			return false;
		}
	}

	public class RecentProject {
		// stored
		public string ProjectPath { get; set; }
		public DateTime ModificationDate { get; set; }
		// TODO: pinned

		// run-time
		public string Name { get; set; }
		public string? DisplayName {
			get {
				var result = Name;
				
				if (result == null || result == "") {
					result = Path.GetFileName(ProjectPath);
				}

				return result;
			}
		}
		public string? DirectoryPath => Path.GetDirectoryName(ProjectPath);

		public RecentProject(string path, long unixModificationTime) {
			ProjectPath = path;
			ModificationDate = DateTimeOffset.FromUnixTimeSeconds(unixModificationTime).Date;
			
			var project = new Project(path);
			Name = project.ModName;
		}
	}
}
