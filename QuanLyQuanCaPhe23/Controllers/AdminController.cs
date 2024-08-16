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
                TempData["IsLoggedIn"] = true;
                DateTime dateFilter = kw ?? DateTime.Today;

                var result = da.ChiTietDonHangs.
                    Include(ct => ct.CaPhe)
                    .ThenInclude(cp => cp.Size)
                    .Where(ct => EF.Functions.DateDiffDay(ct.DonHang.NgayTao, dateFilter) == 0) // Lọc theo ngày tạo đơn hàng
                    .GroupBy(ct => new { CaPheTen = ct.CaPhe.Ten, SizeTen = ct.CaPhe.Size.Ten })
                    .Select(g => new
                    {
                        CaPhe = g.Key.CaPheTen + " " + g.Key.SizeTen,
                        DoanhThu = g.Sum(ct => ct.SoLuong * ct.Tien)
                    })
                    .ToList();
                List<ThongKeTheoDoanhThuModel> ds = new List<ThongKeTheoDoanhThuModel>();
                foreach (var item in result)
                {
                    ThongKeTheoDoanhThuModel a = new ThongKeTheoDoanhThuModel();
                    a.Ten = item.CaPhe;
                    a.DoanhThu = (decimal)item.DoanhThu;
                    ds.Add(a);
                }
                return View(ds);
            }
            else
                return RedirectToAction("DangNhap");
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
                TempData["IsLoggedIn"] = true;
                DateTime dateFilter = kw ?? DateTime.Today;
                var result = da.ChiTietDonHangs
                    .Include(ct => ct.CaPhe)
                    .ThenInclude(cp => cp.Size)
                    .Where(ct => EF.Functions.DateDiffDay(ct.DonHang.NgayTao, dateFilter) == 0) // Lọc theo ngày tạo đơn hàng
                    .GroupBy(ct => new { CaPheTen = ct.CaPhe.Ten, SizeTen = ct.CaPhe.Size.Ten })
                    .Select(g => new
                    {
                        CaPhe = g.Key.CaPheTen + " " + g.Key.SizeTen,
                        SoLuongBanDuoc = g.Count() // Sử dụng hàm Count() để đếm số lượng sản phẩm
                }).ToList();
                List<ThongKeSoLuongSanPhamModel> ds = new List<ThongKeSoLuongSanPhamModel>();
                foreach (var item in result)
                {
                    Console.WriteLine($"CaPhe: {item.CaPhe}, SoLuongBanDuoc: {item.SoLuongBanDuoc}");
                    ThongKeSoLuongSanPhamModel a = new ThongKeSoLuongSanPhamModel();
                    a.Ten = item.CaPhe;
                    a.SoLuong = (int)item.SoLuongBanDuoc;
                    ds.Add(a);
                }
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
                TempData["IsLoggedIn"] = true;
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
                var ds = da.CaPhes.Include(c => c.Size).ToList();
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
                    TempData["IsLoggedIn"] = true;

                    int pageNumber = int.Parse(page);
                    TempData["IsLoggedIn"] = true;

                    ds = da.CaPhes
                              .OrderBy(item => item.Id)
                              .Skip((pageNumber - 1) * pageSize)//Lấy từ vị trí
                              .Take(pageSize)//Lấy bao nhiêu
                              .ToList();
                    TempData["IsLoggedIn"] = true;

                }

                return View(ds);
            }
            else
                return RedirectToAction("DangNhap");
        }

        // GET: CaPheController/Details/5
        public ActionResult Details(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }
            TempData["IsLoggedIn"] = true;
            var p = da.CaPhes.Include(c => c.Size).FirstOrDefault(s => s.Id == id);
            return View(p);
        }

        // GET: CaPheController/Create
        public ActionResult Create()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }
            TempData["IsLoggedIn"] = true;
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
                TempData["IsLoggedIn"] = true;
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
            TempData["IsLoggedIn"] = true;
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
                TempData["IsLoggedIn"] = true;
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
                if (!IsAdminLoggedIn())
                {
                    return RedirectToAction("DangNhap");
                }
                TempData["IsLoggedIn"] = true;
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
                    TempData["IsLoggedIn"] = true;
                    _httpContextAccessor.HttpContext.Session.SetString("IsLoggedIn", "true");
                    _httpContextAccessor.HttpContext.Session.SetString("FullName", ql.HoQl + " " + ql.TenQl);

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
        public ActionResult ListDonHang(string? page)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }
            string check = _httpContextAccessor.HttpContext.Session.GetString("UserName");
            if (check != null)
            {
                TempData["IsLoggedIn"] = true;
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
                var ds = da.DonHangs.ToList();
                int pageSize = 4;
                if (!string.IsNullOrEmpty(page))
                {
                    TempData["IsLoggedIn"] = true;

                    int pageNumber = int.Parse(page);
                    TempData["IsLoggedIn"] = true;

                    ds = da.DonHangs
                              .OrderBy(item => item.Id)
                              .Skip((pageNumber - 1) * pageSize)//Lấy từ vị trí
                              .Take(pageSize)//Lấy bao nhiêu
                              .ToList();
                    TempData["IsLoggedIn"] = true;
                }
                return View(ds);
            }
            else
                return RedirectToAction("DangNhap");
        }
        public async Task<IActionResult> DonHangDetails(int? id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("DangNhap");
            }
            TempData["IsLoggedIn"] = true;

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
            TempData["IsLoggedIn"] = true;

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
            TempData["IsLoggedIn"] = true;
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
                TempData["IsLoggedIn"] = true;

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
    }
}
