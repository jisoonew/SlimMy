using Microsoft.Extensions.DependencyInjection;
using SlimMy.Model;
using SlimMy.Service;
using SlimMy.Test;
using SlimMy.View;
using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 로그인 뷰 표시
            var loginView = new View.Login();
            var loginViewModel = new MainPage(_dataService, loginView);
            loginView.DataContext = loginViewModel;

            loginView.ShowDialog();
        }
    }
}
