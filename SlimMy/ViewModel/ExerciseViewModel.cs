using SlimMy.Model;
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
    public class ExerciseViewModel : BaseViewModel
    {
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        private int _currentPage; // 현재 페이지 번호.
        private int _totalPages; // 전체 데이터에서 생성된 총 페이지 수.
        private int _pageSize = 20; // 페이지당 항목 수

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

        private int _selectedChatRoomIndex;
        public int SelectedChatRoomIndex
        {
            get => _selectedChatRoomIndex;
            set
            {
                _selectedChatRoomIndex = value;
                OnPropertyChanged(nameof(SelectedChatRoomIndex)); // UI에 변경 알림
            }
        }

        private Exercise _selectedChatRoom;
        public Exercise SelectedChatRoom
        {
            get { return _selectedChatRoom; }
            set
            {
                if (_selectedChatRoom != value)
                {
                    _selectedChatRoom = value;
                    OnPropertyChanged(nameof(SelectedChatRoom));
                }
                else
                {
                    // 동일한 항목을 선택해도 동작하도록 처리
                    OnPropertyChanged(nameof(SelectedChatRoom));
                }
            }
        }

        public ObservableCollection<Exercise> AllData { get; set; } // 전체 데이터

        public ICommand NextPageCommand { get; set; }
        public ICommand PreviousPageCommand { get; set; }

        public ICommand TestCommand { get; set; }

        public ExerciseViewModel()
        {
            _repo = new Repo(_connstring);
            Exercises = new ObservableCollection<Exercise>();

            ExerciseList();

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

            TestCommand = new RelayCommand(ExerciseTest);
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
                    ImagePath = exerciseBundle.ImagePath
                });
            }
        }

        public void ExerciseTest(object parameter)
        {
            if (parameter is Exercise exerciseData)
            {
                SelectedChatRoom = exerciseData;
                MessageBox.Show("AllData : " + exerciseData.ExerciseID + "이름 : " + exerciseData.ExerciseName);
            }
        }

        // 페이징에 따른 운동 목록
        public void ChattingRefreshChatRooms()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var chatRooms = _repo.AllExerciseList(); // DB에서 최신 데이터 가져오기

                if (AllData == null)
                    AllData = new ObservableCollection<Exercise>();

                AllData.Clear();

                foreach (var chatRoom in chatRooms)
                {
                    AllData.Add(chatRoom);
                }

                // 총 페이지 수 재계산
                TotalPages = (int)Math.Ceiling((double)AllData.Count / _pageSize);

                if (CurrentPage > TotalPages)
                    CurrentPage = TotalPages;

                // 현재 페이지 데이터 업데이트
                UpdateCurrentPageData();
            });
        }

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
