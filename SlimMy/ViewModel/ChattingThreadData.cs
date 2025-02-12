using SlimMy.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlimMy.ViewModel
{
    public class ChattingThreadData
    {
        public static int chattingRoomCnt = 0;
        public Thread chattingThread;
        public ChattingWindow chattingWindow;
        public int chattingRoomNum;
        public string chattingRoomNumStr;
        private static object obj = new object();

        public ChattingThreadData(Thread chattingThread, ChattingWindow chattingWindow)
        {
            lock (obj)
            {
                this.chattingThread = chattingThread;
                this.chattingWindow = chattingWindow;
                this.chattingRoomNum = ++chattingRoomCnt;
            }
        }

        public ChattingThreadData(Thread chattingThread, ChattingWindow chattingWindow, string chatRoom)
        {
            lock (obj)
            {
                this.chattingThread = chattingThread;
                this.chattingWindow = chattingWindow;
                this.chattingRoomNumStr = chatRoom;
            }
        }
    }
}
