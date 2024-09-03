using System;
using System.Collections.Generic;

#nullable disable

namespace QuanLyQuanCaPhe23.Models
{
    public partial class QuanLy
    {
        public QuanLy()
        {
            CaPhes = new HashSet<CaPhe>();
        }
        public int MaQl { get; set; }
        public string HoQl { get; set; }
        public string TenQl { get; set; }
        public string UserName { get; set; }
        public string Pass { get; set; }
        public string DiaChi { get; set; }
        public string SoDienThoai { get; set; }
        public ICollection<CaPhe> CaPhes { get; set; }
    }
}
