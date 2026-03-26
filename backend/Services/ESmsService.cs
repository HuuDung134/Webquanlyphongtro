using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DoAnCoSo.Services
{
    public interface IESmsService
    {
        Task<(bool Success, string? ErrorMessage, string? CodeResult)> SendSmsAsync(string phone, string content, string? brandname = null);
    }

    /// <summary>
    /// Service gửi SMS qua eSMS API
    /// </summary>
    public class ESmsService : IESmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ESmsService> _logger;
        private readonly string _apiKey;
        private readonly string _secretKey;
        private readonly string _brandName;
        private readonly string _apiUrl;

        public ESmsService(HttpClient httpClient, IConfiguration configuration, ILogger<ESmsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _apiKey = (_configuration["ESms:ApiKey"] ?? Environment.GetEnvironmentVariable("ESMS_API_KEY") ?? "").Trim();
            _secretKey = (_configuration["ESms:SecretKey"] ?? Environment.GetEnvironmentVariable("ESMS_SECRET_KEY") ?? "").Trim();
            _brandName = (_configuration["ESms:BrandName"] ?? Environment.GetEnvironmentVariable("ESMS_BRAND_NAME") ?? "").Trim();
            var apiUrl = _configuration["ESms:ApiUrl"] ?? Environment.GetEnvironmentVariable("ESMS_API_URL") ?? "https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/";
            // Đảm bảo URL có dấu / ở cuối
            if (!string.IsNullOrEmpty(apiUrl) && !apiUrl.EndsWith("/"))
            {
                apiUrl += "/";
            }
            _apiUrl = apiUrl;

            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_secretKey))
            {
                _logger.LogWarning("[ESmsService] ESms API Key hoặc Secret Key chưa được cấu hình");
            }
        }

        /// <summary>
        /// Gửi SMS qua eSMS API
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage, string? CodeResult)> SendSmsAsync(string phone, string content, string? brandname = null)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_secretKey))
            {
                var error = "ESms API Key hoặc Secret Key chưa được cấu hình";
                _logger.LogWarning("[ESmsService] {Error}", error);
                return (false, error, null);
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                var error = "Số điện thoại rỗng";
                _logger.LogWarning("[ESmsService] {Error}", error);
                return (false, error, null);
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                var error = "Nội dung tin nhắn rỗng";
                _logger.LogWarning("[ESmsService] {Error}", error);
                return (false, error, null);
            }

            try
            {
                // Chuẩn hóa số điện thoại
                phone = NormalizePhoneNumber(phone);

                if (string.IsNullOrWhiteSpace(phone))
                {
                    var error = "Số điện thoại không hợp lệ sau khi chuẩn hóa";
                    _logger.LogWarning("[ESmsService] {Error}", error);
                    return (false, error, null);
                }

                var brandNameToUse = brandname ?? _brandName;
                if (string.IsNullOrWhiteSpace(brandNameToUse))
                {
                    brandNameToUse = "Baotrixemay"; // Default brandname
                }

                // Tạo request body theo format của eSMS API
                var requestBody = new
                {
                    ApiKey = _apiKey,
                    SecretKey = _secretKey,
                    Phone = phone,
                    Content = content,
                    Brandname = brandNameToUse,
                    SmsType = "2",
                    Sandbox = "0" // Production mode
                };

                var json = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("[ESmsService] Đang gửi SMS đến {Phone}", phone);

                var response = await _httpClient.PostAsync(_apiUrl, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (result.TryGetProperty("CodeResult", out var codeResult))
                        {
                            var code = codeResult.GetString();
                            
                            if (code == "100" || code == "200")
                            {
                                _logger.LogInformation("[ESmsService] Gửi SMS thành công đến {Phone}. CodeResult: {Code}", phone, code);
                                return (true, null, code);
                            }
                            else
                            {
                                var errorMsg = result.TryGetProperty("ErrorMessage", out var errorMsgElement) 
                                    ? errorMsgElement.GetString() 
                                    : $"CodeResult: {code}";
                                
                                _logger.LogWarning("[ESmsService] Gửi SMS thất bại đến {Phone}. {Error}", phone, errorMsg);
                                return (false, errorMsg, code);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("[ESmsService] Response không có CodeResult: {Response}", responseContent);
                            return (false, "Response không có CodeResult", null);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[ESmsService] Lỗi parse response: {Response}", responseContent);
                        return (false, $"Lỗi parse response: {ex.Message}", null);
                    }
                }
                else
                {
                    var error = $"HTTP Error: {response.StatusCode}, Response: {responseContent}";
                    _logger.LogWarning("[ESmsService] {Error}", error);
                    return (false, error, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ESmsService] Lỗi khi gửi SMS đến {Phone}", phone);
                return (false, $"Exception: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Chuẩn hóa số điện thoại Việt Nam
        /// </summary>
        private string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            // Loại bỏ khoảng trắng, dấu gạch ngang, dấu ngoặc
            phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            // Loại bỏ dấu +
            if (phone.StartsWith("+"))
                phone = phone.Substring(1);

            // Nếu bắt đầu bằng 84, thay bằng 0
            if (phone.StartsWith("84") && phone.Length >= 10)
                phone = "0" + phone.Substring(2);

            // Kiểm tra định dạng số điện thoại Việt Nam (10 số, bắt đầu bằng 0)
            if (phone.Length == 10 && phone.StartsWith("0") && phone.All(char.IsDigit))
                return phone;

            // Nếu không đúng định dạng, thử loại bỏ số 0 đầu và thêm lại
            if (phone.Length == 9 && phone.All(char.IsDigit))
                return "0" + phone;

            return string.Empty;
        }
    }
}

