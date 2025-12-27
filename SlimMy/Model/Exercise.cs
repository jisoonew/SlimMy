using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SlimMy.Model
{
    public class Exercise
    {
        public Guid ExerciseID { get; set; }
        public string ExerciseName { get; set; }
        public BitmapImage ImagePath { get; set; }
        public byte[] ImagePathByte { get; set; }
        public decimal Met { get; set; }
    }
}
