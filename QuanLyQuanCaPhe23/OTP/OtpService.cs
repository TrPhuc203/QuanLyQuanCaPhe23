using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Verify.V2.Service;
using Twilio.Types;

namespace QuanLyQuanCaPhe23.OTP
{
    public class OtpService : IOtpService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string generatedOtp;


        public OtpService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;

            // Khởi tạo TwilioClient với các thông tin xác thực từ configuration
            TwilioClient.Init(
                _configuration["Twilio:AccountSid"],
                _configuration["Twilio:AuthToken"]
            );
        }
        public string GenerateOtpOnce()
        {
            if (string.IsNullOrEmpty(generatedOtp))
            {
                generatedOtp = GenerateOtp();
            }
            return generatedOtp;
        }

        public string GenerateOtp()
        {
            Random random = new Random();
            const string chars = "0123456789";
            char[] otp = new char[6];
            for (int i = 0; i < 6; i++)
            {
                otp[i] = chars[random.Next(chars.Length)];
            }
            return new string(otp);
        }

      public void SendOtp(string toPhoneNumber)
        {
            try
            {
                var accountSid = "AC25f201e0cf666301287dc2d58a016481";
                var authToken = "634e6a9885ca1dabe037cb365e7b65a2";
                TwilioClient.Init(accountSid, authToken);

                // Tạo OTP bằng Verify Service
                var verification = VerificationResource.Create(
                    to: toPhoneNumber,
                    channel: "sms", // Bạn có thể chọn 'sms' hoặc 'call'
                    pathServiceSid: "VAedd5d961eb13231650ea9593f97af62b"
                );

                Console.WriteLine(verification.Sid);

                // Đợi một chút để OTP được tạo và gửi
                System.Threading.Thread.Sleep(5000); // Nghỉ 5 giây

                // Xác nhận OTP đã gửi thành công bằng Verify Service
                var verificationCheck = VerificationCheckResource.Create(
                    to: toPhoneNumber,
                    code: GenerateOtpOnce(),
                    pathServiceSid: "VAedd5d961eb13231650ea9593f97af62b"
                );

                // Gửi OTP qua dịch vụ Message Service
                var messageOptions = new CreateMessageOptions(
                    new PhoneNumber(toPhoneNumber));
                messageOptions.From = new PhoneNumber("+13209952500");
                messageOptions.Body = "Your verification code is: " + GenerateOtpOnce();
                messageOptions.MessagingServiceSid = "MG5695ebcac025e7203ed53ceda65137f9";
                var message = MessageResource.Create(messageOptions);
                Console.WriteLine(message.Body);
                Console.WriteLine("OTP dc tao ra la: " + GenerateOtpOnce());
                // Lưu mã OTP vào session sau khi gửi thành công
                _httpContextAccessor.HttpContext.Session.SetString("OTP", GenerateOtpOnce());






                Console.WriteLine($"Đã gửi tin nhắn có SID: {message.Sid}");


            }
            catch (Exception ex)
            {
                // Xử lý lỗi khi gửi OTP không thành công
                // Ví dụ: ghi log, thông báo cho người dùng, ...
                // Log lỗi
                throw; // Ném lại ngoại lệ để controller có thể xử lý
            }
        }


    }
}
