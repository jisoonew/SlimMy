using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    class LoginReply
    {
        public bool ok { get; set; }
        public string message { get; set; }
        public Guid userId { get; set; }
        public string nick { get; set; }
        public string accessToken { get; set; }
        public Guid requestId { get; set; }
    }
}
