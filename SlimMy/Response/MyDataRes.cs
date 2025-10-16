using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public sealed class MyDataRes
    {
        public bool ok { get; set; }
        public string message { get; set; } = "ok";
        public User userData { get; set; }
    }
}
