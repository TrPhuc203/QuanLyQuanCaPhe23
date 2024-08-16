using System;
using System.Collections.Generic;
using QuanLyQuanCaPhe23.Models;

namespace QuanLyQuanCaPhe23.ViewModels
{
    public class DonHangDetailsViewModel
    {
        public int DonHangId { get; set; }
        public DateTime? NgayTao { get; set; }
        public string PayPalKey { get; set; }
        public KhachHang KhachHang { get; set; }
        public List<ChiTietDonHang> ChiTietDonHangs { get; set; }
    }
}
