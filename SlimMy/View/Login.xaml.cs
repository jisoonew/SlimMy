using Microsoft.Extensions.DependencyInjection;
using SlimMy.Interface;
using SlimMy.Service;
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
    /// Login.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Login : Window, IView
    {

        public Login()
        {
            InitializeComponent();
        }

        public string Password { get; internal set; }

        // 회원가입
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 새로운 창 생성
            var newWindow = new SignUp(); // AnotherWindow는 새로 지정할 창의 클래스

            // 새 창을 주 창으로 설정
            Application.Current.MainWindow = newWindow;

            // 새 창을 열고, 이전 주 창을 닫습니다.
            newWindow.Show();
        }
    }
}
