using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class SelectChatRoomRes
    {
        public bool ok { get; set; }
        public string message { get; set; }
        public IEnumerable<ChatRooms> chatRooms { get; set; }
        public Guid reqId { get; set; }
    }
}
