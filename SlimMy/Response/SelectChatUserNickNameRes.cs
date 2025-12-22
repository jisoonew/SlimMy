using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class SelectChatUserNickNameRes
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public List<ChatUserList> UsersInChatRoom { get; set; }
    }
}
