using System;
using System.Collections.Generic;

#nullable disable

namespace QuanLyQuanCaPhe23.Models
{
    public partial class ChiTietDonHang
    {
        public int DonHangId { get; set; }
        public int CaPheId { get; set; }
        public int? KhuyenMaiId { get; set; }
        public int? SoLuong { get; set; }
        public decimal? Tien { get; set; }

        public virtual CaPhe CaPhe { get; set; }
        public virtual DonHang DonHang { get; set; }
        public virtual KhuyenMai KhuyenMai { get; set; }
    }
}
