// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Microsoft.WindowsAPICodePack.Dialogs;
using SuitTool.Data;
using System.Windows;

namespace SuitTool {
	public partial class App: Application {
		public RecentProjects RecentProjects = new();
		private Window _currentWindow = null;

		public List<RecentProject> SortedRecentProjects {
			get {
				var result = new List<RecentProject>();
				foreach (var project in RecentProjects.Projects) {
					result.Add(project);
				}

				result.Sort((x, y) => -x.ModificationDate.CompareTo(y.ModificationDate)); // TODO: should this also be sorted by pinned?

				return result;
			}
		}

		protected override void OnStartup(StartupEventArgs e) {
			// TODO: handle args

			LoadRecentProjects();

			_currentWindow = new RecentProjectsWindow();
			_currentWindow.Show();
		}

		private void LoadRecentProjects() {
			try {
				var s = new RecentProjects("RecentProjects.json");
				RecentProjects = s;
			} catch {}
		}

		private void SaveRecentProjects() {
			try {
				RecentProjects.Save("RecentProjects.json");
			} catch {}
		}

		public void UpdateRecentProjects(string projectFilename) {
			RecentProject recentProject = null;
			foreach (var project in RecentProjects.Projects) {
				if (project.ProjectPath == projectFilename) {
					recentProject = project;
					break;
				}
			}

			if (recentProject == null) {
				RecentProjects.Projects.Add(new RecentProject(projectFilename, 0));
				recentProject = RecentProjects.Projects.Last();
			}

			recentProject.ModificationDate = DateTime.Now;

			SaveRecentProjects();
		}

		public void ShowOpenProjectDialog() {
			CommonOpenFileDialog dialog = new CommonOpenFileDialog();
			dialog.Title = "Open existing project...";
			dialog.RestoreDirectory = true;

			dialog.Filters.Add(new CommonFileDialogFilter("Suit Project", "*.suit_project") { ShowExtensions = true });
			dialog.Filters.Add(new CommonFileDialogFilter("All files", "*") { ShowExtensions = true });

			if (dialog.ShowDialog() != CommonFileDialogResult.Ok) {
				return;
			}

			var projectFileName = dialog.FileName;
			LoadProject(projectFileName);
		}

		public void ShowNewProjectDialog() {
			var dialog = new CommonSaveFileDialog();
			dialog.Title = "Create project...";
			dialog.RestoreDirectory = true;
			dialog.Filters.Add(new CommonFileDialogFilter("Suit Project", "*.suit_project") { ShowExtensions = true });
			dialog.DefaultFileName = "*.suit_project";

			if (dialog.ShowDialog() != CommonFileDialogResult.Ok) {
				return;
			}

			var newProjectFileName = dialog.FileName;
			try {
				var project = new Project();
				project.Save(newProjectFileName);
			} catch {}

			LoadProject(newProjectFileName);
		}

		public void LoadProject(string filename) {
			MainWindow mainWindow = null;
			if (_currentWindow != null) {
				if (_currentWindow is MainWindow mw) {
					mainWindow = mw;
				}
			}

			if (mainWindow == null) {
				mainWindow = new MainWindow();
			}

			//

			if (_currentWindow != null) {
				if (_currentWindow is RecentProjectsWindow recentProjectsWindow) {
					recentProjectsWindow.Close();
					_currentWindow = null;
				}
			}

			_currentWindow = mainWindow;
			mainWindow.OpenProject(filename);
			mainWindow.Show();
		}
	}
}
