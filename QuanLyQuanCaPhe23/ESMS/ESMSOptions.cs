using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QuanLyQuanCaPhe23.ESMS;
using System;
public class ESMSOptions
{
    public string ApiKey { get; set; }
    public string SecretKey { get; set; }
    public string BrandName { get; set; }
}
