using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class PlanItem : BaseViewModel
    {
        private Guid plannerID;
        private Guid exerciseID;
        private string name;
        private int minutes;
        private int calories;
        private bool isCompleted;

        public Guid PlannerID
        {
            get { return plannerID; }
            set { plannerID = value; OnPropertyChanged(nameof(PlannerID)); }
        }

        public Guid ExerciseID
        {
            get { return exerciseID; }
            set { exerciseID = value; OnPropertyChanged(nameof(ExerciseID)); }
        }

        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged(nameof(Name)); }
        }

        public int Minutes
        {
            get { return minutes; }
            set { minutes = value; OnPropertyChanged(nameof(Minutes)); }
        }

        public int Calories
        {
            get { return calories; }
            set { calories = value; OnPropertyChanged(nameof(Calories)); }
        }

        public bool IsCompleted
        {
            get { return isCompleted; }
            set { isCompleted = value; OnPropertyChanged(nameof(IsCompleted)); }
        }

        public override string ToString()
        {
            return $"[{Minutes}분] {Name} {Calories}kcal";
        }
    }
}
