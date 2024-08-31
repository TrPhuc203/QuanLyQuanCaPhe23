using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuanLyQuanCaPhe23.Extensions;
using QuanLyQuanCaPhe23.Models;
using QuanLyQuanCaPhe23.OTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using System.Net.Mail;
using System.Net;

namespace QuanLyQuanCaPhe23.Controllers
{
    public class NguoiDungController : Controller
    {
        QUANLYCAPHEContext da = new QUANLYCAPHEContext();
        private readonly QUANLYCAPHEContext _context;
        private readonly IConfiguration _configuration;
        public readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<NguoiDungController> _logger;
        private string HoKh, TenKh, UserName, Pass, DiaChi, SoDienThoai, Gmail;

        // Constructor được cập nhật để inject QUANLYCAPHEContext
        public NguoiDungController(
            QUANLYCAPHEContext context,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<NguoiDungController> logger)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        // GET: NguoiDungController
        // Action để khởi động quá trình đăng nhập với Google
        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "NguoiDung");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // Action để xử lý phản hồi sau khi người dùng đăng nhập bằng Google
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = result.Principal.Identities.FirstOrDefault().Claims.Select(claim => new
            {
                claim.Type,
                claim.Value
            });

            // Lấy thông tin người dùng từ claim
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var phoneNumber = claims.FirstOrDefault(c => c.Type == "phone_number")?.Value; // Kiểm tra và yêu cầu quyền tương ứng
            var address = claims.FirstOrDefault(c => c.Type == "address")?.Value; // Kiểm tra và yêu cầu quyền tương ứng

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Error", "Home"); // Xử lý lỗi nếu không lấy được email
            }

            // Kiểm tra người dùng trong cơ sở dữ liệu
            KhachHang kh = da.KhachHangs.SingleOrDefault(k => k.Gmail.Equals(email));

            if (kh == null)
            {
                // Nếu người dùng chưa tồn tại, tạo tài khoản mới
                kh = new KhachHang
                {
                    HoKh = "",
                    TenKh = name,
                    UserName = email,
                    Pass = "default_password",
                    DiaChi = "", // Gán địa chỉ từ Google
                    SoDienThoai = "", // Gán số điện thoại từ Google
                    Gmail = email
                };
                da.KhachHangs.Add(kh);
                da.SaveChanges();
            }

            // Lưu thông tin người dùng vào session
            _httpContextAccessor.HttpContext.Session.SetInt32("MaKh", kh.MaKh);
            _httpContextAccessor.HttpContext.Session.SetString("UserName", kh.UserName);
            _httpContextAccessor.HttpContext.Session.SetString("HoKh", kh.HoKh);
            _httpContextAccessor.HttpContext.Session.SetString("TenKh", kh.TenKh);
            _httpContextAccessor.HttpContext.Session.SetString("Gmail", kh.Gmail);
            _httpContextAccessor.HttpContext.Session.SetString("FullName", $"{kh.HoKh} {kh.TenKh}");
            _httpContextAccessor.HttpContext.Session.SetString("DiaChi", kh.DiaChi); // Lưu địa chỉ vào session
            _httpContextAccessor.HttpContext.Session.SetString("SoDienThoai", kh.SoDienThoai); // Lưu số điện thoại vào session
            _httpContextAccessor.HttpContext.Session.SetString("IsLoggedInKH", "true");

            return RedirectToAction("ListCaPhe");
        }


        // Các action khác của NguoiDungController
        public ActionResult ListCaPhe(string? kw, string? page, string? size, int? gia)
        {
            string check = _httpContextAccessor.HttpContext.Session.GetString("UserName");
            if (check != null)
            {
                ViewData["SuccessMessage"] = _httpContextAccessor.HttpContext.Session.GetString("SuccessMessage");

                // Lấy tên người dùng từ session
                var nameH = _httpContextAccessor.HttpContext.Session.GetString("HoKh");
                var name = _httpContextAccessor.HttpContext.Session.GetString("TenKh");

                ViewData["FullName"] = (!string.IsNullOrEmpty(nameH) && !string.IsNullOrEmpty(name))
                    ? $"{nameH} {name}"
                    : "Khách hàng";

                // Xóa thông báo từ session
                _httpContextAccessor.HttpContext.Session.Remove("SuccessMessage");

                // Lấy danh sách sản phẩm từ cơ sở dữ liệu
                var ds = da.CaPhes.Include(c => c.Size).AsQueryable();

                // Áp dụng các điều kiện lọc
                if (!string.IsNullOrEmpty(kw))
                {
                    ds = ds.Where(s => s.Ten.Contains(kw)); // Sử dụng Contains thay vì IndexOf
                }
                if (!string.IsNullOrEmpty(size))
                {
                    if (int.TryParse(size, out int sizeid))
                    {
                        ds = ds.Where(s => s.SizeId == sizeid);
                    }
                }
                if (gia.HasValue)
                {
                    ds = ds.Where(s => s.Tien <= gia.Value);
                }

                // Lấy tổng số sản phẩm sau khi lọc
                var totalItems = ds.Count();

                int pageSize = 6; // Sửa giá trị pageSize thành 6 sản phẩm mỗi trang
                int pageNumber = 1; // Mặc định trang đầu tiên

                if (!string.IsNullOrEmpty(page) && int.TryParse(page, out int parsedPageNumber))
                {
                    pageNumber = parsedPageNumber;
                }

                // Áp dụng phân trang
                var pagedItems = ds.OrderBy(item => item.Id)
                                   .Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToList();

                // Truyền các tham số lọc vào ViewData để sử dụng trong phân trang
                ViewData["CurrentPage"] = pageNumber;
                ViewData["TotalPages"] = (int)Math.Ceiling(totalItems / (double)pageSize);
                ViewData["FilterKw"] = kw; // Truyền tham số lọc từ view
                ViewData["FilterSize"] = size;
                ViewData["FilterGia"] = gia;

                return View(pagedItems);
            }
            else
            {
                return RedirectToAction("DangNhap");
            }
        }

        // GET: NguoiDungController/Details/5
        public ActionResult Details(int id)
        {
            var p = da.CaPhes.Include(c => c.Size).FirstOrDefault(s => s.Id == id);
            return View(p);
        }
        public ActionResult DangKy()
        {
            //KhachHang kh = new KhachHang();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DangKyAsync(IFormCollection collection)
        {
            // Gán các giá tị người dùng nhập liệu cho các biến
            HoKh = collection["HoKh"];
            TenKh = collection["TenKh"];
            UserName = collection["UserName"];
            Pass = collection["Pass"];
            DiaChi = collection["DiaChi"];
            SoDienThoai = collection["SoDienThoai"];
            Gmail = collection["Gmail"];
            _httpContextAccessor.HttpContext.Session.SetString("HoKh", HoKh);
            _httpContextAccessor.HttpContext.Session.SetString("TenKh", TenKh);
            _httpContextAccessor.HttpContext.Session.SetString("UserName", UserName);
            _httpContextAccessor.HttpContext.Session.SetString("Pass", Pass);
            _httpContextAccessor.HttpContext.Session.SetString("DiaChi", DiaChi);
            _httpContextAccessor.HttpContext.Session.SetString("SoDienThoai", SoDienThoai);
            _httpContextAccessor.HttpContext.Session.SetString("Gmail", Gmail);

            if (String.IsNullOrEmpty(HoKh))
            {
                ViewData["Loil"] = "Họ khách hàng không được để trống";
            }
            else if (String.IsNullOrEmpty(TenKh))
            {
                ViewData["Loi2"] = "Tên khách hàng không được để trống";
            }
            else if (String.IsNullOrEmpty(UserName))
            {
                ViewData["Loi3"] = "Phải nhập tên đăng nhập";
            }
            else if (String.IsNullOrEmpty(Pass))
            {
                ViewData["Loi4"] = "Phải nhập mật khẩu";
            }
            else if (String.IsNullOrEmpty(DiaChi))
            {
                ViewData["Loi5"] = "Phải nhập địa chỉ";
            }
            else if (String.IsNullOrEmpty(SoDienThoai))
            {
                ViewData["Loi6"] = "Phải nhập số điện thoại";
            }
            else if (String.IsNullOrEmpty(Gmail))
            {
                ViewData["Loi7"] = "Phải nhập Gmail";
            }
            else
            {
                //////Gán giá trị cho đối tượng được tạo mới (kh)
                ////kh.HoKh = ho;
                ////kh.TenKh = ten;
                ////kh.Pass = pass;
                ////kh.DiaChi = diachi;
                ////kh.SoDienThoai = dienthoai;
                //string internationalPhoneNumber = "+84" + SoDienThoai.Substring(1);
                //Console.WriteLine(internationalPhoneNumber);
                //var otpService = new OtpService(_configuration, _httpContextAccessor);
                //otpService.SendOtp(internationalPhoneNumber); // Truyền số điện thoại cần gửi mã OTP vào hàm SendOtp

                //_httpContextAccessor.HttpContext.Session.SetString("UserPhoneNumber", SoDienThoai);
                KhachHang existingUser = da.KhachHangs.SingleOrDefault(k => k.Gmail.Equals(Gmail));
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Email đã được sử dụng.");
                    return this.DangKy();
                }
                HoKh = _httpContextAccessor.HttpContext.Session.GetString("HoKh");
                TenKh = _httpContextAccessor.HttpContext.Session.GetString("TenKh");
                UserName = _httpContextAccessor.HttpContext.Session.GetString("UserName");
                Pass = _httpContextAccessor.HttpContext.Session.GetString("Pass");
                DiaChi = _httpContextAccessor.HttpContext.Session.GetString("DiaChi");
                SoDienThoai = _httpContextAccessor.HttpContext.Session.GetString("SoDienThoai");
                Gmail = _httpContextAccessor.HttpContext.Session.GetString("Gmail");
                KhachHang kh = new KhachHang();
                kh.HoKh = HoKh;
                kh.TenKh = TenKh;
                kh.UserName = UserName;
                kh.Pass = Pass;
                kh.DiaChi = DiaChi;
                kh.SoDienThoai = SoDienThoai;
                kh.Gmail = Gmail;
                da.KhachHangs.Add(kh);
                da.SaveChanges();
                return RedirectToAction("Dangnhap");
            }
            return this.DangKy();
        }

        //public ActionResult VerifyOtp()
        //{
        //    return View();
        //}


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult VerifyOtp(string otp)
        //{
        //    var sessionOtp = _httpContextAccessor.HttpContext.Session.GetString("OTP");
        //    var userPhoneNumber = _httpContextAccessor.HttpContext.Session.GetString("UserPhoneNumber");
        //    Console.WriteLine("OTP khach hang nhap vao la" + otp);
        //    Console.WriteLine("OTP tao ra la : " + sessionOtp);
        //    if (sessionOtp == otp)
        //    {
        //        HoKh = _httpContextAccessor.HttpContext.Session.GetString("HoKh");
        //        TenKh = _httpContextAccessor.HttpContext.Session.GetString("TenKh");
        //        UserName = _httpContextAccessor.HttpContext.Session.GetString("UserName");
        //        Pass = _httpContextAccessor.HttpContext.Session.GetString("Pass");
        //        DiaChi = _httpContextAccessor.HttpContext.Session.GetString("DiaChi");
        //        SoDienThoai = _httpContextAccessor.HttpContext.Session.GetString("SoDienThoai");
        //        Console.WriteLine("OTP khach hang nhap vao la" + otp);
        //        Console.WriteLine("OTP tao ra la : " + sessionOtp);
        //        KhachHang kh = new KhachHang();
        //        kh.HoKh = HoKh;
        //        kh.TenKh = TenKh;
        //        kh.UserName = UserName;
        //        kh.Pass = Pass;
        //        kh.DiaChi = DiaChi;
        //        kh.SoDienThoai = SoDienThoai;
        //        da.KhachHangs.Add(kh);
        //        da.SaveChanges();
        //        _httpContextAccessor.HttpContext.Session.Remove("OTP");
        //        _httpContextAccessor.HttpContext.Session.Remove("UserPhoneNumber");

        //        // OTP verification success, proceed with further actions (e.g., login user)
        //        return RedirectToAction("DangNhap");
        //    }
        //    else
        //    {
        //        ModelState.AddModelError(string.Empty, "Invalid OTP. Please try again.");
        //        return View();
        //    }
        //}

        public ActionResult DangNhap()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(IFormCollection collection)
        {
            var uname = collection["UserName"];
            var pass = collection["Pass"];
            var idKH = collection["MaKh"];

            if (String.IsNullOrEmpty(uname))
            {
                ViewData["Loi1"] = "Phải nhập tên đăng nhập";
            }
            else if (String.IsNullOrEmpty(pass))
            {
                ViewData["Loi2"] = "Phải nhập mật khẩu";
            }
            else
            {
                KhachHang kh = da.KhachHangs.SingleOrDefault(k => k.UserName.Equals(uname.ToString()) && k.Pass.Equals(pass.ToString()));
                if (kh != null)
                {
                    _httpContextAccessor.HttpContext.Session.SetInt32("MaKh", kh.MaKh);
                    _httpContextAccessor.HttpContext.Session.SetString("UserName", kh.UserName);
                    _httpContextAccessor.HttpContext.Session.SetString("HoKh", kh.HoKh); // Lưu họ vào session
                    _httpContextAccessor.HttpContext.Session.SetString("TenKh", kh.TenKh); // Lưu tên vào session
                    string fullName = $"{kh.HoKh} {kh.TenKh}";
                    _httpContextAccessor.HttpContext.Session.SetString("FullName", fullName);
                    TempData["IsLoggedInKH"] = true;
                    _httpContextAccessor.HttpContext.Session.SetString("IsLoggedInKH", "true");
                    _httpContextAccessor.HttpContext.Session.SetString("SuccessMessage", "Chúc mừng đăng nhập thành công");

                    return RedirectToAction("ListCaphe");
                }
                else
                {

                    ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không đúng.");
                    return View();
                }
            }
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            // Kiểm tra sự tồn tại của người dùng
            var users = await _context.KhachHangs.Where(k => k.Gmail.Equals(email)).ToListAsync();
            if (users.Count == 0)
            {
                ViewData["Message"] = "Email không tồn tại trong hệ thống";
                return View();
            }
            else if (users.Count > 1)
            {
                // Xử lý tình huống có nhiều phần tử (có thể thông báo lỗi hoặc ghi log)
            }
            var user = users.First();
            // Sinh mã OTP
            var otpCode = OTPGenerator.GenerateOTP();
            user.ResetCode = otpCode; // Thêm thuộc tính ResetCode vào mô hình KhachHang
            _context.Update(user);
            await _context.SaveChangesAsync();

            // Cấu hình email
            var mailMessage = new MailMessage
            {
                From = new MailAddress("trongphuc1321@gmail.com"),
                Subject = "Mã khôi phục mật khẩu",
                Body = $"Mã OTP của bạn là: {otpCode}",
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            // Gửi email
            try
            {
                using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtpClient.Credentials = new NetworkCredential("trongphuc1321@gmail.com", "wpwz qkyu pgju oaug");
                    smtpClient.EnableSsl = true;
                    await smtpClient.SendMailAsync(mailMessage);
                }

                // Thông báo thành công
                ViewData["Message"] = "Mã OTP đã được gửi đến email của bạn";
            }
            catch (SmtpException ex)
            {
                // Xử lý lỗi gửi email
                ViewData["Message"] = $"Lỗi khi gửi email: {ex.Message}";
            }
            catch (Exception ex)
            {
                // Xử lý lỗi chung
                ViewData["Message"] = $"Đã xảy ra lỗi: {ex.Message}";
            }

            return RedirectToAction("ConfirmResetCode", new { email });
        }

        [HttpGet]
        public IActionResult ConfirmResetCode(string email)
        {
            // Kiểm tra email có hợp lệ không
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword"); // Hoặc trang khác nếu không có email
            }

            // Tạo mô hình với email được truyền vào
            var model = new ConfirmResetCodeViewModel
            {
                Email = email
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmResetCode(ConfirmResetCodeViewModel model)
        {
            var user = await _context.KhachHangs.SingleOrDefaultAsync(k => k.Gmail.Equals(model.Email));
            if (user == null || user.ResetCode != model.ResetCode)
            {
                ViewData["ErrorMessage"] = "Mã xác nhận không đúng.";
                return View(model);
            }

            // Đặt mật khẩu mới cho người dùng
            user.Pass = model.NewPassword;
            user.ResetCode = null; // Xóa mã xác nhận sau khi sử dụng
            _context.Update(user);
            await _context.SaveChangesAsync();

            ViewData["SuccessMessage"] = "Mật khẩu đã được thay đổi thành công.";
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            ViewData["Email"] = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string newPassword)
        {
            var user = await _context.KhachHangs.SingleOrDefaultAsync(k => k.Gmail.Equals(email));
            if (user == null)
            {
                ViewData["Message"] = "Email không tồn tại trong hệ thống";
                return View();
            }

            user.Pass = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetCode = null; // Xóa mã OTP sau khi thay đổi mật khẩu
            _context.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("DangNhap");
        }

        public ActionResult DangXuat()
        {
            // Xóa session khi người dùng đăng xuất
            _httpContextAccessor.HttpContext.Session.Clear();
            return RedirectToAction("TrangChu", "Home");
        }
        public List<GioHang> GetListCarts()
        {
            List<GioHang> carts = _httpContextAccessor.HttpContext.Session.GetObjectFromJson<List<GioHang>>("GioHang");

            //Chưa có thì tạo mới giỏ hàng trống
            if (carts == null)
            {
                carts = new List<GioHang>();
            }
            //Có rồi thì lấy các sp trả về
            return carts;
        }
        public ActionResult LichSuDonHang()
        {
            string userName = _httpContextAccessor.HttpContext.Session.GetString("UserName");
            int? maKh = _httpContextAccessor.HttpContext.Session.GetInt32("MaKh");
            string isLoggedIn = _httpContextAccessor.HttpContext.Session.GetString("IsLoggedInKH");

            if (!string.IsNullOrEmpty(userName) && maKh.HasValue && isLoggedIn == "true")
            {
                var dsDonHang = da.DonHangs
                    .Include(dh => dh.ChiTietDonHangs)
                    .ThenInclude(ctdh => ctdh.CaPhe)
                    .Where(dh => dh.KhachHangId == maKh.Value)
                    .OrderByDescending(dh => dh.NgayTao)
                    .ToList();
                if (!dsDonHang.Any())
                {
                    ViewBag.Message = "Bạn chưa có đơn hàng nào.";
                }
                return View(dsDonHang);
            }
            else
            {
                return RedirectToAction("DangNhap");
            }
        }

    }
}
