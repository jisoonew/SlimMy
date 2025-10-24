using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class GetTodayWeightCompletedRes
    {
        public bool ok { get; set; }
        public string message { get; set; } = "ok";
        public int weightCount { get; set; }
    }
}
