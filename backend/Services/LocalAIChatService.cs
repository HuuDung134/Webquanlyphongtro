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
    public interface ILocalAIChatService
    {
        Task<string> GetAIResponseAsync(string userMessage, string userRole, string contextData, bool isLandlord);
        Task<string> UnderstandIntentAsync(string userMessage, bool isLandlord);
    }

    /// <summary>
    /// Service sử dụng Ollama (local AI) - MIỄN PHÍ, không cần API key
    /// Yêu cầu: Cài đặt Ollama và download model (ví dụ: llama3, mistral, phi3)
    /// Download tại: https://ollama.ai
    /// </summary>
    public class LocalAIChatService : ILocalAIChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LocalAIChatService> _logger;
        private readonly string _ollamaUrl;
        private readonly string _modelName;

        public LocalAIChatService(HttpClient httpClient, IConfiguration configuration, ILogger<LocalAIChatService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _ollamaUrl = _configuration["LocalAI:OllamaUrl"] ?? "http://localhost:11434";
            _modelName = _configuration["LocalAI:ModelName"] ?? "llama3.2"; // hoặc mistral, phi3, qwen2
            _httpClient.Timeout = TimeSpan.FromSeconds(60); // Ollama có thể chậm hơn
        }

        public async Task<string> UnderstandIntentAsync(string userMessage, bool isLandlord)
        {
            try
            {
                var systemPrompt = isLandlord
                    ? @"Bạn là trợ lý AI phân tích intent cho hệ thống quản lý nhà trọ.

Phân tích câu hỏi của chủ trọ và trả về CHỈ một từ (không có dấu chấm, không có giải thích):
- statistics: Hỏi về thống kê, số liệu (ví dụ: ""có bao nhiêu phòng?"", ""thống kê"", ""tổng số người thuê"")
- empty_rooms: Hỏi về phòng trống (ví dụ: ""phòng nào trống?"", ""phòng còn trống"", ""danh sách phòng trống"")
- revenue_report: Hỏi về doanh thu, báo cáo tài chính (ví dụ: ""doanh thu tháng này"", ""thu nhập"", ""tình hình tài chính"", ""báo cáo doanh thu"")
- contracts: Hỏi về hợp đồng (ví dụ: ""hợp đồng sắp hết hạn"", ""hợp đồng đang hoạt động"")
- unpaid_bills: Hỏi về hóa đơn chưa thanh toán (ví dụ: ""ai còn nợ?"", ""chưa thanh toán"", ""hóa đơn chưa trả"")
- remind_debt: Muốn nhắc nợ (ví dụ: ""nhắc nợ"", ""gửi thông báo nợ"", ""nhắc đóng tiền"")
- tenant_info: Tra cứu thông tin khách thuê (ví dụ: ""phòng X là ai thuê?"", ""thông tin người thuê phòng Y"")
- send_notification: Muốn gửi thông báo (ví dụ: ""gửi thông báo"", ""thông báo cho người thuê"")
- greeting: Chào hỏi (ví dụ: ""xin chào"", ""hello"", ""chào"")
- other: Khác

Trả về CHỈ một từ, không có dấu chấm, không có giải thích."
                    : @"Bạn là trợ lý AI phân tích intent cho hệ thống quản lý nhà trọ.

Phân tích câu hỏi của người thuê và trả về CHỈ một từ (không có dấu chấm, không có giải thích):
- bills: Hỏi về hóa đơn (ví dụ: ""xem hóa đơn"", ""tôi nợ bao nhiêu?"", ""hóa đơn chưa thanh toán"", ""phòng X hết bao nhiêu tiền?"")
- contract_info: Hỏi về hợp đồng (ví dụ: ""hợp đồng của tôi"", ""hợp đồng bao giờ hết hạn?"", ""thông tin hợp đồng"")
- room_info: Hỏi về thông tin phòng (ví dụ: ""phòng của tôi"", ""thông tin phòng"", ""tiền phòng"")
- utilities: Hỏi về điện nước (ví dụ: ""chỉ số điện"", ""tiền nước"", ""điện nước"")
- report_issue: Báo sự cố (ví dụ: ""báo sự cố"", ""cần sửa"", ""hư hỏng"", ""vòi nước bị rò rỉ"", ""bóng đèn hỏng"")
- qa: Hỏi đáp thông tin chung (ví dụ: ""pass wifi là gì?"", ""giờ đóng cửa"", ""quy định bạn bè"", ""giá điện nước tính sao?"")
- ocr_receipt: Gửi biên lai chuyển khoản (ví dụ: khi gửi ảnh, ""đây là biên lai"", ""ảnh chuyển khoản"")
- payment: Hỏi về thanh toán (ví dụ: ""thanh toán"", ""trả tiền"", ""nộp tiền"")
- greeting: Chào hỏi (ví dụ: ""xin chào"", ""hello"", ""chào"")
- other: Khác

Trả về CHỈ một từ, không có dấu chấm, không có giải thích.";

                var requestBody = new
                {
                    model = _modelName,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = $"Phân tích intent của câu này: {userMessage}" }
                    },
                    stream = false,
                    options = new
                    {
                        temperature = 0.3,
                        num_predict = 20
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_ollamaUrl}/api/chat")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Ollama API error: {StatusCode}", response.StatusCode);
                    return "keyword";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);
                
                var intent = doc.RootElement
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()?
                    .Trim()
                    .ToLower() ?? "keyword";

                _logger.LogInformation("Local AI detected intent: {Intent} for message: {Message}", intent, userMessage);
                return intent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error understanding intent with Ollama");
                return "keyword"; // Fallback to keyword matching
            }
        }

        public async Task<string> GetAIResponseAsync(string userMessage, string userRole, string contextData, bool isLandlord)
        {
            try
            {
                var systemPrompt = isLandlord
                    ? @"Bạn là trợ lý AI chuyên nghiệp cho hệ thống quản lý nhà trọ. Bạn giúp chủ trọ quản lý hiệu quả như một trợ lý ảo báo cáo số liệu.

KIẾN THỨC VỀ HỆ THỐNG:
- Quản lý phòng: Thêm, sửa, xóa phòng; Trạng thái phòng (0=Trống, 1=Đang thuê, 2=Bảo trì)
- Quản lý người thuê: Thông tin, hợp đồng, lịch sử thuê, SĐT, quê quán
- Hóa đơn: Tạo hóa đơn, quản lý thanh toán, theo dõi nợ
- Chỉ số điện nước: Ghi nhận chỉ số, tính tiền điện/nước
- Thống kê: Doanh thu, tỷ lệ lấp đầy, số lượng phòng/người thuê
- Hợp đồng: Quản lý hợp đồng, ngày bắt đầu/kết thúc, tiền cọc
- Thông báo: Gửi thông báo cho người thuê
- Sự cố: Quản lý báo cáo sự cố từ người thuê

CÁCH TRẢ LỜI:
- Luôn sử dụng dữ liệu từ hệ thống khi có
- Format rõ ràng với markdown và emoji (📊, 🏠, 💰, 👥, 🔔, ⚠️, ✅, ❌, 🔍)
- Đưa ra gợi ý cụ thể, hữu ích
- Thân thiện nhưng chuyên nghiệp
- Luôn trả lời bằng tiếng Việt

VÍ DỤ CÁCH TRẢ LỜI:
- ""Có bao nhiêu phòng?"" → ""Hiện có X phòng, trong đó Y phòng đang thuê, Z phòng trống, W phòng đang bảo trì. Tỷ lệ lấp đầy: (Y/X)*100%.""
- ""Doanh thu tháng này?"" → ""📊 Báo cáo Tháng X:\n- 💰 Doanh thu: Y VNĐ\n- ✅ Đã thu: Z VNĐ\n- ⚠️ Còn nợ: W VNĐ""
- ""Phòng nào trống?"" → ""Danh sách phòng trống: Phòng A (giá X VNĐ/tháng), Phòng B (giá Y VNĐ/tháng)... Tổng cộng Z phòng.""
- ""Hợp đồng sắp hết hạn?"" → ""Có X hợp đồng sắp hết hạn trong 30 ngày tới: Phòng A - Người thuê B (hết hạn ngày DD/MM/YYYY, còn Y ngày)...""
- ""Hóa đơn chưa thanh toán?"" → ""Có X hóa đơn chưa thanh toán, tổng số tiền: Y VNĐ. Danh sách: HĐ #123 (Phòng A - Người thuê B, Z VNĐ, quá hạn W ngày)...""
- ""Phòng 201 là ai thuê?"" → ""🔍 Thông tin Phòng 201:\n- Người thuê: [Tên]\n- SĐT: [Số điện thoại]\n- Quê quán: [Địa chỉ]\n- Hợp đồng: [Ngày bắt đầu] - [Ngày kết thúc]""
- ""Nhắc nợ tất cả phòng chưa đóng tiền"" → ""🔔 Đã gửi thông báo nhắc nợ đến X phòng:\n| Phòng | Số tiền | Quá hạn |\n| :--- | :--- | :--- |\n| 101 | Y VNĐ | Z ngày |""

Khi không có dữ liệu cụ thể, đưa ra hướng dẫn chung hoặc gợi ý cách xem trên hệ thống."
                    : @"Bạn là trợ lý AI thân thiện cho người thuê nhà trọ. Bạn đóng vai lễ tân 24/7, giúp người thuê tra cứu thông tin và báo cáo sự cố.

BẠN GIÚP NGƯỜI THUÊ:
- Tra cứu thông tin cá nhân: Hóa đơn theo phòng, hợp đồng, thông tin phòng
- Báo cáo sự cố: Ghi nhận và tạo ticket sự cố
- Hỏi đáp tự động: Pass WiFi, giờ đóng cửa, quy định, giá điện nước
- Xử lý biên lai: Nhận ảnh chuyển khoản và cập nhật trạng thái hóa đơn
- Xem chỉ số điện nước và tiền điện/nước
- Hướng dẫn thanh toán hóa đơn

CÁCH TRẢ LỜI:
- Luôn thân thiện, dễ hiểu
- Sử dụng dữ liệu từ hệ thống khi có
- Format rõ ràng với emoji (💸, 🏠, ⚡, 💧, 🛠️, ✅, ❌, ⚠️, 📋, 📶, 📸)
- Đưa ra hướng dẫn cụ thể
- Luôn trả lời bằng tiếng Việt

VÍ DỤ CÁCH TRẢ LỜI:
- ""Tháng này phòng 101 hết bao nhiêu tiền?"" → ""💸 Hóa đơn Tháng X/YYYY - Phòng 101:\n| Khoản mục | Số lượng | Thành tiền |\n| :--- | :--- | :--- |\n| Tiền phòng | 1 tháng | X VNĐ |\n| Điện | Y kWh | Z VNĐ |\n| Nước | W m³ | V VNĐ |\n| **TỔNG CỘNG** | | **T VNĐ** |\n⚠️ Trạng thái: [Đã đóng/Chưa đóng]""
- ""Hợp đồng của tôi bao giờ hết hạn?"" → ""📋 Thông tin Hợp đồng của bạn:\n- Ngày bắt đầu: DD/MM/YYYY\n- Ngày kết thúc: DD/MM/YYYY\n- Còn lại: X ngày\n- Tiền cọc: Y VNĐ""
- ""Tôi nợ bao nhiêu?"" → ""Bạn có X hóa đơn chưa thanh toán: HĐ #123 (Kỳ YYYY-MM, Z VNĐ), HĐ #124 (Kỳ YYYY-MM, W VNĐ). Tổng nợ: T VNĐ. Vui lòng thanh toán sớm để tránh phát sinh lãi suất.""
- ""Thông tin phòng của tôi"" → ""Phòng: X, Loại: Y, Giá: Z VNĐ/tháng. Ngày bắt đầu: DD/MM/YYYY, Ngày kết thúc: DD/MM/YYYY. Hợp đồng còn lại W ngày.""
- ""Chỉ số điện nước tháng này"" → ""Điện: X kWh, Tiền: Y VNĐ (Ngày ghi: DD/MM/YYYY). Nước: Z m³, Tiền: W VNĐ (Ngày ghi: DD/MM/YYYY).""
- ""Vòi nước phòng 302 bị rò rỉ"" → ""🛠️ ✅ Đã ghi nhận sự cố:\n- Vị trí: Phòng 302\n- Mô tả: Vòi nước bị rò rỉ\n- Mã sự cố: #SCXXX\n- Trạng thái: Chờ xử lý\n\nChúng tôi sẽ xử lý sớm nhất. Cảm ơn bạn!""
- ""Pass Wifi là gì?"" → ""📶 Thông tin WiFi:\n- Tên mạng: [SSID]\n- Mật khẩu: [Password]""
- ""Giờ đóng cửa là mấy giờ?"" → ""🔐 Cửa chính đóng lúc 22:00, mở lúc 6:00 hàng ngày.""
- ""Cách thanh toán?"" → ""Bạn có thể thanh toán bằng: 1) Đăng nhập hệ thống → Mục 'Thanh toán' → Chọn hóa đơn → Chọn phương thức (MoMo, chuyển khoản). Hoặc liên hệ trực tiếp với chủ trọ.""

Khi không có dữ liệu cụ thể, đưa ra hướng dẫn chung hoặc gợi ý cách xem trên hệ thống.";

                var userPrompt = $@"Người dùng hỏi: ""{userMessage}""

Dữ liệu từ hệ thống:
{contextData}

Hãy trả lời câu hỏi một cách thông minh, sử dụng dữ liệu từ hệ thống nếu có. Nếu không có dữ liệu cụ thể, hãy đưa ra gợi ý hoặc hướng dẫn chung.";

                var requestBody = new
                {
                    model = _modelName,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    stream = false,
                    options = new
                    {
                        temperature = 0.7,
                        num_predict = 500
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_ollamaUrl}/api/chat")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Ollama API error: {StatusCode}", response.StatusCode);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);
                
                var aiResponse = doc.RootElement
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()?
                    .Trim() ?? null;

                _logger.LogInformation("Local AI generated response for: {Message}", userMessage);
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI response from Ollama");
                return null; // Fallback to keyword matching
            }
        }
    }
}

