using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QuanLyQuanCaPhe23.ESMS;
using System;

public class ESMSHelper
{
    private readonly string _apiKey;
    private readonly string _secretKey;
    private readonly string _brandName;
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ESMSOptions _options;

    public ESMSHelper(IOptions<ESMSOptions> options, IHttpClientFactory clientFactory)
    {
        var esmsOptions = options.Value;
        _apiKey = esmsOptions.ApiKey;
        _secretKey = esmsOptions.SecretKey;
        _brandName = esmsOptions.BrandName;
        _httpClient = new HttpClient();
        _clientFactory = clientFactory;
        _options = options.Value;

    }

    public async Task<bool> SendOtp(string phoneNumber, string message)
    {
        var client = _clientFactory.CreateClient();
        var url = "https://api.esms.vn/MainService.svc/json/SendMultipleMessage_V4_get";

        var payload = new
        {
            Phone = phoneNumber,
            Content = message,
            ApiKey = "0C520FAF3E634212D61E45C7D98884",
            SecretKey = "977E388EE29AF3E6AD019E60E69FF0",
            SmsType = 2
        };

        try
        {

            // Send request
            var response = await client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));

            // Read response
            var jsonString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(jsonString);
            var result = JsonConvert.DeserializeObject<ESMSResponse>(jsonString);
            Console.WriteLine(result);

            return result.CodeResult == "100";
        }
        catch (Exception ex)
        {
            
            // Ghi lại lỗi
            return false;
        }
    }
}

