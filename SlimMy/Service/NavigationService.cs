using SlimMy.View;
using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SlimMy.Service
{
    public class NavigationService : INavigationService
    {
        private readonly IDataService _dataService = new DataService();

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

        // 닉네임 변경 화면 전환
        public async Task NavigateToNickName()
        {
            var myChatsViewModel = await ViewModel.NicknameChangeViewModel.CreateAsync();
            var pageInstance = new View.NicknameChange { DataContext = myChatsViewModel };
            pageInstance.Show();
        }

        // 로그인 화면
        public async Task NavigateToCloseAndLoginAsync(string viewCloseName)
        {
            // 1. 로그인 뷰 먼저 생성
            var loginView = new View.Login();
            var loginViewModel = new MainPageViewModel(_dataService, loginView);
            loginView.DataContext = loginViewModel;

            // 2. MainWindow 교체 먼저 (Shutdown 방지)
            Application.Current.MainWindow = loginView;

            // 3. 로그인 창 표시
            loginView.Show();

            // 4. 약간의 딜레이를 주어 WPF 내부 루프가 안정화되도록 함
            await Task.Delay(200);  // 중요! 렌더링 시점 보장

            // 5. 기존 창 닫기 (MainHome 등)
            Window currentWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.GetType().Name == viewCloseName);

            currentWindow?.Close();
        }

        // 닉네임 변경 화면 닫기
        public async Task NavigateToNickNameClose()
        {
            // 닉네임 변경 창 닫기
            foreach (Window window in Application.Current.Windows)
            {
                if (window is View.NicknameChange)
                {
                    window.Close();
                    break;
                }
            }

            // 메인 홈 포커싱 + 새 데이터 로딩
            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainHome main)
                {
                    var newVm = await MyPageViewModel.CreateAsync();

                    // 새 MyPage에 ViewModel을 직접 주입
                    var newPage = new MyPage();
                    newPage.DataContext = newVm;

                    // Frame에 직접 페이지 삽입
                    main.MainFrame.Navigate(newPage);

                    break;
                }
            }
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
