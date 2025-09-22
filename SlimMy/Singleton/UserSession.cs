using SlimMy.Model;
using SlimMy.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy
{
    public class UserSession
    {
        private static UserSession _instance;
        public User CurrentUser { get; set; }
        public INetworkTransport Transport { get; set; }

        private UserSession() { }

        public static UserSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UserSession();
                }
                return _instance;
            }
        }
    }
}
