using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Test
{
    public interface IDataService
    {
        string GetUserId();
        void SetUserId(string userId);
    }
}
