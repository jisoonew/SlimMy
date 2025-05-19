using SlimMy.View;
using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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

        public void NavigateToAddExercise()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var exerciseWindow = new AddExercise();
                exerciseWindow.Show();
            });
        }

        public void NavigateToNickName()
        {
            MessageBox.Show("옴?");

            Application.Current.Dispatcher.Invoke(() =>
            {
                var exerciseWindow = new NicknameChange();
                exerciseWindow.Show();
            });
        }

        public async Task NavigateToMyPageFrameAsync(Type pageType)
        {
            if (_frame == null)
            {
                MessageBox.Show("Navigation Frame이 설정되지 않았습니다.");
                return;
            }

            object pageInstance = null;

            if (pageType == typeof(View.MyPage))
            {
                var myChatsViewModel = await ViewModel.MyPageViewModel.CreateAsync();
                pageInstance = new View.MyPage { DataContext = myChatsViewModel };
            }
            else if (pageType.IsSubclassOf(typeof(Page)))
            {
                pageInstance = Activator.CreateInstance(pageType);
            }

            if (pageInstance is Page page)
            {
                _frame.Navigate(page);
            }
            else
            {
                MessageBox.Show("올바른 Page 타입이 아닙니다.");
            }
        }

        private Frame _frame;

        public NavigationService(Frame frame)
        {
            _frame = frame;
        }

        public void NavigateToFrame(Type pageType)
        {
            if (_frame != null && pageType.IsSubclassOf(typeof(Page))) // Page 타입인지 확인
            {
                Page pageInstance = (Page)Activator.CreateInstance(pageType); // Type을 객체로 변환
                _frame.Navigate(pageInstance);
            }
            else
            {
                MessageBox.Show("올바른 Page 타입이 아님");
            }
        }

        public async Task NavigateToFrameAsync(Type pageType)
        {
            if (_frame == null)
            {
                MessageBox.Show("Navigation Frame이 설정되지 않았습니다.");
                return;
            }

            object pageInstance = null;

            if (pageType == typeof(View.MyChats))
            {
                var myChatsViewModel = await ViewModel.MyChatsViewModel.CreateAsync();
                pageInstance = new View.MyChats { DataContext = myChatsViewModel };
            }
            else if (pageType.IsSubclassOf(typeof(Page)))
            {
                pageInstance = Activator.CreateInstance(pageType);
            }

            if (pageInstance is Page page)
            {
                _frame.Navigate(page);
            }
            else
            {
                MessageBox.Show("올바른 Page 타입이 아닙니다.");
            }
        }

        public async Task NavigateToCommunityFrameAsync(Type pageType)
        {
            if (_frame == null)
            {
                MessageBox.Show("Navigation Frame이 설정되지 않았습니다.");
                return;
            }

            object pageInstance = null;

            if (pageType == typeof(View.Community))
            {
                var myChatsViewModel = await ViewModel.CommunityViewModel.CreateAsync();
                pageInstance = new View.Community { DataContext = myChatsViewModel };
            }
            else if (pageType.IsSubclassOf(typeof(Page)))
            {
                pageInstance = Activator.CreateInstance(pageType);
            }

            if (pageInstance is Page page)
            {
                _frame.Navigate(page);
            }
            else
            {
                MessageBox.Show("올바른 Page 타입이 아닙니다.");
            }
        }

        public async Task NavigateToDashBoardFrameAsync(Type pageType)
        {
            if (_frame == null)
            {
                MessageBox.Show("Navigation Frame이 설정되지 않았습니다.");
                return;
            }

            object pageInstance = null;

            if (pageType == typeof(View.DashBoard))
            {
                var myChatsViewModel = await ViewModel.DashBoardViewModel.CreateAsync();
                pageInstance = new View.DashBoard { DataContext = myChatsViewModel };
            }
            else if (pageType.IsSubclassOf(typeof(Page)))
            {
                pageInstance = Activator.CreateInstance(pageType);
            }

            if (pageInstance is Page page)
            {
                _frame.Navigate(page);
            }
            else
            {
                MessageBox.Show("올바른 Page 타입이 아닙니다.");
            }
        }

        public async Task NavigateToExerciseHistoryFrameAsync(Type pageType)
        {
            if (_frame == null)
            {
                MessageBox.Show("Navigation Frame이 설정되지 않았습니다.");
                return;
            }

            object pageInstance = null;

            if (pageType == typeof(View.ExerciseHistory))
            {
                var myChatsViewModel = await ViewModel.ExerciseHistoryViewModel.CreateAsync();
                pageInstance = new View.ExerciseHistory { DataContext = myChatsViewModel };
            }
            else if (pageType.IsSubclassOf(typeof(Page)))
            {
                pageInstance = Activator.CreateInstance(pageType);
            }

            if (pageInstance is Page page)
            {
                _frame.Navigate(page);
            }
            else
            {
                MessageBox.Show("올바른 Page 타입이 아닙니다.");
            }
        }

        public async Task NavigateToWeightHistoryFrameAsync(Type pageType)
        {
            if (_frame == null)
            {
                MessageBox.Show("Navigation Frame이 설정되지 않았습니다.");
                return;
            }

            object pageInstance = null;

            if (pageType == typeof(View.WeightHistory))
            {
                var myChatsViewModel = await ViewModel.WeightHistoryViewModel.CreateAsync();
                pageInstance = new View.WeightHistory { DataContext = myChatsViewModel };
            }
            else if (pageType.IsSubclassOf(typeof(Page)))
            {
                pageInstance = Activator.CreateInstance(pageType);
            }

            if (pageInstance is Page page)
            {
                _frame.Navigate(page);
            }
            else
            {
                MessageBox.Show("올바른 Page 타입이 아닙니다.");
            }
        }

        // 로그인 -> 메인 화면
        public void NavigateToMainWindow(MainPageViewModel mainPageViewModel)
        {
            MessageBox.Show("메인 윈도우 진입");

            var mainWindow = new MainHome(mainPageViewModel);
            mainWindow.Show();

            // 로그인 창 닫기
            foreach (Window window in Application.Current.Windows)
            {
                if (window is View.Login)
                {
                    window.Close();
                    break;
                }
            }
        }

        // 운동 선택창 닫기
        public void NavigateToExerciseWindow()
        {
            // 로그인 창 닫기
            foreach (Window window in Application.Current.Windows)
            {
                if (window is AddExercise)
                {
                    window.Close();
                    break;
                }
            }
        }

        public void SetFrame(Frame frame)
        {
            _frame = frame;
        }
    }
}
