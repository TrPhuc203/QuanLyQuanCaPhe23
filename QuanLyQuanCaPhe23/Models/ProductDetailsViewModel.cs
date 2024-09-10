using System.Collections.Generic;

namespace QuanLyQuanCaPhe23.Models
{
    public class ProductDetailsViewModel
    {
        public CaPhe SanPham { get; set; }
        public List<BinhLuan> BinhLuans { get; set; }
    }
}
