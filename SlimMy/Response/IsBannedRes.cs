using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class IsBannedRes
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public bool IsBannedCheck { get; set; }
        public Guid RequestID { get; set; }
    }
}
