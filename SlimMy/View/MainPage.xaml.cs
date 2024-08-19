using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        protected override void OnClosing(CancelEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("채팅프로그램을 종료하시겠습니까?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }

            Environment.Exit(1);
        }
    }
}
