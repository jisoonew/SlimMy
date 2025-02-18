using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SlimMy.Service
{
    public class NavigationService : INavigationService
    {
        public NavigationService() { }

        public void NavigateToClose(string viewCloseName)
        {
            // 현재 실행 중인 창을 찾음
            Window currentWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.GetType().Name == viewCloseName);

            if (currentWindow != null)
            {
                currentWindow.Close();
            }
        }
    }
}
