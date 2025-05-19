using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SlimMy.Helper
{
    public static class PasswordHelper
    {
        public static readonly DependencyProperty BoundPassword =
               DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordHelper),
                   new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject obj)
        {
            return (string)obj.GetValue(BoundPassword);
        }

        public static void SetBoundPassword(DependencyObject obj, string value)
        {
            if (obj is PasswordBox passwordBox)
            {

                if (passwordBox.Password != value)
                {
                    passwordBox.Password = value;
                }
            }

            obj.SetValue(BoundPassword, value);
        }

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                if (passwordBox.Password != (string)e.NewValue)
                {
                    passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                    passwordBox.Password = (string)e.NewValue ?? string.Empty;
                    passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
                }
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                string newPassword = passwordBox.Password;
                SetBoundPassword(passwordBox, newPassword);

                if (passwordBox.DataContext is MyPageViewModel viewModel)
                {
                    if (passwordBox.Name == "passwordBox")  // 비밀번호 입력
                        viewModel.Password = newPassword;
                    else if (passwordBox.Name == "passwordConfirmBox")  // 비밀번호 확인
                        viewModel.PasswordConfirm = newPassword;
                }
            }
        }
    }
}
