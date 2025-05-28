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

            viewModel.SetNavigationService(_navigationService);

            this.DataContext = viewModel;

            MainHomeLoaded.TrySetResult(true);
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("SlimMy를 종료하시겠습니까?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }

            Application.Current.Shutdown();
        }

        public static TaskCompletionSource<bool> MainHomeLoaded = new TaskCompletionSource<bool>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainHomeLoaded.TrySetResult(true); // 로딩 완료 신호
        }
    }
}
