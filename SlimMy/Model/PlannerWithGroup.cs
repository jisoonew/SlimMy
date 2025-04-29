using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class PlannerWithGroup : BaseViewModel
    {
        private Guid plannerGroupId;
        private DateTime plannerDate;
        private string plannerTitle;
        private List<PlannerExercise> exercises = new List<PlannerExercise>();

        public Guid PlannerGroupId
        {
            get => plannerGroupId;
            set { plannerGroupId = value; OnPropertyChanged(nameof(PlannerGroupId)); }
        }

        public DateTime PlannerDate
        {
            get => plannerDate;
            set { plannerDate = value; OnPropertyChanged(nameof(PlannerDate)); }
        }

        public string PlannerTitle
        {
            get => plannerTitle;
            set { plannerTitle = value; OnPropertyChanged(nameof(PlannerTitle)); }
        }

        public List<PlannerExercise> Exercises
        {
            get => exercises;
            set { exercises = value; OnPropertyChanged(nameof(Exercises)); }
        }
    }
}
