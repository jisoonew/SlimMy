using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Interface
{
    public interface ICommunity
    {
        List<User> GetUsers();
    }
}
