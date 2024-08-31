using System;

namespace QuanLyQuanCaPhe23.OTP
{
    public static class OTPGenerator
    {
        private static Random _random = new Random();

        public static string GenerateOTP()
        {
            // Tạo mã OTP 6 chữ số
            return _random.Next(100000, 999999).ToString();
        }
    }
}