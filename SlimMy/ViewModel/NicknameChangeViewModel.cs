using SlimMy.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SlimMy.ViewModel
{
    public class NicknameChangeViewModel : BaseViewModel
    {
        private UserRepository _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        private string _newNickname;
        public string NewNickname
        {
            get => _newNickname;
            set
            {
                _newNickname = value;
                OnPropertyChanged(nameof(NewNickname));
            }
        }

        // 닉네임 성공 메시지
        private bool _nickNameCheck;
        public bool NickNameCheck
        {
            get { return _nickNameCheck; }
            set { _nickNameCheck = value; OnPropertyChanged(nameof(NickNameCheck)); }
        }

        // 닉네임 실패 메시지
        private bool _nickNameNoCheck;
        public bool NickNameNoCheck
        {
            get { return _nickNameNoCheck; }
            set { _nickNameNoCheck = value; OnPropertyChanged(nameof(NickNameNoCheck)); }
        }

        public NicknameChangeViewModel(string initialNickname)
        {
            NewNickname = initialNickname;
        }

        public NicknameChangeViewModel() { }

        private async Task Initialize()
        {
            _repo = new UserRepository(_connstring); // Repo 초기화
        }

        public static async Task<NicknameChangeViewModel> CreateAsync()
        {
            try
            {
                var vm = new NicknameChangeViewModel();
                await vm.Initialize();
                return vm;
            }
            catch (Exception ex)
            {
                MessageBox.Show("NicknameChangeViewModel 생성 실패: " + ex.Message);
                return null;
            }
        }

        // 닉네임 여부
        public async Task NickNameCheckPrint(string nickName)
        {
            MessageBox.Show("왔닝? : " + nickName);

            //var allUserNickName = await _repo.AllNickName();

            //foreach (var nickNameList in allUserNickName)
            //{
            //    if (nickNameList.Equals(nickName))
            //    {
            //        NickNameCheck = false;
            //        NickNameNoCheck = true;
            //    }
            //    else
            //    {
            //        NickNameCheck = true;
            //        NickNameNoCheck = false;
            //    }
            //}
        }
    }
}
