using SlimMy.Service;
using SlimMy.ViewModel;
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
    /// MainHome.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainHome : Window, IView
    {
        private NavigationService _navigationService;

        public MainHome(MainPageViewModel viewModel)
        {
            InitializeComponent();

            _navigationService = new NavigationService(MainFrame);
            _navigationService.SetFrame(MainFrame);

            // ViewModel에게 NavigationService를 주입하거나 초기화할 수 있다면 여기서 하면 됨
            viewModel.SetNavigationService(_navigationService);

            this.DataContext = viewModel;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
