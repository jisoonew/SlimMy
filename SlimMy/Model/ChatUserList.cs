using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class ChatUserList
    {
        public string UsersName { get; set; }

        public ChatUserList(string name)
        {
            this.UsersName = name;
        }
    }
}
