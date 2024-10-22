using SlimMy.Interface;
using SlimMy.Model;
using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SlimMy.View
{
    /// <summary>
    /// Community.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Community : Page
    {
        public Community()
        {
            InitializeComponent();
        }

        public Community(User userModel)
        {
        }

        private void CommunityChat_Click(object sender, RoutedEventArgs e)
        {
            CreateChatRoom createChatRoom = new CreateChatRoom();
            createChatRoom.Show();
        }

        //private void ChatRoomListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var selectedChatRoom = ChatRoomListView.SelectedItem as Chat;

        //    if (selectedChatRoom != null)
        //    {
        //        // ViewModel에서 GUID 속성을 사용하여 선택된 항목의 GUID 가져오기
        //        Guid selectedGuid = selectedChatRoom.ChatRoomId;

        //        // GUID 출력
        //        // MessageBox.Show(selectedGuid.ToString()); // 예시: GUID를 문자열로 출력
        //    }
        //}
    }
}
