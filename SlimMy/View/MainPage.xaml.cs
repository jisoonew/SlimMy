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

namespace SlimMy.View
{
    /// <summary>
    /// MainPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainPage : Window
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void DashBoard_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Source = new Uri("DashBoard.xaml", UriKind.Relative);
        }

        private void Planner_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Source = new Uri("Planner.xaml", UriKind.Relative);
        }

        private void Community_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Source = new Uri("Community.xaml", UriKind.Relative);
        }
    }
}
