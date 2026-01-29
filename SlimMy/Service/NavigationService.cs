using SlimMy.Model;
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

        // 운동 추가
        public async Task NavigateToAddExerciseViewAsync(PlannerViewModel plannerVm)
        {
            var exerciseViewModel = await ExerciseViewModel.CreateAsync(plannerVm);
            var pageInstance = new View.AddExercise { DataContext = exerciseViewModel };
            pageInstance.Show();
        }

        // 운동 설정
        public async Task NavigateToDietGoalViewAsync()
        {
            var dietGoalViewModel = await DietGoalViewModel.CreateAsync();
            var pageInstance = new View.DietGoal { DataContext = dietGoalViewModel };
            pageInstance.Show();
        }

        private View.ReportDialog? _reportDialog;
        private ReportDialogViewModel? _reportDialogVm;

        // 신고
        public async Task NavigateToReportDialogViewAsync(ReportTarget target, Action onClosed)
        {
            if (_reportDialog != null)
            {
                if (_reportDialog.WindowState == WindowState.Minimized)
                    _reportDialog.WindowState = WindowState.Normal;

                _reportDialog.Activate();
                return;
            }

            _reportDialogVm = await ReportDialogViewModel.CreateAsync(target);

            _reportDialog = new View.ReportDialog
            {
                DataContext = _reportDialogVm
            };

            _reportDialog.Closed += (s, e) =>
            {
                _reportDialog = null;
                _reportDialogVm = null;
                onClosed?.Invoke();
            };

            _reportDialog.Show();
        }

        // 현재 신고창 VM에 메시지 추가
        public void AddReportMessage(ChatMessage msg)
        {
            if (_reportDialogVm == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _reportDialogVm.ReportMessage(msg);
            });
        }

        // 현재 신고창 VM에 메시지 삭제
        public void RemoveReportMessage(ChatMessage msg)
        {
            if (_reportDialogVm == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _reportDialogVm.SelectedMessages.Remove(msg);
            });
        }

        // 신고 화면 닫기
        public void NavigateToReportClose()
        {
            if (_reportDialog == null) return;

            if (_reportDialog.Dispatcher.CheckAccess())
            {
                _reportDialog.Close();
            }
            else
            {
                _reportDialog.Dispatcher.Invoke(() => _reportDialog.Close());
            }
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
            // 로그인 뷰 먼저 생성
            var loginView = new View.Login();
            var loginViewModel = new MainPageViewModel(_dataService, loginView);
            loginView.DataContext = loginViewModel;

            // MainWindow 교체 먼저 (Shutdown 방지)
            Application.Current.MainWindow = loginView;

            // 로그인 창 표시
            loginView.Show();

            // 약간의 딜레이를 주어 WPF 내부 루프가 안정화되도록 함
            await Task.Delay(200);  // 중요! 렌더링 시점 보장

            // 기존 창 닫기 (MainHome 등)
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

        public async Task NavigateToReportFrameAsync(Type pageType)
        {
            if (_frame == null)
            {
                MessageBox.Show("Navigation Frame이 설정되지 않았습니다.");
                return;
            }

            object pageInstance = null;

            if (pageType == typeof(View.Report))
            {
                var reportViewModel = await ViewModel.ReportViewModel.CreateAsync();
                pageInstance = new View.Report { DataContext = reportViewModel };
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

        public async Task NavigateToPlannerFrameAsync(Type pageType)
        {
            if (_frame == null)
            {
                MessageBox.Show("Navigation Frame이 설정되지 않았습니다.");
                return;
            }

            object pageInstance = null;

            if (pageType == typeof(View.Planner))
            {
                var myChatsViewModel = await ViewModel.PlannerViewModel.CreateAsync();
                pageInstance = new View.Planner { DataContext = myChatsViewModel };
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
            // 운도 선택창 창 닫기
            foreach (Window window in Application.Current.Windows)
            {
                if (window is AddExercise)
                {
                    window.Close();
                    break;
                }
            }
        }

        // 모든 창을 닫고 로그인 창만 생성
        public void NavigateToLoginOnly()
        {
            var app = Application.Current;
            if (app == null)
                throw new InvalidOperationException("Application.Current 가 null 입니다.");

            app.Dispatcher.Invoke(() =>
            {
                // 로그인 창 찾기 or 없으면 새로 생성
                var loginWindow = app.Windows
                    .OfType<View.Login>()
                    .FirstOrDefault();

                if (loginWindow == null)
                {
                    loginWindow = new View.Login();
                    loginWindow.Show();
                }

                // 현재 열린 모든 창을 리스트로 복사
                var windows = app.Windows.Cast<Window>().ToList();

                // 로그인창을 제외한 나머지 전부 닫기
                foreach (var window in windows)
                {
                    if (!ReferenceEquals(window, loginWindow))
                        window.Close();
                }

                // 메인 윈도우를 로그인창으로 교체
                app.MainWindow = loginWindow;
            });
        }

        public void SetFrame(Frame frame)
        {
            _frame = frame;
        }
    }
}
