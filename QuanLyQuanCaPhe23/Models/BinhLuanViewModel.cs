using System;

namespace QuanLyQuanCaPhe23.Models
{
    public class BinhLuanViewModel
    {
        public int Id { get; set; }
        public string TenSanPham { get; set; }
        public string TenKhachHang { get; set; }
        public string NoiDung { get; set; }
        public int? SoSao { get; set; }
        public DateTime? NgayTao { get; set; }
    }
}
