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
using QuanLyQuanCaPhe23.ViewModels;
using System.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using Microsoft.AspNetCore.SignalR;
using System.Net.Mail;
using System.Net;
using System.Text;
using TimeZoneConverter;

namespace QuanLyQuanCaPhe23.Controllers
{
    public class GioHangController : Controller
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly IVnPayService _vnPayService;
        private readonly IPayPalService _payPalService;
        private readonly ILogger<GioHangController> _logger;
        public GioHangController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IVnPayService vnPayService, IPayPalService payPalService, ILogger<GioHangController> logger, IHubContext<NotificationHub> hubContext)
        {
            _vnPayService = vnPayService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _payPalService = payPalService;
            _logger = logger;
            _hubContext = hubContext;
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
        public IActionResult IncreaseQuantity(int id)
        {
            List<GioHang> gh = GetListCarts();
            var c = gh.Find(s => s.Id == id);
            if (c != null)
            {
                if (c.Tien.HasValue) // Ensure Tien is not null
                {
                    c.SoLuong++;
                    // TongTien will be recalculated automatically
                }
                else
                {
                    // Log or handle the case where Tien is null
                    _logger.LogWarning($"Price (Tien) is null for item ID {id}");
                }
            }
            else
            {
                _logger.LogWarning($"Item with ID {id} not found in cart.");
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("GioHang", gh);
            return RedirectToAction("ListCarts");
        }

        public IActionResult DecreaseQuantity(int id)
        {
            List<GioHang> gh = GetListCarts();
            var c = gh.Find(s => s.Id == id);
            if (c != null)
            {
                if (c.Tien.HasValue) // Ensure Tien is not null
                {
                    if (c.SoLuong > 1)
                    {
                        c.SoLuong--;
                        // TongTien will be recalculated automatically
                    }
                    else
                    {
                        gh.Remove(c); // Remove item if quantity is 1 and decreased
                    }
                }
                else
                {
                    // Log or handle the case where Tien is null
                    _logger.LogWarning($"Price (Tien) is null for item ID {id}");
                }
            }
            else
            {
                _logger.LogWarning($"Item with ID {id} not found in cart.");
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
        public async Task<ActionResult> OrderAsync()
        {
            //Tạo mới 1 đơn hàng lưu trong DonHangs: chỉ thêm ngày đặt hàng(NgayTao)
            DonHang o = new DonHang();
            o.NgayTao = DateTime.Now;
            ViewData["SuccessMessage"] = _httpContextAccessor.HttpContext.Session.GetString("SuccessMessage");
            var MaKH = _httpContextAccessor.HttpContext.Session.GetInt32("MaKh");
            o.KhachHangId = MaKH;
            da.DonHangs.Add(o);
            da.SaveChanges();
            var hubContext = (IHubContext<NotificationHub>)HttpContext.RequestServices.GetService(typeof(IHubContext<NotificationHub>));
            await hubContext.Clients.All.SendAsync("ReceiveNotification", o.Id);

            // Lấy thông tin người mua
           
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
            var productDetails = new StringBuilder();
            foreach (var item in gh)
            {
                productDetails.AppendLine($"Sản phẩm: {item.Ten}, Số lượng: {item.SoLuong}. ");
            }
            var khachHang = da.KhachHangs.Find(MaKH);
            string userName = khachHang.HoKh + " " + khachHang.TenKh;

            string adminEmail = "trongphucnguyen14703@gmail.com";
            var mailMessage = new MailMessage
            {
                From = new MailAddress("trongphuc1321@gmail.com"),
                Subject = "Thông báo thanh toán đơn hàng",
                Body = $"Đơn hàng #{o.Id} đã được thanh toán thành công." +
               $"<br/>Người mua: {userName}" +
               $"<br/><br/>Chi tiết sản phẩm:<br/>{productDetails.ToString()}",
                IsBodyHtml = true
            };
            mailMessage.To.Add(adminEmail); // Gửi mã OTP đến email người dùng đã đăng ký

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
                ViewData["Message"] = "Đơn hàng thanh toán đã gửi đến gmail của Admin";
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

            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("GioHang", gh);
            // Làm rỗng giỏ hàng
            HttpContext.Session.Remove("GioHang");
            //Hiển thị chitietdonhang
            //return RedirectToAction("ThanhToanPayPal", new { orID = o.Id });
            return RedirectToAction("ChiTietDonHangList", new { orID = o.Id });
        }
        public ActionResult ChiTietDonHangList(int orID)
        {
            // Lấy chi tiết đơn hàng từ cơ sở dữ liệu
            var chitietdonghang = da.ChiTietDonHangs
                                        .Include(ct => ct.CaPhe) // Nạp thông tin về CaPhe
                                            .ThenInclude(cp => cp.Size) // Nạp thông tin về Size của CaPhe
                                        .Include(ct => ct.KhuyenMai) // Nạp thông tin về KhuyenMai
                                        .Where(s => s.DonHangId == orID)
                                        .ToList();

            // Tính tổng tiền
            var tongTien = chitietdonghang.Sum(ct => ct.SoLuong * ct.Tien);

            // Tạo view model
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

            // Gán tổng tiền cho ViewBag
            ViewBag.TongTien = tongTien;

            return View(viewModel);
        }

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

                    var notificationMessage = "Đơn hàng #" + paymentInfo.OrderId + " đã được thanh toán thành công.";
                    var hubContext = (IHubContext<NotificationHub>)HttpContext.RequestServices.GetService(typeof(IHubContext<NotificationHub>));
                    await hubContext.Clients.All.SendAsync("ReceiveNotification", o.Id);

                  
                    List<GioHang> gh = GetListCarts();

                    var orderDetailsViewModel = new DonHangDetailsViewModel
                    {
                        DonHangId = o.Id,
                        NgayTao = o.NgayTao,
                        PayPalKey = o.PayPalKey,
                        KhachHang = o.KhachHang,
                        ChiTietDonHangs = new List<ChiTietDonHang>()
                    };

                    decimal tongTien = 0; // Khởi tạo tổng tiền

                    foreach (var item in gh)
                    {
                        var caPhe = da.CaPhes.Find(item.Id);

                        ChiTietDonHang odd = new ChiTietDonHang
                        {
                            DonHangId = o.Id,
                            CaPheId = item.Id,
                            SoLuong = item.SoLuong,
                            Tien = item.Tien,
                            KhuyenMaiId = 1,
                            CaPhe = caPhe
                        };
                        da.ChiTietDonHangs.Add(odd);
                        orderDetailsViewModel.ChiTietDonHangs.Add(odd);

                        tongTien += (decimal)item.Tien * item.SoLuong; // Tính tổng tiền
                    }
                    da.SaveChanges();

                    var productDetails = new StringBuilder();
                    foreach (var item in gh)
                    {
                        productDetails.AppendLine($"Sản phẩm: {item.Ten}, Số lượng: {item.SoLuong}. ");
                    }
                    var khachHang = da.KhachHangs.Find(MaKH);
                    string userName = khachHang.HoKh + " " + khachHang.TenKh;

                    string adminEmail = "trongphucnguyen14703@gmail.com";
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress("trongphuc1321@gmail.com"),
                        Subject = "Thông báo thanh toán đơn hàng",
                        Body = $"Đơn hàng #{o.Id} đã được thanh toán thành công." +
                       $"<br/>Người mua: {userName}" +
                       $"<br/><br/>Chi tiết sản phẩm:<br/>{productDetails.ToString()}",
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(adminEmail); // Gửi mã OTP đến email người dùng đã đăng ký

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
                        ViewData["Message"] = "Đơn hàng thanh toán đã gửi đến gmail của Admin";
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

                    HttpContext.Session.Remove("GioHang");

                    ViewBag.TongTien = tongTien; // Truyền tổng tiền vào ViewBag

                    return View("PaymentSuccess", orderDetailsViewModel);
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
