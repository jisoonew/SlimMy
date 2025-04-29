using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class PlannerExercise : BaseViewModel
    {
        public Guid PlannerID { get; set; }
        public Guid Exercise_Info_ID { get; set; }
        public string ExerciseName { get; set; }
        public int Indexnum { get; set; }
        public int Minutes { get; set; }
        public int Calories { get; set; }
        public bool IsCompleted { get; set; }
    }
}
