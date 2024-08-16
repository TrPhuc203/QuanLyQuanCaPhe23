using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayPal.Core;
using PayPal.v1.Payments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace QuanLyQuanCaPhe23.PAYPAL
{
    public class PayPalService : IPayPalService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PayPalService> _logger;
        private const double ExchangeRate = 22_863.0;

        public PayPalService(IConfiguration configuration, ILogger<PayPalService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public static double ConvertVndToDollar(double vnd)
        {
            var total = Math.Round(vnd / ExchangeRate, 2);
            return total;
        }

        public async Task<string> CreatePaymentUrl(PaymentInformationModel model)
        {
            var envSandbox = new SandboxEnvironment(_configuration["Paypal:ClientId"], _configuration["Paypal:SecretKey"]);
            var client = new PayPalHttpClient(envSandbox);
            var paypalOrderId = DateTime.Now.Ticks;
            var urlCallBack = _configuration["PaymentCallBack:ReturnUrl"];
            var payment = new Payment()
            {
                Intent = "sale",
                Transactions = new List<Transaction>()
        {
            new Transaction()
            {
                Amount = new Amount()
                {
                    Total = ConvertVndToDollar(model.Amount).ToString("F2", CultureInfo.InvariantCulture),
                    Currency = "USD",
                    Details = new AmountDetails
                    {
                        Tax = "0.00",
                        Shipping = "0.00",
                        Subtotal = ConvertVndToDollar(model.Amount).ToString("F2", CultureInfo.InvariantCulture)
                    }
                },
                ItemList = new ItemList()
                {
                    Items = new List<Item>()
                    {
                        new Item()
                        {
                            Name = " | Order: " + model.OrderDescription,
                            Currency = "USD",
                            Price = ConvertVndToDollar(model.Amount).ToString("F2", CultureInfo.InvariantCulture),
                            Quantity = "1",
                            Sku = "sku",
                            Tax = "0.00",
                            Url = "https://www.code-mega.com"
                        }
                    }
                },
                Description = $"Invoice #{model.OrderDescription}",
                InvoiceNumber = paypalOrderId.ToString()
            }
        },
                RedirectUrls = new RedirectUrls()
                {
                    ReturnUrl = $"{urlCallBack}?payment_method=PayPal&success=1&order_id={paypalOrderId}",
                    CancelUrl = $"{urlCallBack}?payment_method=PayPal&success=0&order_id={paypalOrderId}"
                },
                Payer = new Payer()
                {
                    PaymentMethod = "paypal"
                }
            };

            var request = new PaymentCreateRequest();
            request.RequestBody(payment);

            try
            {
                // Thực hiện yêu cầu thanh toán
                var response = await client.Execute(request);
                string responseContent;

                // Sử dụng reflection để lấy nội dung phản hồi nếu có
                if (response.GetType().GetProperty("Body") != null)
                {
                    var body = response.GetType().GetProperty("Body").GetValue(response, null);
                    responseContent = body != null ? body.ToString() : string.Empty;
                }
                else
                {
                    responseContent = response.ToString();
                }

                // Ghi log phản hồi
                _logger.LogError($"PayPal response: {responseContent}");

                var result = response.Result<Payment>();
                using var links = result.Links.GetEnumerator();
                string paymentUrl = string.Empty;

                while (links.MoveNext())
                {
                    var lnk = links.Current;
                    if (lnk == null) continue;
                    if (!lnk.Rel.ToLower().Trim().Equals("approval_url")) continue;
                    paymentUrl = lnk.Href;
                }

                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating PayPal payment: {ex.Message}");
                throw;
            }
        }


        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var response = new PaymentResponseModel();

            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.ToLower().Equals("order_description"))
                {
                    response.OrderDescription = value;
                }

                if (!string.IsNullOrEmpty(key) && key.ToLower().Equals("transaction_id"))
                {
                    response.TransactionId = value;
                }

                if (!string.IsNullOrEmpty(key) && key.ToLower().Equals("order_id"))
                {
                    response.OrderId = value;
                }

                if (!string.IsNullOrEmpty(key) && key.ToLower().Equals("payment_method"))
                {
                    response.PaymentMethod = value;
                }

                if (!string.IsNullOrEmpty(key) && key.ToLower().Equals("success"))
                {
                    response.Success = Convert.ToInt32(value) > 0;
                }

                if (!string.IsNullOrEmpty(key) && key.ToLower().Equals("paymentid"))
                {
                    response.PaymentId = value;
                }

                if (!string.IsNullOrEmpty(key) && key.ToLower().Equals("payerid"))
                {
                    response.PayerId = value;
                }
            }

            return response;
        }

        public async Task<Payment> ExecutePayment(string paymentId, string payerId)
        {
            var envSandbox = new SandboxEnvironment(_configuration["Paypal:ClientId"], _configuration["Paypal:SecretKey"]);
            var client = new PayPalHttpClient(envSandbox);

            var paymentExecution = new PaymentExecution() { PayerId = payerId };
            var paymentExecuteRequest = new PaymentExecuteRequest(paymentId);
            paymentExecuteRequest.RequestBody(paymentExecution);

            var response = await client.Execute(paymentExecuteRequest);
            return response.Result<Payment>();
        }
    }
}
