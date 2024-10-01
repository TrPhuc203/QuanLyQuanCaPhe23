using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyQuanCaPhe23.Models;
using QuanLyQuanCaPhe23.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;

namespace QuanLyQuanCaPhe23.Controllers
{
    public class AdminController : Controller
    {
        QUANLYCAPHEContext da = new QUANLYCAPHEContext();
        public readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IHttpContextAccessor httpContextAccessor, ILogger<AdminController> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

        }
        private bool IsAdminLoggedIn()
        {
            string username = _httpContextAccessor.HttpContext.Session.GetString("UserName");
            return !string.IsNullOrEmpty(username) && username == "admin";
        }
        // GET: AdminController
        public ActionResult ThongKeTheoDoanhThu(DateTime? kw)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }

            string check = _httpContextAccessor.HttpContext.Session.GetString("UserName");
            if (check != null)
            {
                TempData["IsLoggedInAd"] = true;
                DateTime dateFilter = kw ?? DateTime.Today;

                var result = da.ChiTietDonHangs
                    .Include(ct => ct.CaPhe)
                    .ThenInclude(cp => cp.Size)
                    .Where(ct => EF.Functions.DateDiffDay(ct.DonHang.NgayTao, dateFilter) == 0) // Lọc theo ngày tạo đơn hàng
                    .GroupBy(ct => new { CaPheTen = ct.CaPhe.Ten, SizeTen = ct.CaPhe.Size.Ten })
                    .Select(g => new
                    {
                        CaPhe = g.Key.CaPheTen + " " + g.Key.SizeTen,
                        DoanhThu = g.Sum(ct => ct.SoLuong * ct.Tien)
                    }).ToList();

                List<ThongKeTheoDoanhThuModel> ds = result.Select(item => new ThongKeTheoDoanhThuModel
                {
                    Ten = item.CaPhe,
                    DoanhThu = (decimal)item.DoanhThu
                }).ToList();

                ViewBag.SelectedDate = dateFilter; // Truyền ngày đã chọn cho view
                return View(ds);
            }
            else
            {
                return RedirectToAction("DangNhap");
            }
        }

        // GET: AdminController/Details/5
        public ActionResult ThongKeTheoSoLuongSanPham(DateTime? kw)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }

            string check = _httpContextAccessor.HttpContext.Session.GetString("UserName");
            if (check != null)
            {
                TempData["IsLoggedInAd"] = true;
                DateTime dateFilter = kw ?? DateTime.Today;

                // Lấy tổng số lượng của từng sản phẩm trong ngày được chọn
                var result = da.ChiTietDonHangs
                    .Include(ct => ct.CaPhe)
                    .ThenInclude(cp => cp.Size)
                    .Where(ct => EF.Functions.DateDiffDay(ct.DonHang.NgayTao, dateFilter) == 0) // Lọc theo ngày tạo đơn hàng
                    .GroupBy(ct => new { CaPheTen = ct.CaPhe.Ten, SizeTen = ct.CaPhe.Size.Ten })
                    .Select(g => new
                    {
                        CaPhe = g.Key.CaPheTen + " " + g.Key.SizeTen,
                        SoLuongBanDuoc = g.Sum(ct => ct.SoLuong) // Tính tổng số lượng sản phẩm
                    }).ToList();

                // Chuyển đổi kết quả thành danh sách mô hình
                List<ThongKeSoLuongSanPhamModel> ds = result.Select(item => new ThongKeSoLuongSanPhamModel
                {
                    Ten = item.CaPhe,
                    SoLuong = (int)item.SoLuongBanDuoc
                }).ToList();

                ViewBag.SelectedDate = dateFilter;
                return View(ds);
            }
            else
                return RedirectToAction("DangNhap");
        }


        public ActionResult ListCaPhe(string? kw, string? page, string? size, int? gia)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }

            string check = _httpContextAccessor.HttpContext.Session.GetString("UserName");
            if (check != null)
            {
                TempData["IsLoggedInAd"] = true;
                ViewData["SuccessMessage"] = _httpContextAccessor.HttpContext.Session.GetString("SuccessMessage");

                // Lấy tên người dùng từ session
                var nameH = _httpContextAccessor.HttpContext.Session.GetString("HoQl");
                var name = _httpContextAccessor.HttpContext.Session.GetString("TenQl");

                if (!string.IsNullOrEmpty(nameH) && !string.IsNullOrEmpty(name))
                {
                    ViewData["FullName"] = $"{nameH} {name}";
                }
                else
                {
                    ViewData["FullName"] = "Quản Lý";
                }

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

        // GET: CaPheController/Details/5
        public ActionResult Details(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }

            TempData["IsLoggedInAd"] = true;

            var sanPham = da.CaPhes.Include(sp => sp.Size).FirstOrDefault(sp => sp.Id == id);
            if (sanPham == null)
            {
                return NotFound();
            }

            var binhLuans = da.BinhLuans
                .Where(bl => bl.SanPhamId == id)
                .Include(bl => bl.KhachHang)
                .ToList();

            var model = new ProductDetailsViewModel
            {
                SanPham = sanPham,
                BinhLuans = binhLuans
            };

            return View(model);
        }

        // GET: CaPheController/Create
        public ActionResult Create()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }
            TempData["IsLoggedInAd"] = true;
            ViewData["Sizes"] = new SelectList(da.Sizes, "Id", "Ten");
            return View();
        }

        // POST: CaPheController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection, IFormFile Anh)
        {
            try
            {
                if (!IsAdminLoggedIn())
                {
                    return RedirectToAction("DangNhap");
                }
                TempData["IsLoggedInAd"] = true;
                string imageUrl = "";
                if (Anh != null && Anh.Length > 0)
                {
                    // Gọi hàm UploadImage để upload ảnh lên Cloudinary và nhận lại URL của ảnh
                    imageUrl = UploadImage(Anh);

                    // Gán URL của ảnh cho thuộc tính Anh của model


                }
                // Lấy SizeID và giá từ form

                // Lấy SizeID và giá từ form
                int sizeID = int.Parse(collection["SizeId"]);
                Console.WriteLine(sizeID);
                CaPhe cp = new CaPhe();
                cp.MieuTa = collection["MieuTa"];
                cp.Ten = collection["Ten"];
                cp.Anh = imageUrl;
                cp.SizeId = sizeID;
                cp.Tien = Decimal.Parse(collection["Tien"]);
                cp.MaQl = null;

                da.CaPhes.Add(cp);
                da.SaveChanges();
                return RedirectToAction("ListCaPhe");

            }
            catch
            {
                return View();
            }
        }
        public string UploadImage(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                Account account = new Account(
                    "drlkwk9ch",
                    "859351453916551",
                    "Szyn_GktO9BGfHcZnwbeg-IfoEI"
                );

                Cloudinary cloudinary = new Cloudinary(account);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(fileName, stream)
                    };

                    var uploadResult = cloudinary.Upload(uploadParams);

                    // Lấy URL của ảnh đã tải lên từ kết quả
                    string imageUrl = uploadResult.SecureUri.ToString();

                    // Lưu URL vào cơ sở dữ liệu hoặc thực hiện các thao tác khác
                    // Ví dụ: return URL để sử dụng trong View
                    return imageUrl;
                }
            }
            else
            {
                // Trả về thông báo lỗi nếu không có file được chọn
                return "Error";
            }

        }
        // GET: CaPheController/Edit/5
        public ActionResult Edit(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }
            TempData["IsLoggedInAd"] = true;
            ViewData["Sizes"] = new SelectList(da.Sizes, "Id", "Ten");

            var p = da.CaPhes.Include(c => c.Size).FirstOrDefault(s => s.Id == id);
            return View(p);
        }

        // POST: CaPheController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection, IFormFile Anh)
        {

            try
            {
                if (!IsAdminLoggedIn())
                {
                    return RedirectToAction("DangNhap");
                }
                TempData["IsLoggedInAd"] = true;
                var cp = da.CaPhes.Include(c => c.Size).FirstOrDefault(s => s.Id == id);
                string anh = cp.Anh;
                int sizeID = int.Parse(collection["SizeId"]);
                cp.MieuTa = collection["MieuTa"];
                cp.Ten = collection["Ten"];

                cp.SizeId = sizeID;
                cp.Tien = Decimal.Parse(collection["Tien"]);


                if (Anh != null && Anh.Length > 0)
                {
                    // Tải tệp hình ảnh mới lên Cloudinary và lấy URL của nó
                    string imageUrl = UploadImage(Anh);

                    // Gán URL của hình ảnh mới cho sản phẩm cà phê
                    cp.Anh = imageUrl;
                }
                else
                {
                    // Nếu không có tệp hình ảnh mới được tải lên, giữ nguyên URL của hình ảnh cũ
                    cp.Anh = anh;
                }
                // Gửi các thay đổi đến cơ sở dữ liệu
                da.SaveChanges();

                // Chuyển hướng đến danh sách sản phẩm sau khi cập nhật thành công
                return RedirectToAction("ListCaPhe");
            }
            catch
            {
                return View();
            }
        }

        // GET: CaPheController/Delete/5
        public ActionResult Delete(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }
            TempData["IsLoggedInAd"] = true;
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
                if (!IsAdminLoggedIn())
                {
                    return RedirectToAction("DangNhap");
                }
                TempData["IsLoggedInAd"] = true;
                var p = da.CaPhes.Include(c => c.Size).FirstOrDefault(s => s.Id == id);
                da.CaPhes.Remove(p);
                da.SaveChanges();
                return RedirectToAction("ListCaPhe");
            }
            catch
            {
                return View();
            }
        }
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
            var idQL = collection["MaQl"];

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
                QuanLy ql = da.QuanLies.SingleOrDefault(k => k.UserName.Equals(uname.ToString()) && k.Pass.Equals(pass.ToString()));
                if (ql != null)
                {
                    _httpContextAccessor.HttpContext.Session.SetInt32("MaQl", ql.MaQl);
                    _httpContextAccessor.HttpContext.Session.SetString("UserName", ql.UserName);
                    _httpContextAccessor.HttpContext.Session.SetString("HoQl", ql.HoQl); // Lưu họ vào session
                    _httpContextAccessor.HttpContext.Session.SetString("TenQl", ql.TenQl); // Lưu tên vào session
                    TempData["IsLoggedInAd"] = true;
                    _httpContextAccessor.HttpContext.Session.SetString("IsLoggedInAd", "true");
                    _httpContextAccessor.HttpContext.Session.SetString("FullNameAd", ql.HoQl + " " + ql.TenQl);

                    _httpContextAccessor.HttpContext.Session.SetString("SuccessMessage", "Chúc mừng đăng nhập thành công");
                    return RedirectToAction("ListCaPhe");
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
            return RedirectToAction("DangNhap", "Admin");
        }
        public ActionResult ListDonHang(string? page)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }

            string check = _httpContextAccessor.HttpContext.Session.GetString("UserName");
            if (check != null)
            {
                TempData["IsLoggedInAd"] = true;
                ViewData["SuccessMessage"] = _httpContextAccessor.HttpContext.Session.GetString("SuccessMessage");

                // Lấy tên người dùng từ session
                var nameH = _httpContextAccessor.HttpContext.Session.GetString("HoQl");
                var name = _httpContextAccessor.HttpContext.Session.GetString("TenQl");

                if (!string.IsNullOrEmpty(nameH) && !string.IsNullOrEmpty(name))
                {
                    ViewData["FullName"] = $"{nameH} {name}";
                }
                else
                {
                    ViewData["FullName"] = "Quản Lý";
                }
                // Xóa thông báo từ session

                var pageSize = 6; // Hiển thị 6 đơn hàng mỗi trang
                var pageNumber = 1; // Mặc định là trang 1 nếu không có tham số trang

                if (!string.IsNullOrEmpty(page))
                {
                    // Chuyển đổi tham số trang thành số nguyên
                    int.TryParse(page, out pageNumber);
                }

                // Lấy danh sách đơn hàng và phân trang
                var ds = da.DonHangs
                          .OrderBy(item => item.Id)
                          .Skip((pageNumber - 1) * pageSize) // Bỏ qua số lượng đơn hàng ở các trang trước
                          .Take(pageSize) // Lấy số lượng đơn hàng cho trang hiện tại
                          .ToList();

                TempData["IsLoggedInAd"] = true;

                // Truyền dữ liệu phân trang tới view
                ViewBag.PageNumber = pageNumber;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = da.DonHangs.Count(); // Tổng số đơn hàng

                return View(ds);
            }
            else
            {
                return RedirectToAction("DangNhap");
            }
        }
        public async Task<IActionResult> DonHangDetails(int? id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }
            TempData["IsLoggedInAd"] = true;

            if (id == null)
            {
                return NotFound();
            }

            var donHang = await da.DonHangs
                .Include(dh => dh.KhachHang)
                .Include(dh => dh.ChiTietDonHangs)
                    .ThenInclude(ctdh => ctdh.CaPhe)
                .Include(dh => dh.ChiTietDonHangs)
                    .ThenInclude(ctdh => ctdh.KhuyenMai)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (donHang == null)
            {
                return NotFound();
            }
            TempData["IsLoggedInAd"] = true;

            var viewModel = new DonHangDetailsViewModel
            {
                DonHangId = donHang.Id,
                NgayTao = donHang.NgayTao,
                PayPalKey = donHang.PayPalKey,
                KhachHang = donHang.KhachHang,
                ChiTietDonHangs = donHang.ChiTietDonHangs.ToList()
            };

            return View(viewModel);
        }
        // GET: CaPheController/Delete/5
        public ActionResult DeleteDonHang(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }
            TempData["IsLoggedInAd"] = true;
            var p = da.DonHangs.FirstOrDefault(s => s.Id == id);
            return View(p);
        }


        // POST: CaPheController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteDonHang(int id, IFormCollection collection)
        {
            try
            {
                if (!IsAdminLoggedIn())
                {
                    return RedirectToAction("DangNhap");
                }
                TempData["IsLoggedInAd"] = true;

                // Xóa tất cả các ChiTietDonHang liên quan đến DonHang
                var chiTietDonHangs = da.ChiTietDonHangs.Where(ct => ct.DonHangId == id);
                da.ChiTietDonHangs.RemoveRange(chiTietDonHangs);
                da.SaveChanges();

                // Sau đó, xóa DonHang
                var dh = da.DonHangs.FirstOrDefault(s => s.Id == id);
                da.DonHangs.Remove(dh);
                da.SaveChanges();

                return RedirectToAction("ListDonHang");
            }
            catch
            {
                return View();
            }
        }

        [HttpGet]
        public IActionResult PrintInvoice(int donHangId)
        {
            // Lấy đơn hàng và các chi tiết đơn hàng liên quan
            var donHang = da.DonHangs
                            .Include(dh => dh.KhachHang)
                            .Include(dh => dh.ChiTietDonHangs)
                                .ThenInclude(ct => ct.CaPhe)
                                .ThenInclude(cp => cp.Size)
                            .FirstOrDefault(dh => dh.Id == donHangId);

            if (donHang == null)
            {
                return NotFound();
            }

            // Tạo PDF invoice ở đây
            var pdfFileBytes = CreatePdfInvoice(donHang);

            // Trả về file PDF và mở trực tiếp trên trình duyệt với Content-Disposition inline
            Response.Headers.Add("Content-Disposition", "inline; filename=HoaDon.pdf");
            return File(pdfFileBytes, "application/pdf");
        }
        private byte[] CreatePdfInvoice(DonHang donHang)
        {
            // Tạo tài liệu PDF với kích thước nhỏ hơn (A6)
            var pdfDocument = new PdfDocument();
            var pdfPage = pdfDocument.AddPage();
            pdfPage.Width = XUnit.FromMillimeter(105); // Độ rộng A6
            pdfPage.Height = XUnit.FromMillimeter(148); // Độ cao A6
            var gfx = XGraphics.FromPdfPage(pdfPage);

            // Đặt font chữ
            var titleFont = new XFont("Courier New", 12, XFontStyle.Bold);
            var regularFont = new XFont("Courier New", 8, XFontStyle.Regular);
            var smallFont = new XFont("Courier New", 7, XFontStyle.Regular);

            // Thiết lập margin và vị trí bắt đầu vẽ
            double pageWidth = pdfPage.Width.Point;
            double leftMargin = 10;
            double rightMargin = 210; // Tạo khoảng trống bên phải rộng hơn
            double yPoint = 10;

            // Hàm vẽ đường gạch "===="
            Action<double> drawDashedLine = (y) =>
            {
                string dashes = new string('=', (int)((rightMargin - leftMargin) / 3));
                gfx.DrawString(dashes, smallFont, XBrushes.Black, new XPoint(leftMargin, y));
            };

            // Tiêu đề hóa đơn
            gfx.DrawString("PHUC COFFEE", titleFont, XBrushes.Black, new XPoint(leftMargin, yPoint));
            yPoint += 15;

            // Thông tin cửa hàng
            gfx.DrawString("Dia chi: Abc, Huyen Nha Be, TP.HCM", smallFont, XBrushes.Black, new XPoint(leftMargin, yPoint));
            yPoint += 10;
            gfx.DrawString("SDT: 0328877290", smallFont, XBrushes.Black, new XPoint(leftMargin, yPoint));
            yPoint += 10;

            drawDashedLine(yPoint);
            yPoint += 10;

            // Thông tin đơn hàng
            gfx.DrawString($"Ngay: {DateTime.Now:dd/MM/yyyy HH:mm}", smallFont, XBrushes.Black, new XPoint(leftMargin, yPoint));
            yPoint += 10;
            gfx.DrawString($"Ma don hang: {donHang.Id}", smallFont, XBrushes.Black, new XPoint(leftMargin, yPoint));
            yPoint += 10;

            drawDashedLine(yPoint);
            yPoint += 10;

            // Thêm thông tin khách hàng (tên, địa chỉ, số điện thoại)
            gfx.DrawString($"Khach hang: {donHang.KhachHang.HoKh} {donHang.KhachHang.TenKh}", smallFont, XBrushes.Black, new XPoint(leftMargin, yPoint));
            yPoint += 10;
            gfx.DrawString($"Dia chi: {donHang.KhachHang.DiaChi}", smallFont, XBrushes.Black, new XPoint(leftMargin, yPoint));
            yPoint += 10;
            gfx.DrawString($"SDT: {donHang.KhachHang.SoDienThoai}", smallFont, XBrushes.Black, new XPoint(leftMargin, yPoint)); // Thêm số điện thoại khách hàng
            yPoint += 10;

            drawDashedLine(yPoint);
            yPoint += 10;

            // Tiêu đề cột
            gfx.DrawString("San pham", regularFont, XBrushes.Black, new XPoint(leftMargin, yPoint));
            gfx.DrawString("SL", regularFont, XBrushes.Black, new XPoint(rightMargin - 80, yPoint));
            gfx.DrawString("Gia", regularFont, XBrushes.Black, new XPoint(rightMargin - 40, yPoint));
            yPoint += 10;

            drawDashedLine(yPoint);
            yPoint += 10;

            // Thêm chi tiết đơn hàng
            foreach (var chiTiet in donHang.ChiTietDonHangs)
            {
                string tenSanPham = $"{chiTiet.CaPhe.Ten} ({chiTiet.CaPhe.Size?.Ten ?? "N/A"})";
                gfx.DrawString(tenSanPham, regularFont, XBrushes.Black, new XPoint(leftMargin, yPoint));
                gfx.DrawString(chiTiet.SoLuong.ToString(), regularFont, XBrushes.Black, new XPoint(rightMargin - 80, yPoint));
                gfx.DrawString($"{(chiTiet.CaPhe.Tien * chiTiet.SoLuong):N0}", regularFont, XBrushes.Black, new XPoint(rightMargin - 40, yPoint));
                yPoint += 10;
            }

            drawDashedLine(yPoint);
            yPoint += 10;

            // Tổng tiền
            var tongTien = donHang.ChiTietDonHangs.Sum(ct => ct.CaPhe.Tien * ct.SoLuong);
            gfx.DrawString("Tong cong:", regularFont, XBrushes.Black, new XPoint(leftMargin, yPoint));
            gfx.DrawString($"{tongTien:N0} VND", regularFont, XBrushes.Black, new XPoint(rightMargin - 40, yPoint));
            yPoint += 15;

            drawDashedLine(yPoint);
            yPoint += 10;

            // Lời cảm ơn
            gfx.DrawString("Cam on quy khach!", smallFont, XBrushes.Black, new XPoint((pageWidth - gfx.MeasureString("Cam on quy khach!", smallFont).Width) / 2, yPoint));

            // Lưu tài liệu PDF vào MemoryStream
            using (MemoryStream stream = new MemoryStream())
            {
                pdfDocument.Save(stream, false);
                return stream.ToArray();
            }
        }
    }
}
