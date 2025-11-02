using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class SendNickNameRes
    {
        public bool ok { get; set; }
        public string message { get; set; }
        public string senderNickName { get; set; }
    }
}
