using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Singleton
{
    class ChattingSession
    {
        private static ChattingSession _instance;
        public ChatRooms CurrentChattingData { get; set; }

        private ChattingSession() { }

        public static ChattingSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ChattingSession();
                }
                return _instance;
            }
        }
    }
}
