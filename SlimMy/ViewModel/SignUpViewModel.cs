using SlimMy.Model;
using SlimMy.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace SlimMy.ViewModel
{
    public class SignUpViewModel : BaseViewModel
    {
//        public AsyncRelayCommand InsertCommand { get; set; }

//        private User _user;

//        public User User
//        {
//            get { return _user; }
//            set { _user = value; OnPropertyChanged(nameof(User)); }
//        }

//        public SignUpViewModel()
//        {
//            _user = new User();
//        }

//        // 회원가입
//        public async Task InsertUser(object parameter)
//        {
//            _user.Gender = User.Gender == "남성" ? "남성" : "여성";

//            // WPF 애플리케이션에서 현재 활성화된 메인 윈도우에서 이름이 "passwordBox"인 컨트롤을 찾기 위해 사용되는 메서드
//            var passwordBox = Application.Current.MainWindow.FindName("passwordBox") as PasswordBox;
//            var passwordCheckBox = Application.Current.MainWindow.FindName("passwordCheckBox") as PasswordBox;

//            string password = passwordBox.Password;
//            string passwordCheck = passwordCheckBox.Password;

//            User.Password = password;
//            User.PasswordCheck = passwordCheck;

//            _signUp = new SignUp();

//            var transport = UserSession.Instance.CurrentUser?.Transport;

//            Debug.WriteLine(
//  $"[VALID] name={Validator.Validator.ValidateName(User.Name)}, " +
//  $"nick={Validator.Validator.ValidateNickName(User.NickName)}, " +
//  $"pass={Validator.Validator.ValidatePassword(User.Password, User.PasswordCheck)}, " +
//  $"birth={Validator.Validator.ValidateBirthDate(User.BirthDate)}, " +
//  $"height={Validator.Validator.ValidateHeight(User.Height)}, " +
//  $"weight={Validator.Validator.ValidateWeight(User.Weight)}, " +
//  $"diet={Validator.Validator.ValidateDietGoal(User.DietGoal)}, " +
//  $"dupNick={_repo.BuplicateNickName(User.NickName)}, " +
//  $"emailCodeOk={SignUp.count}"
//);

//            // 유효성 검사
//            if (Validator.Validator.ValidateName(User.Name) && Validator.Validator.ValidateNickName(User.NickName)
//                && Validator.Validator.ValidatePassword(User.Password, User.PasswordCheck) && Validator.Validator.ValidateBirthDate(User.BirthDate) && Validator.Validator.ValidateHeight(User.Height)
//                && Validator.Validator.ValidateWeight(User.Weight) && Validator.Validator.ValidateDietGoal(User.DietGoal) && _repo.BuplicateNickName(User.NickName) && SignUp.count == 1)
//            {
//                var loginReq = new { name = User.Name, gender = User.Gender, nickname = User.NickName, email = User.Email, password = User.Password, birth = User.BirthDate, height = User.Height, weight = User.Weight, diet = User.DietGoal };
//                byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(loginReq));
//                await transport.SendFrameAsync((byte)MessageType.Sign_Up, payload);

//                var (respType, respPayload) = await transport.ReadFrameAsync();
//                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
//                var signUpRes = JsonSerializer.Deserialize<SignUpReply>(respPayload, opts);

//                if (signUpRes?.ok == true)
//                {
//                    MessageBox.Show("회원가입이 완료되었습니다.");
//                }
//            }
//            else
//            {
//                // 유효성 검사에 실패한 경우 처리
//                MessageBox.Show("회원가입에 실패하였습니다.");
//            }

//            if (SignUp.count == 0)
//            {
//                MessageBox.Show("인증 번호가 일치하지 않습니다.");
//            }
//        }
    }
}
