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
        public string UsersName { get; set; }
        public string UsersNickName { get; set; } // NickName 속성 추가

        public ChatUserList(string name, string nickName)
        {
            this.UsersName = name;
            this.UsersNickName = nickName;
        }
    }
}
