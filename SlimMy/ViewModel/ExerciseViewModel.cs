using SlimMy.Model;
using SlimMy.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    // 시간 기반으로 칼로리를 계산할 것인지
    // 횟수 기반으로 칼로리를 계산할 것인지 선택
    public enum CalorieMode
    {
        TimeBased,
        RepetitionBased
    }

    public class ExerciseViewModel : BaseViewModel
    {
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";
        private readonly INavigationService _navigationService;

        private int _currentPage; // 현재 페이지 번호.
        private int _totalPages; // 전체 데이터에서 생성된 총 페이지 수.
        private int _pageSize = 20; // 페이지당 항목 수

        public int minutes; // 운동 시간

        // 전체 데이터에서 총 몇 개의 페이지가 있는지 계산하여 저장.
        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(nameof(TotalPages)); }
        }

        private ObservableCollection<Exercise> _exercises;
        public ObservableCollection<Exercise> Exercises
        {
            get { return _exercises;}
            set { _exercises = value; OnPropertyChanged(nameof(Exercises)); }
        }

        private ObservableCollection<Exercise> _currentPageData;
        public ObservableCollection<Exercise> CurrentPageData
        {
            get => _currentPageData;
            set
            {
                _currentPageData = value;
                OnPropertyChanged(nameof(CurrentPageData));
            }
        }

        // 현재 페이지 번호를 관리
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    UpdateCurrentPageData();
                    OnPropertyChanged(nameof(CurrentPage));
                }
            }
        }

        // 운동 소모 시간
        private string _plannedMinutes;
        public string PlannedMinutes
        {
            get { return _plannedMinutes; }
            set
            {
                _plannedMinutes = value; OnPropertyChanged(nameof(PlannedMinutes));
            }
        }

        // 계산 이후 칼로리
        private string _calories;
        public string Calories
        {
            get { return _calories; }
            set { _calories = value; OnPropertyChanged(nameof(Calories)); }
        }

        private int _selectedEXerciseIndex;
        public int SelectedEXerciseIndex
        {
            get => _selectedEXerciseIndex;
            set
            {
                _selectedEXerciseIndex = value;
                OnPropertyChanged(nameof(SelectedEXerciseIndex)); // UI에 변경 알림
            }
        }

        private Exercise _selectedChatRoomData;
        public Exercise SelectedChatRoomData
        {
            get { return _selectedChatRoomData; }
            set
            {
                if (_selectedChatRoomData != value)
                {
                    _selectedChatRoomData = value;
                    OnPropertyChanged(nameof(SelectedChatRoomData));
                }
                else
                {
                    // 동일한 항목을 선택해도 동작하도록 처리
                    OnPropertyChanged(nameof(SelectedChatRoomData));
                }
            }
        }

        // 시간 or 횟수 기반 칼로리 계산
        private CalorieMode _selectedCalorieMode;
        public CalorieMode SelectedCalorieMode
        {
            get => _selectedCalorieMode;
            set
            {
                _selectedCalorieMode = value;
                OnPropertyChanged(nameof(SelectedCalorieMode));
                OnPropertyChanged(nameof(IsTimeBased));
                OnPropertyChanged(nameof(IsRepetitionBased));
                // UpdateCalories(); // 선택 모드 바뀌면 즉시 칼로리 다시 계산
            }
        }

        private string _exerciseName;
        public string ExerciseName
        {
            get { return _exerciseName; }
            set { _exerciseName = value; OnPropertyChanged(nameof(ExerciseName)); }
        }

        public bool IsTimeBased => SelectedCalorieMode == CalorieMode.TimeBased;
        public bool IsRepetitionBased => SelectedCalorieMode == CalorieMode.RepetitionBased;

        public ObservableCollection<Exercise> AllData { get; set; } // 전체 데이터

        public static bool IsEditMode { get; set; }

        // 페이징 다음 버튼
        public ICommand NextPageCommand { get; set; }

        // 페이징 이전 버튼
        public ICommand PreviousPageCommand { get; set; }

        // 운동 선택
        public ICommand SelectedExerciseCommand { get; set; }

        // 플래너 운동 추가
        public ICommand AddExerciseCommand { get; set; }

        public ICommand CalculateCaloriesCommand { get; set; }

        public ExerciseViewModel()
        {
            _repo = new Repo(_connstring);
            Exercises = new ObservableCollection<Exercise>();

            // 운동 목록 데이터 가져오기
            ExerciseList();

            // 결합도를 낮추기 위한 뷰 전환 서비스
            _navigationService = new NavigationService();

            AllData = Exercises;

            // 총 페이지 수 계산
            TotalPages = (int)Math.Ceiling((double)AllData.Count / _pageSize);

            // 현재 페이지 초기화
            CurrentPage = 1;

            // 명령 초기화
            NextPageCommand = new RelayCommand(
            execute: _ => NextPage(),
            canExecute: _ => CanGoToNextPage());

            PreviousPageCommand = new RelayCommand(
                execute: _ => PreviousPage(),
                canExecute: _ => CanGoToPreviousPage());

            UpdateCurrentPageData();

            // 운동 선택
            SelectedExerciseCommand = new RelayCommand(SelectedExercise);

            // 칼로리 계산 시간/횟수 선택
            SelectedCalorieMode = CalorieMode.TimeBased; // 기본값

            // 플래너 운동 추가
            AddExerciseCommand = new RelayCommand(AddExerciseData);

            // 칼로리 계산
            CalculateCaloriesCommand = new RelayCommand(CalculateCalories);
        }

        // 운동 목록 출력
        public void ExerciseList()
        {
            var exercisesData = _repo.AllExerciseList();

            Exercises.Clear();

            foreach (var exerciseBundle in exercisesData)
            {
                Exercises.Add(new Exercise
                {
                    ExerciseID = exerciseBundle.ExerciseID,
                    ExerciseName = exerciseBundle.ExerciseName,
                    ImagePath = exerciseBundle.ImagePath,
                    Met = exerciseBundle.Met
                });
            }
        }

        // 운동 선택
        public void SelectedExercise(object parameter)
        {
            if (parameter is Exercise exerciseData)
            {
                SelectedChatRoomData = exerciseData;
                string msg = string.Format("{0}를 선택하시겠습니까?", SelectedChatRoomData.ExerciseName);
                MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (messageBoxResult == MessageBoxResult.No)
                {
                    return;
                }
                else
                {
                    // MessageBox.Show("AllData : " + exerciseData.ExerciseID + "이름 : " + exerciseData.ExerciseName);
                    // 선택한 운동 이름 및 MET 값 설정
                    ExerciseName = exerciseData.ExerciseName;
                }
            }
        }

        // 플래너 운동 추가하기
        public void AddExerciseData(object parameter)
        {
            string msg = string.Format("{0}를 선택하시겠습니까?", SelectedChatRoomData.ExerciseName);
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                // MessageBox.Show("AllData : " + exerciseData.ExerciseID + "이름 : " + exerciseData.ExerciseName);
                // 운동 선택 윈도우 창 닫기
                // _navigationService.NavigateToClose("AddExercise");

                CalculateCalories(null);

                if (IsEditMode)
                {
                    // 선택된 운동 아이디 플래너에게 전달
                    PlannerViewModel.Instance.UpdatePlannerPrint(SelectedChatRoomData, Calories, minutes);

                    // 운동 선택창 닫기
                    _navigationService.NavigateToExerciseWindow();
                }
                else
                {
                    // 선택된 운동 아이디 플래너에게 전달
                    PlannerViewModel.Instance.SelectedPlannerPrint(SelectedChatRoomData, Calories, minutes);

                    // 운동 선택창 닫기
                    _navigationService.NavigateToExerciseWindow();
                }
            }
        }

        // 칼로리 계산
        private void CalculateCalories(object parameter)
        {
            User currentUser = UserSession.Instance.CurrentUser;

            double userWeight = _repo.SelectUserWeight(currentUser.UserId);
            double met = (double)SelectedChatRoomData.Met;
            minutes = Convert.ToInt32(PlannedMinutes);
            int result = (int)(met * userWeight * 3.5 / 200) * minutes;

            Calories = result.ToString();
        }

        // 페이지에 따른 운동 목록 출력
        private void UpdateCurrentPageData()
        {
            // Skip: 현재 페이지 이전의 항목을 건너뜀.
            // Tack: 페이지 크기만큼 데이터를 가져옴.
            // PageSize = 10, CurrentPage = 2 → Skip(10).Take(10) → 데이터 11~20번 가져옴.
            if (AllData == null || AllData.Count == 0)
                return;

            int totalDataCount = AllData.Count; // 전체 데이터 수
            int remainingDataCount = totalDataCount - ((CurrentPage - 1) * _pageSize); // 남은 데이터 수

            int dataToTake = Math.Min(_pageSize, remainingDataCount); // 현재 페이지에 가져올 데이터 수

            CurrentPageData = new ObservableCollection<Exercise>(
                AllData.Skip((CurrentPage - 1) * _pageSize).Take(dataToTake));

            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(CurrentPageData));
            });
        }

        private void NextPage()
        {
            if (CanGoToNextPage())
            {
                CurrentPage++;
            }
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
                CurrentPage--;
        }

        private bool CanGoToNextPage() => CurrentPage < TotalPages;
        private bool CanGoToPreviousPage() => CurrentPage > 1;
    }
}
