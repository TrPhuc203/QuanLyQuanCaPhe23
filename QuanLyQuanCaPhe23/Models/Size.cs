using System;
using System.Collections.Generic;

#nullable disable

namespace QuanLyQuanCaPhe23.Models
{
    public partial class Size
    {
        public Size()
        {
            CaPhes = new HashSet<CaPhe>();
        }

        public int Id { get; set; }
        public string Ten { get; set; }
        public string DungTich { get; set; }

        public virtual ICollection<CaPhe> CaPhes { get; set; }
    }
}
