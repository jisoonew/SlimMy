using SlimMy.Model;
using SlimMy.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    public class PlannerViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

        public ICommand ExerciseCommand { get; set; }

        public PlannerViewModel()
        {
            _navigationService = new NavigationService();

            ExerciseCommand = new Command(AddExerciseNavigation);
        }

        // 운동 추가 뷰
        public void AddExerciseNavigation(object parameter)
        {
            _navigationService.NavigateToAddExercise();
        }
    }
}
