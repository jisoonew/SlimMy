using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        public static Random randomNum = new Random(); // 전역변수
        public static int checkNum = randomNum.Next(10000000, 99999999);

        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        private Repo _repo;

        // 이메일 인증 발송
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string SystemMailId = "pjsu200@naver.com";
            string SystemMailPwd = "qkrwltn848501!";
            string recipientEmail = email.Text;
            _repo = new Repo(_connstring);

            // 이메일 유효성 검사
            if (Validator.Validator.ValidateEmail(recipientEmail) && _repo.DuplicateEmail(recipientEmail))
            {
                MailMessage mail = new MailMessage();

                // 받는 사람 이메일
                mail.To.Add(email.Text);

                // 보내는 사람 이메일
                mail.From = new MailAddress(SystemMailId);

                // 이메일 제목
                mail.Subject = "SlimMy 회원가입 본인인증";

                // 이메일 내용
                mail.Body = "SlimMy 이메일 인증 번호 : " + checkNum.ToString();

                mail.IsBodyHtml = true;
                mail.Priority = MailPriority.High;
                mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                mail.SubjectEncoding = Encoding.UTF8;
                mail.BodyEncoding = Encoding.UTF8;

                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.naver.com";
                smtp.Port = 587; // 또는 465
                smtp.Timeout = 10000;
                smtp.UseDefaultCredentials = false; // 네이버는 기본 자격 증명을 사용하지 않음
                smtp.EnableSsl = true; // 또는 false에 따라 사용하는 포트에 따라 설정
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Credentials = new System.Net.NetworkCredential(SystemMailId, SystemMailPwd);

                try
                {
                    smtp.Send(mail);
                    smtp.Dispose();

                    MessageBox.Show("인증번호 전송완료", "전송 완료");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
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
