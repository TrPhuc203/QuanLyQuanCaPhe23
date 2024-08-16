using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyQuanCaPhe23.VNPAY
{
    public class VnPaymentResponseModel
    {
        public bool Success { get; set; }
        public string PaymentMethod { get; set; }
        public string OrderDescription { get; set; }
        public string OrderId { get; set; }
        public string PaymentId { get; set; }
        public string TransactionId { get; set; }
        public string Token { get; set; }
        public string VnPayResponseCode { get; set; }
    }
    public class VnPaymentRequestModel
    {
        public int OrderId { get; set; }
        public string FullName{ get; set; }
        public string Description { get; set; }
        public int Amout { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Amount { get; internal set; }
    }
}
