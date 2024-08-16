using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyQuanCaPhe23.ESMS
{
    public class ESMSResponse
    {
        public string CodeResult { get; set; } // Mã kết quả
        public string ErrorMessage { get; set; } // Thông điệp lỗi (nếu có)
    }
}
