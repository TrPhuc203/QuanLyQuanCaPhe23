using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyQuanCaPhe23.Models
{
    public class DonHangViewModel
    {
        public int DonHangId { get; set; }
        public List<SanPhamViewModel> SanPhams { get; set; }
        public KhuyenMai? KhuyenMaiId { get; set; }
    }
}
