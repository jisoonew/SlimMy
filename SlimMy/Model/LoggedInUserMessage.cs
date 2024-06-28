using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class LoggedInUserMessage
    {
        public User User { get; }

        public LoggedInUserMessage(User user)
        {
            User = user;
        }
    }
}
