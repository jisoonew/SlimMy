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

        public int UpdateIndex { get; set; }

        public ICommand DeleteCommand { get; set; }

        public ICommand SaveCommand { get; set; }

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

        private DateTime _selectedDate;
        public DateTime SelectedDate
        {
            get { return _selectedDate; }
            set { _selectedDate = value; OnPropertyChanged(nameof(SelectedDate)); }
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

            UpdateIndex = Items.IndexOf(SelectedPlannerData);

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
                Items[UpdateIndex].Calories = double.Parse(calories);
            }
        }

        // 플래너 삭제
        public void DeletePlannerPrint(object parameter)
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

                // Guid userID, Guid Exercise_info_id, int indexnum, int minutes, int calories, char isCompleted

                int itemIndex = 0;
                foreach (var item in Items)
                {
                    // MessageBox.Show(currentUser.UserId + "\n" + item.ExerciseID + "\n" + itemIndex + "\n" + item.Minutes + "\n" + item.Calories + "\n" + item.IsCompleted);

                    _repo.InsertPlanner(currentUser.UserId, item.ExerciseID, itemIndex, item.Minutes, (int)item.Calories, item.IsCompleted, SelectedDate);
                    itemIndex++;
                }

                // MessageBox.Show(currentUser.UserId + "\n" + currentUser.NickName + "\n" + currentUser.NickName + "\n" + currentUser.NickName + "\n" + currentUser.NickName + "\n");
                // await _repo.InsertPlanner();
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
                Calories = double.Parse(calories)
            });
        }
    }
}
