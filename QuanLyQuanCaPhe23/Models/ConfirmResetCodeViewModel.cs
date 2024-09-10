using System;
using System.Collections.Generic;

#nullable disable

namespace QuanLyQuanCaPhe23.Models
{
    public class ConfirmResetCodeViewModel
    {
        public string Username { get; set; }
        public string ResetCode { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
