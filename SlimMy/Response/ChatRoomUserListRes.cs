using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class ChatRoomUserListRes
    {
        public bool ok { get; set; }
        public string message { get; set; }
        public List<string> users { get; set; }
    }
}
