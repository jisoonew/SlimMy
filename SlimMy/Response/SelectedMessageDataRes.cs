using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class SelectedMessageDataRes
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public ChatMessage ChatMessageData { get; set; }
        public Guid RequestID { get; set; }
    }
}
