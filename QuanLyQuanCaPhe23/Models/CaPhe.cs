using System;
using System.Collections.Generic;

#nullable disable

namespace QuanLyQuanCaPhe23.Models
{
    public partial class CaPhe
    {
        public CaPhe()
        {
            BinhLuans = new HashSet<BinhLuan>();
            ChiTietDonHangs = new HashSet<ChiTietDonHang>();
        }

        public int Id { get; set; }
        public string Ten { get; set; }
        public string MieuTa { get; set; }
        public string Anh { get; set; }
        public int? SizeId { get; set; }
        public decimal? Tien { get; set; }
        public int? MaQl { get; set; }

        public virtual QuanLy MaQlNavigation { get; set; }
        public virtual Size Size { get; set; }
        public virtual ICollection<BinhLuan> BinhLuans { get; set; }
        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; }
    }
}
