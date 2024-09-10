using System;
using System.Collections.Generic;

#nullable disable

namespace QuanLyQuanCaPhe23.Models
{
    public partial class DonHang
    {
        public DonHang()
        {
            ChiTietDonHangs = new HashSet<ChiTietDonHang>();
        }

        public int Id { get; set; }
        public int? KhachHangId { get; set; }
        public DateTime? NgayTao { get; set; }
        public string PayPalKey { get; set; }
        public string TrangThai { get; set; }

        public virtual KhachHang KhachHang { get; set; }
        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; }
    }
}
