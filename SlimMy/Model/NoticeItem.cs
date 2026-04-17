using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class NoticeItem : BaseViewModel
    {
        public Guid NoticeID { get; set; }
        public string Title { get; set; } = "";
        public string NoticeType { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }

        private bool _isRead;
        public bool IsRead
        {
            get => _isRead;
            set
            {
                if (_isRead == value) return;
                _isRead = value;
                OnPropertyChanged(nameof(IsRead));
            }
        }

        public string CreatedAtText => CreatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
    }
}
