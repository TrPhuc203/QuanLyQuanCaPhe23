using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyQuanCaPhe23.Models
{
    public class GioHang
    {
        QUANLYCAPHEContext da = new QUANLYCAPHEContext();

        public int Id { get; set; }
        public string Ten { get; set; }
        public string Anh { get; set; }
        public int? SizeId { get; set; }
        public decimal? Tien { get; set; }
        public int SoLuong { get; set; }

        public decimal? TongTien { get { return Tien * SoLuong; } }
        public virtual Size Size { get; set; }
        public string TenSize { get; set; }
        public GioHang(int Id)
        {
            this.Id = Id;
            CaPhe c = da.CaPhes.Single(s => s.Id == Id);
            Size s = da.Sizes.SingleOrDefault(p => p.Id == c.SizeId);
            Anh = c.Anh;
            Ten = c.Ten;
            Tien = c.Tien;
            SoLuong = 1;
            TenSize = s.Ten;
        }
   
    }
}
