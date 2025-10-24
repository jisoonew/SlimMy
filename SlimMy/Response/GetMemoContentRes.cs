using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class GetMemoContentRes
    {
        public bool ok { get; set; }
        public string message { get; set; } = "ok";
        public WeightMemoItem memoContent { get; set; }
    }
}
