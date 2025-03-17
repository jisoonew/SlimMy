using SlimMy.Service;
using System;
using System.Collections.Generic;
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

        public void AddExerciseNavigation(object parameter)
        {
            _navigationService.NavigateToAddExercise();
        }
    }
}
