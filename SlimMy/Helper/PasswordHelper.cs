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
        // VM PasswordBox 바인딩용
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordHelper),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnBoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject obj)
            => (string)obj.GetValue(BoundPasswordProperty);

        public static void SetBoundPassword(DependencyObject obj, string value)
            => obj.SetValue(BoundPasswordProperty, value);

        // 이벤트 중복 등록 방지용
        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(PasswordHelper),
                new PropertyMetadata(false));

        private static bool GetIsUpdating(DependencyObject obj)
            => (bool)obj.GetValue(IsUpdatingProperty);

        private static void SetIsUpdating(DependencyObject obj, bool value)
            => obj.SetValue(IsUpdatingProperty, value);

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox passwordBox)
                return;

            // PasswordChanged 이벤트는 한 번만 붙이기
            passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
            passwordBox.PasswordChanged += PasswordBox_PasswordChanged;

            // VM -> UI 반영 (null이면 빈 문자열)
            var newValue = e.NewValue as string ?? string.Empty;

            // 업데이트 중이면 루프 방지
            if (GetIsUpdating(passwordBox))
                return;

            // UI 값이 다를 때만 반영
            if (passwordBox.Password != newValue)
            {
                SetIsUpdating(passwordBox, true);
                passwordBox.Password = newValue;
                SetIsUpdating(passwordBox, false);
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox passwordBox)
                return;

            // UI -> VM 반영 중 루프 방지
            if (GetIsUpdating(passwordBox))
                return;

            SetIsUpdating(passwordBox, true);
            SetBoundPassword(passwordBox, passwordBox.Password);
            SetIsUpdating(passwordBox, false);
        }
    }
}
