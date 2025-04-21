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
        private readonly INavigationService _navigationService;

        public ICommand ExerciseCommand { get; set; }

        public ObservableCollection<string> Items { get; set; }

        private static PlannerViewModel _instance;
        public static PlannerViewModel Instance => _instance ?? (_instance = new PlannerViewModel());

        public PlannerViewModel()
        {
            _navigationService = new NavigationService();

            ExerciseCommand = new Command(AddExerciseNavigation);

            Items = new ObservableCollection<string>();
        }

        // 운동 추가 뷰
        public void AddExerciseNavigation(object parameter)
        {
            _navigationService.NavigateToAddExercise();
        }
        
        public void SelectedPlannerPrint(Exercise exerciseData, string calories)
        {
            Items.Clear();
            Items.Add(exerciseData.ExerciseName);
            Items.Add(calories);

            MessageBox.Show("여기는 플래너 : " + exerciseData.ExerciseID + "\n" + exerciseData.ExerciseName + "\n" + calories);
        }
    }
}
