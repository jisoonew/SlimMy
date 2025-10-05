using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    class SignUpReply
    {
        public bool ok { get; set; }
        public string message { get; set; }
        public Guid userId { get; set; }
        public string nick { get; set; }
        public bool needsEmailVerification { get; set; }
    }
}
