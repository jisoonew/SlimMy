using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class Chat : INotifyPropertyChanged
    {
        private Guid chatRoomId;
        private string chatRoomName;
        private string description;
        private string category;
        private DateTime createdAt;

        public string CreatorEmail { get; set; }

        public Guid ChatRoomId
        {
            get { return chatRoomId; }
            set { chatRoomId = value; OnPropertyChanged(nameof(chatRoomId)); }
        }

        public string ChatRoomName
        {
            get { return chatRoomName; }
            set { chatRoomName = value; OnPropertyChanged(nameof(chatRoomName)); }
        }

        public string Description
        {
            get { return description; }
            set { description = value; OnPropertyChanged(nameof(description)); }
        }

        public string Category
        {
            get { return category; }
            set { category = value; OnPropertyChanged(nameof(category)); }
        }

        public DateTime CreatedAt
        {
            get { return createdAt; }
            set { createdAt = value; OnPropertyChanged(nameof(createdAt)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
