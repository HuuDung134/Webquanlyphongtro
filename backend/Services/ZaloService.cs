using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace DoAnCoSo.Services
{
    public interface IZaloService
    {
        Task<bool> SendPaymentSuccessAsync(string phone, string message);
        Task<bool> SendReminderAsync(string phone, string message);
    }

    /// <summary>
    /// Gửi tin nhắn qua Zalo OA (dạng text đơn giản hoặc ZNS nếu đã cấu hình template).
    /// Hiện tại gửi text qua OA API theo access token trong cấu hình.
    /// </summary>
    public class ZaloService : IZaloService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly string _accessToken;

        public ZaloService(HttpClient httpClient, IConfiguration configuration)
        {
            _http = httpClient;
            _config = configuration;
            _accessToken = _config["Zalo:AccessToken"] ?? string.Empty;
        }

        public Task<bool> SendPaymentSuccessAsync(string phone, string message)
            => SendTextAsync(phone, message);

        public Task<bool> SendReminderAsync(string phone, string message)
            => SendTextAsync(phone, message);

        private async Task<bool> SendTextAsync(string phone, string message)
        {
            if (string.IsNullOrWhiteSpace(_accessToken)) return false;
            if (string.IsNullOrWhiteSpace(phone)) return false;

            // Zalo OA API mẫu: gửi SMS qua ZNS cần template; ở đây demo gửi OA message text tới user id tra cứu theo phone.
            var url = "https://openapi.zalo.me/v3.0/oa/message/cs";
            var payload = new
            {
                recipient = new { phone },
                message = new { text = message }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var res = await _http.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}

