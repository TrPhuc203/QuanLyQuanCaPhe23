using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QuanLyQuanCaPhe23.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuanLyQuanCaPhe23.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet;
using System.IO;
using CloudinaryDotNet.Actions;
using QuanLyQuanCaPhe23.VNPAY;
using Microsoft.Extensions.Configuration;
using QuanLyQuanCaPhe23.PAYPAL;
using Microsoft.Extensions.Logging;

namespace QuanLyQuanCaPhe23.Controllers
{
    public class GioHangController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IVnPayService _vnPayService;
        private readonly IPayPalService _payPalService;
        private readonly ILogger<GioHangController> _logger;
        public GioHangController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IVnPayService vnPayService, IPayPalService payPalService, ILogger<GioHangController> logger)
        {
            _vnPayService = vnPayService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _payPalService = payPalService;
            _logger = logger;
        }


        QUANLYCAPHEContext da = new QUANLYCAPHEContext();

        public readonly IHttpContextAccessor _httpContextAccessor;

        public IActionResult AddToCart(int id)
        {
            List<GioHang> gh = GetListCarts();
            var c = gh.Find(s => s.Id == id);
            if (c != null)
            {
                c.SoLuong++;
            }
            else
            {
                c = new GioHang(id);
                gh.Add(c);
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("GioHang", gh);
            return RedirectToAction("ListCarts");

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
        // GET: CaPheController/Delete/5
        public ActionResult Delete(int id)
        {
            TempData["IsLoggedIn"] = true;
            var p = da.CaPhes.Include(c => c.Size).FirstOrDefault(s => s.Id == id);
            return View(p);
        }

        // POST: CaPheController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                TempData["IsLoggedIn"] = true;
                var p = da.CaPhes.Include(c => c.Size).FirstOrDefault(s => s.Id == id);
                // Lấy danh sách giỏ hàng từ session
                List<GioHang> gh = GetListCarts();

                // Xóa sản phẩm khỏi giỏ hàng
                var updatedCart = gh.Where(item => item.Id != id).ToList();

                // Cập nhật lại giỏ hàng trong session
                HttpContext.Session.SetObjectAsJson("GioHang", updatedCart);

                // Chuyển hướng người dùng đến trang giỏ hàng
                return RedirectToAction("ListCarts");
            }
            catch
            {
                return View();
            }
        }
        public IActionResult ListCarts()
        {
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
            List<GioHang> gh = GetListCarts();
            ViewBag.CountProduct = gh.Sum(s => s.SoLuong);
            ViewBag.Total = gh.Sum(s => s.TongTien);
            var tongtien = gh.Sum(s => s.TongTien);
            _httpContextAccessor.HttpContext.Session.SetString("TongTien", tongtien.ToString());

            return View(gh);
        }
        public ActionResult Order()
        {
            //Tạo mới 1 đơn hàng lưu trong DonHangs: chỉ thêm ngày đặt hàng(NgayTao)
            DonHang o = new DonHang();
            o.NgayTao = DateTime.Now;
            ViewData["SuccessMessage"] = _httpContextAccessor.HttpContext.Session.GetString("SuccessMessage");
            var MaKH = _httpContextAccessor.HttpContext.Session.GetInt32("MaKh");
            o.KhachHangId = MaKH;
            da.DonHangs.Add(o);
            da.SaveChanges();
            // Có bao nhiêu sp tạo mới bấy nhiêu dòng trong
            // Lấy dssp trong giỏ hàng
            List<GioHang> gh = GetListCarts();
            //Duyệt giỏ hàng thêm vào db
            foreach (var item in gh)
            {
                //Tạo mới ChiTietDonHang
                ChiTietDonHang odd = new ChiTietDonHang();
                // Thiết lập các thuộc tính
                odd.DonHangId = o.Id;
                odd.CaPheId = item.Id;
                odd.SoLuong = item.SoLuong;
                odd.Tien = (item.Tien);
                odd.KhuyenMaiId = 1;
                //Thêm
                da.ChiTietDonHangs.Add(odd);
            }
            da.SaveChanges();
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("GioHang", gh);
            // Làm rỗng giỏ hàng
            HttpContext.Session.Remove("GioHang");
            //Hiển thị chitietdonhang
            //return RedirectToAction("ThanhToanPayPal", new { orID = o.Id });
            return RedirectToAction("ChiTietDonHangList", new { orID = o.Id });
        }
        public ActionResult ChiTietDonHangList(int orID)
        {
            var chitietdonghang = da.ChiTietDonHangs
                                        .Include(ct => ct.CaPhe) // Nạp thông tin về CaPhe
                                            .ThenInclude(cp => cp.Size) // Nạp thông tin về Size của CaPhe
                                        .Include(ct => ct.KhuyenMai) // Nạp thông tin về KhuyenMai
                                        .Where(s => s.DonHangId == orID)
                                        .ToList();
            var tongTienString = _httpContextAccessor.HttpContext.Session.GetString("TongTien");
            var viewModel = new DonHangViewModel
            {
                DonHangId = orID,
                KhuyenMaiId = chitietdonghang.FirstOrDefault()?.KhuyenMai,
                SanPhams = chitietdonghang.Select(ct => new SanPhamViewModel
                {

                    CaPhe = ct.CaPhe,
                    SoLuong = ct.SoLuong,
                    Tien = ct.Tien
                }).ToList()
            };
            ViewBag.TongTien = tongTienString;

            return View(viewModel);
        }
        public IActionResult ThanhToanOnline(int orID)
        {
            var chitietdonghang = da.ChiTietDonHangs
                                            .Include(ct => ct.CaPhe) // Nạp thông tin về CaPhe
                                                .ThenInclude(cp => cp.Size) // Nạp thông tin về Size của CaPhe
                                            .Include(ct => ct.KhuyenMai) // Nạp thông tin về KhuyenMai
                                            .Where(s => s.DonHangId == orID)
                                            .ToList();
            var tongTienString = _httpContextAccessor.HttpContext.Session.GetString("TongTien");
            var model = new DonHangViewModel
            {
                DonHangId = orID,
                KhuyenMaiId = chitietdonghang.FirstOrDefault()?.KhuyenMai,
                SanPhams = chitietdonghang.Select(ct => new SanPhamViewModel
                {

                    CaPhe = ct.CaPhe,
                    SoLuong = ct.SoLuong,
                    Tien = ct.Tien
                }).ToList()
            };
            ViewBag.TongTien = tongTienString;

            var vnPayModel = new VnPaymentRequestModel
            {
                Amout = 100000,
                CreatedDate = DateTime.Now,
                Description = "Thanh toán online",
                FullName = "Duong Van Khanh",
                OrderId = orID


            };

            return Redirect(_vnPayService.CreatePaymentURL(HttpContext, vnPayModel));

        }

        //public IActionResult PaymentCalLBack()
        //{
        //    var response = _vnPayService.PaymentExecute(Request.Query);
        //    if (response == null || response.VnPayResponseCode != "00")
        //    {
        //        TempData["Message"] = $"Lỗi thanh toán VNPAY { response.VnPayResponseCode} ";
        //    }
        //    //Lưu đơn hàng vào db
        //    TempData["Message"] = "Thanh toán VNPAY thành công";
        //    return RedirectToAction("PaymentSuccess");
        //}
        //public IActionResult PaymentSuccess()
        //{
        //    return View();
        //}
        //public IActionResult PaymentFail()
        //{
        //    return View();
        //}


        public async Task<IActionResult> ThanhToanPayPal(int orID)
        {

            var tongTienString = _httpContextAccessor.HttpContext.Session.GetString("TongTien");
            double tongTien = Double.Parse(tongTienString);

            ViewBag.TongTien = tongTienString;
            var nameH = _httpContextAccessor.HttpContext.Session.GetString("HoKh");
            var name = _httpContextAccessor.HttpContext.Session.GetString("TenKh");
            var fullName = nameH + name;
            PaymentInformationModel model = new PaymentInformationModel
            {
                OrderType = "Thanh toan tien caphe",
                Amount = tongTien,
                OrderDescription = String.Format("Hoa don thanh toan online"),
                Name = fullName
            };
            var url = await _payPalService.CreatePaymentUrl(model);
            return Redirect(url);
        }

        public async Task<IActionResult> PaymentCallback()
        {
            var paymentInfo = _payPalService.PaymentExecute(Request.Query);

            // Trích xuất các giá trị từ kết quả trả về
            string payment_method = paymentInfo.PaymentMethod;
            string success = paymentInfo.Success.ToString();
            string orderId = paymentInfo.OrderId;
            string paymentId = paymentInfo.PaymentId;
            string token = Request.Query["token"];
            string payerId = paymentInfo.PayerId;
            string url = "https://localhost:44336/NguoiDung/PaymentCallback";

            _logger.LogInformation("PaymentCallback called with success: {successString}", success);

            if (paymentInfo.Success && !string.IsNullOrEmpty(paymentId) && !string.IsNullOrEmpty(payerId))
            {
                var executedPayment = await _payPalService.ExecutePayment(paymentId, payerId);
                if (executedPayment != null && executedPayment.State == "approved")
                {
                    _logger.LogInformation("Payment was successful.");
                    DonHang o = new DonHang();
                    ViewData["SuccessMessage"] = _httpContextAccessor.HttpContext.Session.GetString("SuccessMessage");
                    var MaKH = _httpContextAccessor.HttpContext.Session.GetInt32("MaKh");
                    o.KhachHangId = MaKH;
                    o.NgayTao = DateTime.Now;
                    o.PayPalKey = url + "&" + "payment_method=" + payment_method + "&" + "success=" + success + "&" + "order_id=" + orderId + "&" + "paymentId=" + paymentId + "&" + "token=" + token + "&" + "PayerID=" + payerId;
                    da.DonHangs.Add(o);
                    da.SaveChanges();
                    int id = o.Id;

                    // Thêm
                    List<GioHang> gh = GetListCarts();
                    // Duyệt giỏ hàng thêm vào db
                    foreach (var item in gh)
                    {
                        // Tạo mới ChiTietDonHang
                        ChiTietDonHang odd = new ChiTietDonHang();
                        // Thiết lập các thuộc tính
                        odd.DonHangId = o.Id;
                        odd.CaPheId = item.Id;
                        odd.SoLuong = item.SoLuong;
                        odd.Tien = item.Tien;
                        odd.KhuyenMaiId = 1;
                        da.ChiTietDonHangs.Add(odd);
                    }
                    da.SaveChanges();
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("GioHang", gh);
                    // Làm rỗng giỏ hàng
                    HttpContext.Session.Remove("GioHang");
                    ViewBag.OrderId = id;
                    ViewBag.TongTien = _httpContextAccessor.HttpContext.Session.GetString("TongTien");
                    return View("PaymentSuccess");
                }
                else
                {
                    _logger.LogWarning("Payment failed or was not approved.");
                    return View("PaymentFailure");
                }
            }
            else
            {
                _logger.LogWarning("Payment failed or success parameter is missing/invalid.");
                return View("PaymentFailure");
            }
        }


        public IActionResult PaymentSuccess()
        {
            return View();
        }

        public IActionResult PaymentFailure()
        {
            return View();
        }










    }   
}
