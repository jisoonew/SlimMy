using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class WeightMemoItem : BaseViewModel
    {
        private Guid _memoID;
        private string _content;

        public Guid MemoID
        {
            get { return _memoID; }
            set { _memoID = value; OnPropertyChanged(nameof(MemoID)); }
        }

        public string Content
        {
            get { return _content; }
            set { _content = value; OnPropertyChanged(nameof(Content)); }
        }
    }
}
