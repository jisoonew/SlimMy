using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class CheckUserSanctionRes
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public bool SanctionCheck { get; set; }
        public DateTime? SanctionDate { get; set; }
        public Guid RequestID { get; set; }
    }
}
