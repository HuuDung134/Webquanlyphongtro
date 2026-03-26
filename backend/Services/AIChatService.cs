using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DoAnCoSo.Services
{
    public interface IAIChatService
    {
        Task<string> GetAIResponseAsync(string userMessage, string userRole, string contextData, bool isLandlord);
        Task<string> UnderstandIntentAsync(string userMessage, bool isLandlord);
    }

    public class AIChatService : IAIChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIChatService> _logger;
        private readonly string _apiKey;

        public AIChatService(HttpClient httpClient, IConfiguration configuration, ILogger<AIChatService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["OCRSettings:ApiKey"] ?? _configuration["OpenAI:ApiKey"] ?? "";
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<string> UnderstandIntentAsync(string userMessage, bool isLandlord)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    return "keyword"; // Fallback to keyword matching
                }

                var systemPrompt = isLandlord
                    ? @"Bạn là trợ lý AI cho hệ thống quản lý nhà trọ. Nhiệm vụ của bạn là phân tích câu hỏi của chủ trọ và xác định ý định (intent).

Các intent có thể:
- statistics: Hỏi về thống kê, số liệu (ví dụ: 'có bao nhiêu phòng?', 'thống kê', 'doanh thu')
- empty_rooms: Hỏi về phòng trống (ví dụ: 'phòng nào trống?', 'phòng còn trống')
- revenue: Hỏi về doanh thu (ví dụ: 'doanh thu tháng này', 'thu nhập')
- contracts: Hỏi về hợp đồng (ví dụ: 'hợp đồng sắp hết hạn', 'hợp đồng')
- unpaid_bills: Hỏi về hóa đơn chưa thanh toán (ví dụ: 'ai còn nợ?', 'chưa thanh toán')
- send_notification: Muốn gửi thông báo
- greeting: Chào hỏi
- other: Khác

Trả về CHỈ intent (ví dụ: 'statistics', 'empty_rooms', 'revenue')"
                    : @"Bạn là trợ lý AI cho hệ thống quản lý nhà trọ. Nhiệm vụ của bạn là phân tích câu hỏi của người thuê và xác định ý định (intent).

Các intent có thể:
- bills: Hỏi về hóa đơn (ví dụ: 'xem hóa đơn', 'tôi nợ bao nhiêu?', 'hóa đơn chưa thanh toán')
- room_info: Hỏi về thông tin phòng (ví dụ: 'phòng của tôi', 'thông tin phòng', 'tiền phòng')
- utilities: Hỏi về điện nước (ví dụ: 'chỉ số điện', 'tiền nước', 'điện nước')
- report_issue: Báo sự cố (ví dụ: 'báo sự cố', 'cần sửa', 'hư hỏng')
- payment: Hỏi về thanh toán (ví dụ: 'thanh toán', 'trả tiền', 'nộp tiền')
- greeting: Chào hỏi
- other: Khác

Trả về CHỈ intent (ví dụ: 'bills', 'room_info', 'utilities')";

                var messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                };

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = messages,
                    temperature = 0.3,
                    max_tokens = 50
                };

                var json = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenAI API error: {StatusCode}", response.StatusCode);
                    return "keyword";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);
                
                var intent = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()?
                    .Trim()
                    .ToLower() ?? "keyword";

                _logger.LogInformation("AI detected intent: {Intent} for message: {Message}", intent, userMessage);
                return intent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error understanding intent");
                return "keyword"; // Fallback to keyword matching
            }
        }

        public async Task<string> GetAIResponseAsync(string userMessage, string userRole, string contextData, bool isLandlord)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    return null; // Fallback to keyword matching
                }

                var systemPrompt = isLandlord
                    ? @"Bạn là trợ lý AI thông minh cho hệ thống quản lý nhà trọ. Bạn giúp chủ trọ quản lý hiệu quả.

Nhiệm vụ của bạn:
1. Hiểu câu hỏi của chủ trọ
2. Sử dụng dữ liệu từ hệ thống (context) để trả lời chính xác
3. Đưa ra gợi ý và lời khuyên hữu ích
4. Trả lời bằng tiếng Việt, thân thiện và chuyên nghiệp
5. Sử dụng emoji phù hợp để làm rõ thông tin

Khi có dữ liệu từ hệ thống, hãy sử dụng nó để trả lời. Nếu không có dữ liệu, hãy đưa ra gợi ý chung.

Format: Sử dụng markdown, emoji, và cấu trúc rõ ràng."
                    : @"Bạn là trợ lý AI thân thiện cho hệ thống quản lý nhà trọ. Bạn giúp người thuê hiểu rõ về tình trạng phòng, hóa đơn và các vấn đề liên quan.

Nhiệm vụ của bạn:
1. Hiểu câu hỏi của người thuê
2. Sử dụng dữ liệu từ hệ thống (context) để trả lời chính xác
3. Đưa ra hướng dẫn rõ ràng và hữu ích
4. Trả lời bằng tiếng Việt, thân thiện và dễ hiểu
5. Sử dụng emoji phù hợp

Khi có dữ liệu từ hệ thống, hãy sử dụng nó để trả lời. Nếu không có dữ liệu, hãy đưa ra hướng dẫn chung.

Format: Sử dụng markdown, emoji, và cấu trúc rõ ràng.";

                var userPrompt = $@"Người dùng hỏi: ""{userMessage}""

Dữ liệu từ hệ thống:
{contextData}

Hãy trả lời câu hỏi một cách thông minh, sử dụng dữ liệu từ hệ thống nếu có. Nếu không có dữ liệu cụ thể, hãy đưa ra gợi ý hoặc hướng dẫn chung.";

                var messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                };

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 500
                };

                var json = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenAI API error: {StatusCode}", response.StatusCode);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);
                
                var aiResponse = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()?
                    .Trim() ?? null;

                _logger.LogInformation("AI generated response for: {Message}", userMessage);
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI response");
                return null; // Fallback to keyword matching
            }
        }
    }
}

