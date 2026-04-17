using SlimMy.Model;
using SlimMy.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SlimMy.ViewModel
{
    public class NoticeDetailViewModel : BaseViewModel
    {
        // 공지 제목
        private string _noticeTitle;
        public string NoticeTitle
        {
            get { return _noticeTitle; }
            set { _noticeTitle = value; OnPropertyChanged(nameof(NoticeTitle)); }
        }

        // 공지 타입
        private string _noticeType;
        public string NoticeType
        {
            get { return _noticeType; }
            set { _noticeType = value; OnPropertyChanged(nameof(NoticeType)); }
        }

        // 공지 생성일
        private string _createdAtText;
        public string CreatedAtText
        {
            get { return _createdAtText; }
            set { _createdAtText = value; OnPropertyChanged(nameof(CreatedAtText)); }
        }

        // 공지 내용
        private string _noticeContent;
        public string NoticeContent
        {
            get { return _noticeContent; }
            set { _noticeContent = value; OnPropertyChanged(nameof(NoticeContent)); }
        }

        public NoticeDetailViewModel(NoticeItem notice)
        {
            NoticeTitle = notice.Title;
            NoticeType = "신고";
            CreatedAtText = notice.CreatedAtText;
            NoticeContent = notice.Content;
        }

        public static async Task<NoticeDetailViewModel> CreateAsync(NoticeItem notice)
        {
            try
            {
                var vm = new NoticeDetailViewModel(notice);
                await vm.Initialize();
                return vm;
            }
            catch (Exception ex)
            {
                MessageBox.Show("NoticeDetailViewModel 생성 실패: " + ex.Message);
                return null;
            }
        }

        private async Task Initialize()
        {
        }
    }
}
