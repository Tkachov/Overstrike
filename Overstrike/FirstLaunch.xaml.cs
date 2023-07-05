using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Overstrike
{
    /// <summary>
    /// Interaction logic for FirstLaunch.xaml
    /// </summary>
    public partial class FirstLaunch : Window
    {
        public FirstLaunch()
        {
            InitializeComponent();
        }

		private void Button_Click(object sender, RoutedEventArgs e) {
			var window = new CreateProfile();
			window.ShowDialog();

            var p = window.GetProfile();
            if (p != null) {                
                Close();
            }
		}
    }
}
