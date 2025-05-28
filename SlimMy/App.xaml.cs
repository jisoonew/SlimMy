using Microsoft.Extensions.DependencyInjection;
using SlimMy.Model;
using SlimMy.Service;
using SlimMy.View;
using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SlimMy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IDataService _dataService = new DataService();

        public App()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 로그인 뷰 모델과 뷰 생성
            var loginView = new View.Login();
            var loginViewModel = new MainPageViewModel(_dataService, loginView);
            loginView.DataContext = loginViewModel;

            // 로그인 창을 MainWindow로 지정하려면 이쪽 사용
            Application.Current.MainWindow = loginView;
            loginView.Show();
        }
    }
}
