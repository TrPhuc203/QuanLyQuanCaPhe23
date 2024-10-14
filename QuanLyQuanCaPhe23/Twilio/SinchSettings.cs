using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
namespace QuanLyQuanCaPhe23.Twilio
{
    public class SinchSettings
    {
        public string ApplicationKey { get; set; }
        public string ApplicationSecret { get; set; }
    }
}
