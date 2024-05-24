﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SlimMy.Validator
{
    // 유효성 검사 진행
    public static class Validator
    {
        // 이름
        public static bool ValidateName(string name)
        {
            // 이름이 2자 이상 5자 이하인지 확인
            if (name.Length < 2 || name.Length > 5)
            {
                MessageBox.Show("이름은 2자 이상 5자 이하만 가능합니다.");
                return false;
            }

            // 한글로만 이루어져 있는지 확인
            if (!Regex.IsMatch(name, @"^[가-힣]+$"))
            {
                MessageBox.Show("한글로만 입력해주세요.");
                return false;
            }

            // 모든 조건을 통과하면 true를 반환
            return true;
        }

        // 닉네임
        public static bool ValidateNickName(string nickName)
        {
            // 닉네임은 최대 20자
            if (nickName.Length > 20)
            {
                MessageBox.Show("닉네임은 최대 20자까지 가능합니다.");
                return false;
            }

            // 모든 조건을 통과하면 true를 반환
            return true;
        }

        // 이메일 
        public static bool ValidateEmail(string email)
        {
            // 닉네임은 최대 20자
            if (!Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                MessageBox.Show("이메일 형식이 아닙니다.");
                return false;
            }

            // 모든 조건을 통과하면 true를 반환
            return true;
        }

        // 비밀번호
        public static bool ValidatePassword(string password)
        {
            // 비밀번호는 최소 8자 최대 100자이면서, 숫자와 특수문자는 최소 1자 이상 포함
            if (!Regex.IsMatch(password, @"^(?=.*[0-9])(?=.*[!@#$%^&*()-_=+{};:,<.>]).{8,100}$"))
            {
                MessageBox.Show("비밀번호는 최소 8자 최대 100자이면서, 숫자와 특수문자는 최소 1자 이상 포함어야 합니다.");
                return false;
            }

            // 모든 조건을 통과하면 true를 반환
            return true;
        }

        // 생년월일
        public static bool ValidateBirthDate(DateTime birthDate)
        {
            DateTime minDate = new DateTime(1930, 1, 1);
            DateTime maxDate = DateTime.Now;

            // 생년월일이 범위 내에 있는지 확인
            if (birthDate < minDate || birthDate > maxDate)
            {
                MessageBox.Show("1930년 이후부터 현재까지의 날짜를 입력하세요.");
                return false;
            }

            // 모든 조건을 통과하면 true를 반환
            return true;
        }
    }
}
