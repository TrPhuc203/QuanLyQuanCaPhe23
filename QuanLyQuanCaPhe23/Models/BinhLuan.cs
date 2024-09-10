using System;
using System.Collections.Generic;

#nullable disable

namespace QuanLyQuanCaPhe23.Models
{
    public partial class BinhLuan
    {
        public int Id { get; set; }
        public int? SanPhamId { get; set; }
        public int? KhachHangId { get; set; }
        public string NoiDung { get; set; }
        public int? SoSao { get; set; }
        public DateTime? NgayTao { get; set; }

        public virtual KhachHang KhachHang { get; set; }
        public virtual CaPhe SanPham { get; set; }
    }
}
