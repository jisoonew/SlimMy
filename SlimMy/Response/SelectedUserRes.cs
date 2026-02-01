using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class SelectedUserRes
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public User UserData { get; set; }
        public Guid RequestID { get; set; }
    }
}
