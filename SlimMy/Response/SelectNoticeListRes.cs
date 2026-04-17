using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class SelectNoticeListRes
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public List<NoticeItem> NoticeItemList { get; set; }
    }
}
