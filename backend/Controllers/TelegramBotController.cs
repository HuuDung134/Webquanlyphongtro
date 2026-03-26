using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using DoAnCoSo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DoAnCoSo.Services;

namespace DoAnCoSo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelegramBotController : ControllerBase
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TelegramBotController> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public TelegramBotController(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<TelegramBotController> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            
            var botToken = configuration["Telegram:BotToken"];
            if (!string.IsNullOrEmpty(botToken))
            {
                _botClient = new TelegramBotClient(botToken);
            }
        }

        // Gửi tin nhắn từ web đến Telegram
        [HttpPost("send-message")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                if (_botClient == null)
                {
                    return BadRequest(new { message = "Telegram Bot chưa được cấu hình" });
                }

                if (string.IsNullOrEmpty(request.ChatId) || string.IsNullOrEmpty(request.Message))
                {
                    return BadRequest(new { message = "ChatId và Message là bắt buộc" });
                }

                if (!long.TryParse(request.ChatId, out long chatId))
                {
                    return BadRequest(new { message = "ChatId không hợp lệ" });
                }

                await _botClient.SendMessage(
                    chatId: chatId,
                    text: request.Message
                );

                return Ok(new { success = true, message = "Tin nhắn đã được gửi thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi tin nhắn Telegram");
                return StatusCode(500, new { message = "Lỗi khi gửi tin nhắn: " + ex.Message });
            }
        }

        // Lấy thông tin bot
        [HttpGet("bot-info")]
        public async Task<IActionResult> GetBotInfo()
        {
            try
            {
                if (_botClient == null)
                {
                    return Ok(new { 
                        configured = false, 
                        message = "Telegram Bot chưa được cấu hình" 
                    });
                }

                var me = await _botClient.GetMe();
                return Ok(new
                {
                    configured = true,
                    username = me.Username,
                    firstName = me.FirstName,
                    id = me.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin bot");
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin bot" });
            }
        }

        // Xử lý tin nhắn từ web interface với Ollama AI
        [HttpPost("process-message")]
        [Authorize]
        public async Task<IActionResult> ProcessMessage([FromBody] ProcessMessageRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Message))
                {
                    return BadRequest(new { response = "Tin nhắn không được để trống" });
                }

                // Lấy thông tin user từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { response = "Không thể xác định người dùng" });
                }

                var user = await _context.Users
                    .Include(u => u.NguoiThue)
                    .FirstOrDefaultAsync(u => u.MaNguoiDung == userId);

                if (user == null)
                {
                    return Unauthorized(new { response = "Người dùng không tồn tại" });
                }

                var isLandlord = user.VaiTro == "Admin" || user.VaiTro == "admin";
                var messageText = request.Message;
                string response = "";

                // Sử dụng Ollama AI nếu được bật
                var enableLocalAI = _configuration.GetValue<bool>("LocalAI:EnableLocalAI", false);
                
                if (enableLocalAI)
                {
                    using var aiScope = _serviceScopeFactory.CreateScope();
                    try
                    {
                        var aiService = aiScope.ServiceProvider.GetRequiredService<ILocalAIChatService>();
                        
                        // Lấy context data
                        var contextData = await GetContextDataAsync(user, isLandlord);
                        
                        // Hiểu intent
                        var intent = await aiService.UnderstandIntentAsync(messageText, isLandlord);
                        
                        // Nếu AI hiểu được intent, generate response
                        if (intent != "keyword" && intent != "other" && intent != "greeting")
                        {
                            var dataForIntent = await GetDataForIntentAsync(intent, user, isLandlord);
                            var aiResponse = await aiService.GetAIResponseAsync(
                                messageText,
                                user.VaiTro,
                                dataForIntent,
                                isLandlord
                            );
                            
                            if (!string.IsNullOrEmpty(aiResponse))
                            {
                                return Ok(new { response = aiResponse });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Lỗi khi sử dụng Ollama AI, fallback về keyword matching");
                    }
                }

                // Fallback về keyword matching
                response = await ProcessMessageWithKeywordMatching(messageText, user, isLandlord);

                return Ok(new { response = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tin nhắn");
                return StatusCode(500, new { 
                    response = "Xin lỗi, có lỗi xảy ra. Vui lòng thử lại sau." 
                });
            }
        }

        private async Task<string> ProcessMessageWithKeywordMatching(string messageText, Models.User user, bool isLandlord)
        {
            var command = messageText.ToLower().Trim();

            if (command.Contains("/start") || command.Contains("/help") || 
                command.Contains("xin chào") || command.Contains("hello"))
            {
                if (isLandlord)
                {
                    return @"🤖 *Chào mừng Chủ trọ!*

Bạn có thể sử dụng các nút trên bàn phím hoặc hỏi tôi:
📊 *Thống kê* - ""Xem thống kê"", ""Có bao nhiêu phòng?"", ""Doanh thu thế nào?""
🔔 *Gửi thông báo* - ""Gửi thông báo cho người thuê""
🏠 *Phòng trống* - ""Phòng nào còn trống?"", ""Danh sách phòng""

Hoặc hỏi tôi bất cứ điều gì về hệ thống!";
                }
                else
                {
                    return @"🤖 *Chào mừng Người thuê!*

Bạn có thể sử dụng các nút trên bàn phím hoặc hỏi tôi:
💸 *Hóa đơn* - ""Xem hóa đơn"", ""Hóa đơn chưa thanh toán"", ""Tôi nợ bao nhiêu?""
🛠 *Báo sự cố* - ""Báo sự cố"", ""Có vấn đề với..."", ""Cần sửa...""
📊 *Thông tin phòng* - ""Thông tin phòng của tôi"", ""Tiền phòng""

Hoặc hỏi tôi bất cứ điều gì!";
                }
            }

            // Xử lý các lệnh khác tương tự như TelegramBotService
            if (command.Contains("thống kê") || command.Contains("thong ke"))
            {
                var totalRooms = await _context.Phong.CountAsync();
                var occupiedRooms = await _context.Phong.CountAsync(p => p.TrangThai == 1);
                var totalTenants = await _context.NguoiThue.CountAsync();
                var totalRevenue = await _context.HoaDon.SumAsync(h => (decimal?)h.TongTien) ?? 0;

                return $"📊 *Thống kê hệ thống:*\n\n" +
                       $"🏠 Tổng số phòng: {totalRooms}\n" +
                       $"🔑 Phòng đang thuê: {occupiedRooms}\n" +
                       $"👥 Tổng người thuê: {totalTenants}\n" +
                       $"💰 Tổng doanh thu: {totalRevenue:N0} ₫";
            }

            return "👋 Cảm ơn bạn đã liên hệ! Tôi đã nhận được tin nhắn của bạn. Vui lòng thử lại với câu hỏi cụ thể hơn hoặc sử dụng các nút bên trên.";
        }

        private async Task<string> GetContextDataAsync(Models.User user, bool isLandlord)
        {
            if (isLandlord)
            {
                var totalRooms = await _context.Phong.CountAsync();
                var occupiedRooms = await _context.Phong.CountAsync(p => p.TrangThai == 1);
                var totalTenants = await _context.NguoiThue.CountAsync();
                var totalRevenue = await _context.HoaDon.SumAsync(h => (decimal?)h.TongTien) ?? 0;
                return $"Tổng số phòng: {totalRooms}, Đang thuê: {occupiedRooms}, Tổng người thuê: {totalTenants}, Tổng doanh thu: {totalRevenue:N0} VNĐ";
            }
            else
            {
                if (user.NguoiThue == null) return "Không có thông tin người thuê";
                var tenantId = user.NguoiThue.MaNguoiThue;
                var bills = await _context.HoaDon
                    .Include(h => h.Phong)
                    .Where(h => h.MaNguoiThue == tenantId)
                    .OrderByDescending(h => h.NgayLap)
                    .Take(3)
                    .Select(h => new { h.TongTien, IsPaid = _context.ThanhToan.Any(t => t.MaHoaDon == h.MaHoaDon && t.TongTien >= h.TongTien) })
                    .ToListAsync();
                var unpaidTotal = bills.Where(b => !b.IsPaid).Sum(b => b.TongTien);
                return $"Hóa đơn gần nhất: {bills.Count} hóa đơn, Tổng nợ: {unpaidTotal:N0} VNĐ";
            }
        }

        private async Task<string> GetDataForIntentAsync(string intent, Models.User user, bool isLandlord)
        {
            // Tương tự như trong TelegramBotService
            return await GetContextDataAsync(user, isLandlord);
        }

        // Lấy thống kê nhanh cho bot
        [HttpGet("quick-stats")]
        [Authorize]
        public async Task<IActionResult> GetQuickStats()
        {
            try
            {
                var stats = new
                {
                    totalRooms = await _context.Phong.CountAsync(),
                    occupiedRooms = await _context.Phong.CountAsync(p => p.TrangThai == 1),
                    totalTenants = await _context.NguoiThue.CountAsync(),
                    totalRevenue = await _context.HoaDon.SumAsync(h => (decimal?)h.TongTien) ?? 0,
                    unpaidBills = await _context.HoaDon
                        .Where(h => !_context.ThanhToan
                            .Any(t => t.MaHoaDon == h.MaHoaDon && t.TongTien >= h.TongTien))
                        .CountAsync()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thống kê");
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê" });
            }
        }
    }

    public class SendMessageRequest
    {
        public string ChatId { get; set; }
        public string Message { get; set; }
    }

    public class ProcessMessageRequest
    {
        public string Message { get; set; }
    }
}

