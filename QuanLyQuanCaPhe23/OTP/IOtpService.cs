using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyQuanCaPhe23.OTP
{
    public interface IOtpService
    {
        // Định nghĩa các phương thức mà OtpService sẽ triển khai
        string GenerateOtp();
        void SendOtp(string toPhoneNumber);
    }
}

