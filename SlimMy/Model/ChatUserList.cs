using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class ChatUserList
    {
        public string UsersID { get; set; }
        public string UsersNickName { get; set; }
        public string HostUserID { get; set; }

        public ChatUserList() { }

        public ChatUserList(string userId, string nickName)
        {
            UsersID = userId;
            UsersNickName = nickName;
        }
    }
}
