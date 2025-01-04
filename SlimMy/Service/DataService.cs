using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Service
{
    public class DataService : IDataService
    {
        private string _userId;

        public string GetUserId()
        {
            return _userId;
        }

        public void SetUserId(string userId)
        {
            _userId = userId;
        }

    }
}
