using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class WeightRecordItem : BaseViewModel
    {
        private Guid _bodyid;
        private DateTime _date;
        private double _height;
        private double _weight;
        private string _weightDiffFromPrevious;
        private double _bmi;
        private double? _targetWeight;

        public Guid BodyID
        {
            get { return _bodyid; }
            set { _bodyid = value; OnPropertyChanged(nameof(BodyID)); }
        }

        public DateTime Date
        {
            get { return _date; }
            set { _date = value; OnPropertyChanged(nameof(Date)); }
        }

        public double Height
        {
            get { return _height; }
            set { _height = value; OnPropertyChanged(nameof(Height)); }
        }

        public double Weight
        {
            get { return _weight; }
            set { _weight = value; OnPropertyChanged(nameof(Weight)); }
        }

        public string WeightDiffFromPrevious
        {
            get { return _weightDiffFromPrevious; }
            set { _weightDiffFromPrevious = value; OnPropertyChanged(nameof(WeightDiffFromPrevious)); }
        }

        public double BMI
        {
            get { return _bmi; }
            set { _bmi = value; OnPropertyChanged(nameof(BMI)); }
        }

        public double? TargetWeight
        {
            get { return _targetWeight; }
            set { _targetWeight = value; OnPropertyChanged(nameof(TargetWeight)); }
        }
    }
}
