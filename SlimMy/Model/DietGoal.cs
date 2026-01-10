using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class DietGoal : BaseViewModel
    {
        private Guid _userID;
        private string _mindset;
        private DateTime _startDate;
        private DateTime _endDate;
        private double _weight;
        private double _height;
        private double _goalWeight;
        private double _bmi;

        public Guid UserID
        {
            get { return _userID; }
            set { _userID = value; OnPropertyChanged(nameof(UserID)); }
        }

        public string Mindset
        {
            get { return _mindset; }
            set { _mindset = value; OnPropertyChanged(nameof(Mindset)); }
        }

        public DateTime StartDate
        {
            get { return _startDate; }
            set { _startDate = value; OnPropertyChanged(nameof(StartDate)); }
        }

        public DateTime EndDate
        {
            get { return _endDate; }
            set { _endDate = value; OnPropertyChanged(nameof(EndDate)); }
        }

        public double Weight
        {
            get { return _weight; }
            set { _weight = value; OnPropertyChanged(nameof(Weight)); }
        }

        public double Height
        {
            get { return _height; }
            set { _height = value; OnPropertyChanged(nameof(Height)); }
        }

        public double GoalWeight
        {
            get { return _goalWeight; }
            set { _goalWeight = value; OnPropertyChanged(nameof(GoalWeight)); }
        }

        public double BMI
        {
            get { return _bmi; }
            set { _bmi = value; OnPropertyChanged(nameof(BMI)); }
        }
    }
}
