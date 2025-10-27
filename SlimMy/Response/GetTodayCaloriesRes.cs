using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class GetTodayCaloriesRes
    {
        public bool ok { get; set; }
        public string message { get; set; }
        public int totalCalories { get; set; }
    }
}
