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
    public class PlannerViewModel : BaseViewModel
    {
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        private readonly INavigationService _navigationService;

        public ICommand ExerciseCommand { get; set; }

        public ObservableCollection<PlanItem> Items { get; set; }

        public ICommand SelectedPlnnaerCommand { get; set; }

        public ICommand DeletePlannerGroupCommand { get; set; }

        public int UpdateIndex { get; set; }

        public ICommand DeleteCommand { get; set; }

        public ICommand SaveCommand { get; set; }

        // 새로 만들기
        public ICommand NewPlannerCommand { get; set; }

        private PlanItem _selectedPlnnerData;
        public PlanItem SelectedPlannerData
        {
            get { return _selectedPlnnerData; }
            set
            {
                if (_selectedPlnnerData != value)
                {
                    _selectedPlnnerData = value;
                    OnPropertyChanged(nameof(SelectedPlannerData));
                }
                else
                {
                    // 동일한 항목을 선택해도 동작하도록 처리
                    OnPropertyChanged(nameof(SelectedPlannerData));
                }
            }
        }

        private string _plannerTitle;
        public string PlannerTitle
        {
            get { return _plannerTitle; }
            set { _plannerTitle = value; OnPropertyChanged(nameof(PlannerTitle)); }
        }

        private DateTime _selectedDate;
        public DateTime SelectedDate
        {
            get { return _selectedDate; }
            set {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged(nameof(SelectedDate));

                    // 특정 날짜에 해당하는 플래너 내역 출력
                    SelectedDatePlanner();
                }
            }
        }

        private string _totalCalories;
        public string TotalCalories
        {
            get { return _totalCalories; }
            set { _totalCalories = value; OnPropertyChanged(nameof(TotalCalories)); }
        }

        // 해당 운동 리스트 위로
        public ICommand MoveUpCommand => new RelayCommand(_ =>
        {
            int index = Items.IndexOf(SelectedPlannerData);
            if(index > 0)
                Items.Move(index, index - 1);
        }, _ => SelectedPlannerData != null && Items.IndexOf(SelectedPlannerData) > 0);

        // 해당 운동 리스트 아래로
        public ICommand MoveDownCommand => new RelayCommand(_ => {
            int index = Items.IndexOf(SelectedPlannerData);
            if (index < Items.Count - 1)
                Items.Move(index, index + 1);
        }, _ => SelectedPlannerData != null && Items.IndexOf(SelectedPlannerData) < Items.Count - 1);

        public ICommand UpdateCommand { set; get; }

        private static PlannerViewModel _instance;
        public static PlannerViewModel Instance => _instance ?? (_instance = new PlannerViewModel());

        private ObservableCollection<PlannerWithGroup> _plannerGroups;
        public ObservableCollection<PlannerWithGroup> PlannerGroups
        {
            get => _plannerGroups;
            set { _plannerGroups = value; OnPropertyChanged(nameof(PlannerGroups)); }
        }

        private PlannerWithGroup _selectedPlannerGroup;
        public PlannerWithGroup SelectedPlannerGroup
        {
            get => _selectedPlannerGroup;
            set
            {
                if (_selectedPlannerGroup != value)
                {
                    _selectedPlannerGroup = value;
                    OnPropertyChanged(nameof(SelectedPlannerGroup));

                    // 운동 리스트 세팅
                    if (_selectedPlannerGroup != null)
                    {
                        Items = new ObservableCollection<PlanItem>(
                            _selectedPlannerGroup.Exercises.Select(ex => new PlanItem
                            {
                                ExerciseID = ex.Exercise_Info_ID,
                                PlannerID = ex.PlannerID,
                                IsCompleted = ex.IsCompleted,
                                Name = ex.ExerciseName,
                                Minutes = ex.Minutes,
                                Calories = ex.Calories
                            }));

                        OnPropertyChanged(nameof(Items));

                        // 선택한 플래너의 총 칼로리
                        SelectedTotalCalories();
                    }
                    else
                    {
                        Items.Clear();
                        OnPropertyChanged(nameof(Items));
                    }
                }
            }
        }

        public IEnumerable<DateTime> HighlightDates { get; set; }


        public PlannerViewModel()
        {
            _repo = new Repo(_connstring);

            _navigationService = new NavigationService();

            ExerciseCommand = new Command(AddExerciseNavigation);

            Items = new ObservableCollection<PlanItem>();

            SelectedPlnnaerCommand = new RelayCommand(PrintPlannerData);

            UpdateCommand = new RelayCommand(UpdatePlannerData);

            DeleteCommand = new RelayCommand(DeletePlannerPrint);

            SaveCommand = new RelayCommand(InsertPlannerPrint);

            DeletePlannerGroupCommand = new RelayCommand(AllDeletePlanner);

            NewPlannerCommand = new RelayCommand(CleanPlanner);

            SelectedDate = DateTime.Now;

            HighlightDates = new List<DateTime>
        {
            new DateTime(2025, 4, 30),
            new DateTime(2025, 5, 1)
        };

            Items.Clear();
        }

        // 운동 추가 뷰
        public void AddExerciseNavigation(object parameter)
        {
            _navigationService.NavigateToAddExercise();
        }

        // 리스트 선택한 운동 데이터 출력
        public void PrintPlannerData(object parameter)
        {
            if (parameter is PlanItem planItem)
            {
                SelectedPlannerData = planItem;
            }
        }

        // 리스트 선택한 운동 데이터 수정
        public void UpdatePlannerData(object parameter)
        {
            // 운동 추가 뷰모델에게 데이터 수정을 알림
            ExerciseViewModel.IsEditMode = true;

            UpdateIndex = Items.IndexOf(SelectedPlannerData);

            // 운동 추가 뷰 생성
            _navigationService.NavigateToAddExercise();
        }

        // 플래너 수정
        public void UpdatePlannerPrint(Exercise exerciseData, string calories, int minutes)
        {
            if (UpdateIndex >= 0)
            {
                Items[UpdateIndex].ExerciseID = exerciseData.ExerciseID;
                Items[UpdateIndex].Name = exerciseData.ExerciseName;
                Items[UpdateIndex].Minutes = minutes;
                Items[UpdateIndex].Calories = int.Parse(calories);

                TotalCaloriesCalculate();
            }
        }

        // 플래너 목록(리스트) 삭제
        public void DeletePlannerPrint(object parameter)
        {
            if(SelectedPlannerData != null)
            {
                string msg = string.Format("{0}를 삭제하시겠습니까?", SelectedPlannerData.Name);
                MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (messageBoxResult == MessageBoxResult.No)
                {
                    return;
                }
                else
                {
                    Items.Remove(SelectedPlannerData);
                    SelectedPlannerData = null;
                }
            }
            else
            {
                MessageBox.Show("운동 항목을 선택해주세요.");
            }
        }

        // 플래너 저장
        public void InsertPlannerPrint(object parameter)
        {
            string msg = string.Format("저장하시겠습니까?");
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                User currentUser = UserSession.Instance.CurrentUser;

                // 선택된 플래너가 있다면
                if (SelectedPlannerGroup != null)
                {
                    // 해당 플래너가 존재하는가?
                    var originalListFromDb = _repo.GetPlannerItemsByGroupId(SelectedPlannerGroup.PlannerGroupId);

                    // 현재 플래너 목록의 플래너 아이디를 가져온다
                    var currentItemIds = new HashSet<Guid>(Items.Select(i => i.PlannerID));

                    // 현재 플래너 목록의 플래너 아이디에 DB에 존재하는 데이터와 일치하지 않는 아이디
                    // 즉, DB에는 있고, 현재 플래너 목록에는 없는 플래너 아이디를 추출
                    var deletedItems = originalListFromDb.Where(dbItem => !currentItemIds.Contains(dbItem.PlannerID));

                    if (deletedItems != null)
                    {
                        foreach (var a in deletedItems)
                        {
                            // 현재 플래너 목록에 없는 플래너 아이디는 모두 삭제
                            _repo.DeletePlannerList(a.PlannerID);
                        }
                    }
                }
               
                if (_repo.ExerciseCheck(currentUser.UserId, SelectedDate))
                {
                    // 플래너 리스트 수정
                    _repo.UpdatePlanner(SelectedPlannerGroup.PlannerGroupId, PlannerTitle, Items.ToList());
                }
                else
                {
                    // 플래너 저장
                    _repo.InsertPlanner(currentUser.UserId, PlannerTitle, SelectedDate, Items.ToList());
                }

            }
        }

        // 플래너 출력
        public void PlannerPrint()
        {
            User currentUser = UserSession.Instance.CurrentUser;

            SelectedDate = SelectedDate.Date;

            var resultList = _repo.ExerciseList(currentUser.UserId, SelectedDate);

            // 특정 날짜의 콤보 박스에 플래너 내역 가져오기
            PlannerGroups = new ObservableCollection<PlannerWithGroup>(resultList);
        }

        // 특정 날짜에 해당하는 플래너 내역 출력
        public void SelectedDatePlanner()
        {
            if (UserSession.Instance.CurrentUser != null)
            {
                var resultList = _repo.ExerciseList(UserSession.Instance.CurrentUser.UserId, SelectedDate.Date);
                PlannerGroups = new ObservableCollection<PlannerWithGroup>(resultList);

                // 플래너 목록(콤보 박스)의 첫 번째 요소 혹은 null 반환
                SelectedPlannerGroup = PlannerGroups.FirstOrDefault();

                // 선택한 플래너의 총 칼로리
                SelectedTotalCalories();
            }
        }

        // 선택한 플래너의 총 칼로리
        public void SelectedTotalCalories()
        {
            int totalCalorieSum;

            if (SelectedPlannerGroup != null)
            {
                totalCalorieSum = 0;

                foreach (var result in SelectedPlannerGroup.Exercises)
                {
                    totalCalorieSum += result.Calories;
                }
            }
            else
            {
                totalCalorieSum = 0;
            }

            TotalCalories = totalCalorieSum.ToString();
        }

        // 플래너 목록을 수정 이후의 칼로리 계산
        public void TotalCaloriesCalculate()
        {
            int totalCalorieSum;

            if (Items != null)
            {
                totalCalorieSum = 0;

                foreach (var result in Items)
                {
                    totalCalorieSum += result.Calories;
                }
            }
            else
            {
                totalCalorieSum = 0;
            }

            TotalCalories = totalCalorieSum.ToString();
        }

        // 해당 플래너 전체 삭제
        public void AllDeletePlanner(object parameter)
        {
            string msg = string.Format("영구 삭제하시겠습니까?");
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                // 플래너 전체 삭제
                _repo.DeletePlanner(SelectedPlannerGroup.PlannerGroupId);
            }
        }

        // 운동 선택 이후에 데이터를 리스트 박스에 출력
        public void SelectedPlannerPrint(Exercise exerciseData, string calories, int minutes)
        {
            Items.Add(new PlanItem
            {
                ExerciseID = exerciseData.ExerciseID,
                Name = exerciseData.ExerciseName,
                Minutes = minutes,
                Calories = int.Parse(calories)
            });

            // 선택한 플래너의 총 칼로리
            TotalCaloriesCalculate();
        }

        // 새로 만들기
        public void CleanPlanner(object parameter)
        {
            string msg = string.Format("새로 플래너를 작성하시겠습니까? \n" + "기존의 데이터는 저장되지 않습니다.");
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                Items.Clear();
                PlannerGroups.Clear();
                PlannerTitle = null;
                SelectedDate = DateTime.Now;
                TotalCalories = null;
            }
        }
    }
}
