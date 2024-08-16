using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanCaPhe23.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyQuanCaPhe23.Controllers
{
    public class CaPheController : Controller
    {
        QUANLYCAPHEContext da = new QUANLYCAPHEContext();
        // GET: CaPheController
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ListCaPhe(string? kw, string? page, string? size, int? gia)
        {


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
                int pageNumber = int.Parse(page);

                ds = da.CaPhes
                          .OrderBy(item => item.Id)
                          .Skip((pageNumber - 1) * pageSize)//Lấy từ vị trí
                          .Take(pageSize)//Lấy bao nhiêu
                          .ToList();
            }

            return View(ds);
        }

        // GET: CaPheController/Details/5
        public ActionResult Details(int id)
        {
            var p = da.CaPhes.Include(c => c.Size).FirstOrDefault(s => s.Id == id);
            return View(p);
        }

        // GET: CaPheController/Create
        public ActionResult Create()
        {
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
    }
}
