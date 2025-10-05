using SlimMy.Model;
using SlimMy.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Topshelf.Runtime.Windows;

namespace SlimMy.View
{
    /// <summary>
    /// SignUp.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SignUp : Window
    {
        public SignUp()
        {
            InitializeComponent();

            Loaded += async (_, __) =>
            {
                try
                {
                    INetworkTransport transport = UserSession.Instance.CurrentUser?.Transport;
                    bool createdTemp = false;

                    if (transport == null)
                    {
                        var t = new TlsTcpTransport();

                        // 서버 인증서가 CN/SAN=localhost 라면 반드시 "localhost"로 접속
                        await t.ConnectAsync("localhost", 9999);
                        if (UserSession.Instance.CurrentUser == null)
                            UserSession.Instance.CurrentUser = new User();
                        UserSession.Instance.CurrentUser.Transport = t;

                        transport = t;
                    }
                    Debug.WriteLine("[SignUp] Connected.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SignUp] Connect failed: {ex}");
                    MessageBox.Show("서버 연결 실패");
                }
            };
        }

        public static Random randomNum = new Random(); // 전역변수
        public static int checkNum = randomNum.Next(10000000, 99999999);

        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        private Repo _repo;

        // 이메일 인증 발송
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string from = "pjsu200@naver.com";
            string appPwd = "4M95R3EM6RHM";
            var to = (email.Text ?? "").Trim();

            _repo = new Repo(_connstring);

            if (!(Validator.Validator.ValidateEmail(to) && _repo.DuplicateEmail(to)))
            {
                MessageBox.Show("이메일 형식이 올바르지 않거나 이미 사용 중입니다.");
                return;
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using var msg = new MailMessage();
            msg.From = new MailAddress(from);
            msg.To.Add(to);
            msg.Subject = "SlimMy 회원가입 본인인증";
            msg.Body = $"SlimMy 이메일 인증 번호 : {SignUp.checkNum}";
            msg.SubjectEncoding = Encoding.UTF8;
            msg.BodyEncoding = Encoding.UTF8;

            using var smtp = new SmtpClient("smtp.naver.com", 587)
            {
                EnableSsl = true,                   // 587 = STARTTLS
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(from, appPwd),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 20000
            };

            try
            {
                smtp.Send(msg);
                MessageBox.Show("인증번호 전송완료", "전송 완료");
            }
            catch (SmtpException ex)
            {
                // 서버가 왜 거절했는지 정확히 보여줌
                MessageBox.Show($"SMTP 오류: {ex.StatusCode}\n{ex.Message}\n{ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public static int count;

        // 이메일 인증 유효성 검사
        public void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (txt_emailchecknum.Text == checkNum.ToString())
            {
                MessageBox.Show("이메일 인증이 완료되었습니다.", "인증 성공");
                emailCheck.Visibility = Visibility.Visible;
                emailNoCheck.Visibility = Visibility.Collapsed;
                passwordTextBox.Margin = new Thickness { Top = 43 };

                count = 1;
            }
            else
            {
                MessageBox.Show("인증 번호가 다릅니다.", "인증 실패");
                emailNoCheck.Visibility = Visibility.Visible;
                emailCheck.Visibility = Visibility.Collapsed;
                passwordTextBox.Margin = new Thickness { Top = 43 };

                count = 0;
            }
        }

    }
}
