// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Windows.Documents;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media;

namespace Overstrike {
	class DragAdorner: Adorner {
		private Rectangle child = null;
		private double offsetLeft = 0;
		private double offsetTop = 0;

		public DragAdorner(UIElement adornedElement, Size size, Brush brush): base(adornedElement) {
			child = new Rectangle {
				Fill = brush,
				Width = size.Width,
				Height = size.Height,
				IsHitTestVisible = false
			};
		}

		public override GeneralTransform GetDesiredTransform(GeneralTransform transform) {
			var result = new GeneralTransformGroup();
			result.Children.Add(base.GetDesiredTransform(transform));
			result.Children.Add(new TranslateTransform(offsetLeft, offsetTop));
			return result;
		}

		public double OffsetLeft {
			get { return offsetLeft; }
			set {
				offsetLeft = value;
				UpdateLocation();
			}
		}

		public void SetOffsets(double left, double top) {
			offsetLeft = left;
			offsetTop = top;
			UpdateLocation();
		}

		public double OffsetTop {
			get { return offsetTop; }
			set {
				offsetTop = value;
				UpdateLocation();
			}
		}

		protected override Size MeasureOverride(Size constraint) {
			child.Measure(constraint);
			return child.DesiredSize;
		}

		protected override Size ArrangeOverride(Size finalSize) {
			child.Arrange(new Rect(finalSize));
			return finalSize;
		}

		protected override Visual GetVisualChild(int index) {
			return child;
		}

		protected override int VisualChildrenCount {
			get { return 1; }
		}

		private void UpdateLocation() {
			AdornerLayer adornerLayer = Parent as AdornerLayer;
			if (adornerLayer != null)
				adornerLayer.Update(AdornedElement);
		}
	}
}
