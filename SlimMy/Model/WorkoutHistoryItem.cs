using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class WorkoutHistoryItem : BaseViewModel
    {
        private DateTime _plannerDate;
        private string _exerciseName;
        private int _minutes;
        private string _planType;
        private int _setCount;
        private int _repCount;
        private int _calories;
        private string _category;
        private string _exerciseAmount;

        public DateTime PlannerDate
        {
            get { return _plannerDate; }
            set { _plannerDate = value; OnPropertyChanged(nameof(PlannerDate)); }
        }

        public string ExerciseName
        {
            get { return _exerciseName; }
            set { _exerciseName = value; OnPropertyChanged(nameof(ExerciseName)); }
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

        public int Minutes
        {
            get { return _minutes; }
            set { _minutes = value; OnPropertyChanged(nameof(Minutes)); }
        }

        public int Calories
        {
            get { return _calories; }
            set { _calories = value; OnPropertyChanged(nameof(Calories)); }
        }

        public string Category
        {
            get { return _category; }
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        public string ExerciseAmount
        {
            get { return _exerciseAmount; }
            set { _exerciseAmount = value; OnPropertyChanged(nameof(ExerciseAmount)); }
        }
    }
}
