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
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace QuanLyQuanCaPhe23.Controllers
{
    public class NguoiDungController : Controller
    {
        QUANLYCAPHEContext da = new QUANLYCAPHEContext();
        private readonly IConfiguration _configuration;
        public readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<NguoiDungController> _logger;
        private string HoKh, TenKh, UserName, Pass, DiaChi, SoDienThoai, Gmail;
        public NguoiDungController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<NguoiDungController> logger)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        // GET: NguoiDungController
        public ActionResult ListCaPhe(string ? kw, string ? page,string ? size, int ? gia)
        {
            string check = _httpContextAccessor.HttpContext.Session.GetString("UserName");
            if (check != null)
            {
                ViewData["SuccessMessage"] = _httpContextAccessor.HttpContext.Session.GetString("SuccessMessage");

                // Lấy tên người dùng từ session
                var nameH = _httpContextAccessor.HttpContext.Session.GetString("HoKh");
                var name = _httpContextAccessor.HttpContext.Session.GetString("TenKh");

                if (!string.IsNullOrEmpty(nameH) && !string.IsNullOrEmpty(name))
                {
                    ViewData["FullName"] = $"{nameH} {name}";
                }
                else
                {
                    ViewData["FullName"] = "Khách hàng";
                }

                // Xóa thông báo từ session
                var ds = da.CaPhes.Include(c => c.Size).ToList();
                _httpContextAccessor.HttpContext.Session.Remove("SuccessMessage");
                if (!string.IsNullOrEmpty(kw))
                {
                    ds = ds.Where(s => s.Ten.Contains(kw)).ToList();
                }
                if (!string.IsNullOrEmpty(size))
                {
                    int sizeid = int.Parse(size);
                    ds = ds.Where(s => s.SizeId == sizeid).ToList();
                }
                if (gia != null)
                {
                    ds = ds.Where(s => s.Tien <= (decimal)gia).ToList();
                }
                int pageSize = 4;
                if (!string.IsNullOrEmpty(page))
                {
                    int pageNumber = int.Parse(page);

                    ds = da.CaPhes
                              .OrderBy(item => item.Id)
                              .Skip((pageNumber - 1) * pageSize)//Lấy từ vị trí
                              .Take(pageSize)//Lấy bao nhiêu
                              .ToList();
                }

                return View(ds);
            }
            else
                return RedirectToAction("DangNhap");



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
       
    }
}
