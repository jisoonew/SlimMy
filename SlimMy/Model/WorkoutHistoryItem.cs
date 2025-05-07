using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    class WorkoutHistoryItem : BaseViewModel
    {
        private DateTime _plannerDate;
        private string _exerciseName;
        private int _minutes;
        private int _calories;
        private string _category;

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
    }
}
