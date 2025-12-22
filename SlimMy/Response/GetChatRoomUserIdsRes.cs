using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class GetChatRoomUserIdsRes
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public List<string> UserIDBundle { get; set; }
    }
}
