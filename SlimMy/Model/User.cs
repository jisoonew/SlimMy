using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Messaging;
using SlimMy.Validator;
using SlimMy.ViewModel;

namespace SlimMy.Model
{
    public class User : INotifyPropertyChanged
    {

        private string name; // 이름
        private string gender; // 성별
        private string nickName; // 닉네임
        private string email; // 이메일
        private string password; // 비밀번호
        private string passwordCheck;
        private DateTime birthDate; // 생년월일
        private int height; // 키
        private int weight; // 몸무게
        private string dietGoal; // 다이어트 목표
        private string ipNum; // 아이피

        public string IpNum
        {
            get { return ipNum; }
            set
            {
                ipNum = value;
                OnPropertyChanged(nameof(ipNum));
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Gender
        {
            get { return gender; }
            set { gender = value; OnPropertyChanged(nameof(gender)); }
        }

        public string NickName
        {
            get { return nickName; }
            set { nickName = value; OnPropertyChanged(nameof(nickName)); }
        }

        public string Email
        {
            get { return email; }
            set { email = value; OnPropertyChanged(nameof(email)); }
        }

        public string Password
        {
            get { return password; }
            set { password = value; OnPropertyChanged(nameof(password)); }
        }

        public string PasswordCheck
        {
            get { return passwordCheck; }
            set { passwordCheck = value; OnPropertyChanged(nameof(passwordCheck)); }
        }

        public DateTime BirthDate
        {
            get { return birthDate; }
            set { birthDate = value; }
        }

        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        public int Weight
        {
            get { return weight; }
            set { weight = value; }
        }

        public string DietGoal
        {
            get { return dietGoal; }
            set { dietGoal = value; OnPropertyChanged(nameof(dietGoal)); }
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
