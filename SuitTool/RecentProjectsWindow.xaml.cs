// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using SuitTool.Data;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SuitTool {
	public partial class RecentProjectsWindow: Window {
		private readonly ObservableCollection<RecentProject> _projectsList = new();

		public RecentProjectsWindow() {
			InitializeComponent();
			MakeRecentProjects();
		}

		private void MakeRecentProjects() {
			_projectsList.Clear();

			var recentProjects = ((App)Application.Current).SortedRecentProjects;
			var recentProjectsSorted = new List<RecentProject>();
			foreach (var project in recentProjects) {
				recentProjectsSorted.Add(project);
			}

			// TODO: also sort by pinned here
			// recentProjectsSorted.Sort((x, y) => x.ModificationDate.CompareTo(y.ModificationDate));

			foreach (var project in recentProjectsSorted) {
				_projectsList.Add(project);
			}

			ProjectsList.ItemsSource = new CompositeCollection {
				new CollectionContainer() { Collection = _projectsList }
			};

			if (recentProjects.Count == 0) {
				HasProjectsLayout.Visibility = Visibility.Collapsed;
				NoProjectsLayout.Visibility = Visibility.Visible;
			}
		}

		#region event handlers

		private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			var view = CollectionViewSource.GetDefaultView(_projectsList);

			var filter = FilterTextBox.Text.Trim();
			if (filter == "") {
				view.Filter = null;
				return;
			}

			string[] words = filter.Split(' ');
			view.Filter = (item) => {
				var project = (RecentProject)item;
				foreach (var word in words) {
					if (!project.Name.Contains(word, StringComparison.OrdinalIgnoreCase) && !project.ProjectPath.Contains(word, StringComparison.OrdinalIgnoreCase)) {
						return false;
					}
				}
				return true;
			};
		}

		private void CreateProject_Click(object sender, RoutedEventArgs e) {
			((App)Application.Current).ShowNewProjectDialog();
		}

		private void OpenProject_Click(object sender, RoutedEventArgs e) {
			((App)Application.Current).ShowOpenProjectDialog();
		}

		private void ProjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (ProjectsList.SelectedItems.Count == 0) return;

			var project = (RecentProject)ProjectsList.SelectedItems[0];
			((App)Application.Current).LoadProject(project.ProjectPath);
		}

		#endregion
	}
}
