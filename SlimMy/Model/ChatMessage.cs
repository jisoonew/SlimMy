using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SlimMy.Model
{
    public class ChatMessage : BaseViewModel
    {
        public Guid MessageID { get; set; }
        public Guid SenderID { get; set; }
        public string SendUserNickName { get; set; }
        public string Message { get; set; }
        public string MessageContent { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsMine { get; set; }
        public TextAlignment Alignment { get; set; }

        private bool _isSelectedForReport;
        public bool IsSelectedForReport
        {
            get => _isSelectedForReport;
            set
            {
                if (_isSelectedForReport == value) return;
                _isSelectedForReport = value;
                OnPropertyChanged(nameof(IsSelectedForReport));
                SelectionChanged?.Invoke(this, _isSelectedForReport);
            }
        }

        public event Action<ChatMessage, bool>? SelectionChanged;
    }
}
