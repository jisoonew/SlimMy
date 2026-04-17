using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
        private string _planType;
        private int _setCount;
        private int _repCount;
        private Visibility _isTimeExercise;
        private Visibility _isSetExercise;

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

        public string PlanType
        {
            get { return _planType; }
            set { _planType = value; OnPropertyChanged(nameof(PlanType)); }
        }

        public int SetCount
        {
            get { return _setCount; }
            set { _setCount = value; OnPropertyChanged(nameof(SetCount)); }
        }

        public int RepCount
        {
            get { return _repCount; }
            set { _repCount = value; OnPropertyChanged(nameof(RepCount)); }
        }

        public Visibility IsTimeExercise
        {
            get { return _isTimeExercise; }
            set { _isTimeExercise = value; OnPropertyChanged(nameof(IsTimeExercise)); }
        }

        public Visibility IsSetExercise
        {
            get { return _isSetExercise; }
            set { _isSetExercise = value; OnPropertyChanged(nameof(IsSetExercise)); }
        }

        public override string ToString()
        {
            return $"[{Minutes}분] {Name} {Calories}kcal";
        }
    }
}
