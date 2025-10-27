using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class GetWeeklyCaloriesRes
    {
        public bool ok { get; set; }
        public string message { get; set; }
        public List<DailyCalorie> list { get; set; }
    }
}
