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
        public string UsersNickName { get; set; } // NickName 속성 추가
        public string HostUserID { get; set; }

        public ChatUserList(string userId, string nickName)
        {
            this.UsersID = userId;
            this.UsersNickName = nickName;
        }
    }
}
