using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DoAnCoSo.Data;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Models;

namespace DoAnCoSo.Services
{
    public class TelegramBotService : IHostedService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly string _botToken;

        public TelegramBotService(
            IConfiguration configuration,
            ILogger<TelegramBotService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _botToken = configuration["Telegram:BotToken"];
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;

            if (string.IsNullOrEmpty(_botToken))
            {
                _logger.LogWarning("Telegram Bot Token không được cấu hình. Bot sẽ không hoạt động.");
                _botClient = null!;
            }
            else
            {
                _botClient = new TelegramBotClient(_botToken);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_botClient == null || string.IsNullOrEmpty(_botToken))
            {
                _logger.LogWarning("Telegram Bot không được khởi động vì thiếu Bot Token.");
                return;
            }

            try
            {
                // Kiểm tra kết nối trước khi start receiving
                var me = await _botClient.GetMe(cancellationToken);
                _logger.LogInformation($"Telegram Bot @{me.Username} đã được khởi động thành công!");

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
                };

                _botClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    errorHandler: HandlePollingErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Không thể kết nối đến Telegram API. Bot sẽ không hoạt động. " +
                    "Nguyên nhân có thể: mạng không ổn định, firewall chặn, hoặc Bot Token không hợp lệ. " +
                    "Ứng dụng vẫn chạy bình thường, chỉ tính năng Telegram Bot bị tắt.");
                // Không throw exception để không làm crash app
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Telegram Bot đã dừng.");
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message is not { } message)
                    return;

                var chatId = message.Chat.Id;
                var messageText = message.Text;

                _logger.LogInformation($"Nhận tin nhắn từ {message.From?.FirstName} ({chatId}): {messageText}");

                // Lấy user từ database bằng TelegramChatId
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var user = await context.Users
                    .Include(u => u.NguoiThue)
                    .FirstOrDefaultAsync(u => u.TelegramChatId == chatId);

                // Nếu user chưa liên kết
                if (user == null)
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"⚠️ *Vui lòng liên kết tài khoản của bạn với Telegram Bot*\n\n" +
                              $"📱 *Chat ID của bạn:* `{chatId}`\n\n" +
                              $"Để liên kết:\n" +
                              $"1. Đăng nhập vào hệ thống\n" +
                              $"2. Vào phần 'Quản lý tài khoản'\n" +
                              $"3. Nhập Chat ID: *{chatId}*\n" +
                              $"4. Nhấn nút 'Liên kết'\n\n" +
                              $"Sau khi liên kết, bạn sẽ có thể sử dụng đầy đủ các tính năng của bot!",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                    return;
                }

                // Xác định vai trò: Admin = Landlord, NguoiDung = Tenant
                var isLandlord = user.VaiTro == "Admin" || user.VaiTro == "admin";

                // Xử lý callback query (khi click button)
                if (update.CallbackQuery != null)
                {
                    await HandleCallbackQueryAsync(botClient, update.CallbackQuery, user, isLandlord, cancellationToken);
                    return;
                }

                // Xử lý ảnh (Photo/Document) - OCR biên lai chuyển khoản
                if (message.Photo != null && message.Photo.Length > 0)
                {
                    await HandlePhotoMessageAsync(botClient, message, user, isLandlord, context, cancellationToken);
                    return;
                }

                if (message.Document != null)
                {
                    await HandleDocumentMessageAsync(botClient, message, user, isLandlord, context, cancellationToken);
                    return;
                }

                // Xử lý tin nhắn text
                if (!string.IsNullOrEmpty(messageText))
                {
                    var keyboard = isLandlord ? GetLandlordKeyboard() : GetTenantKeyboard();
                    
                    // Xử lý lệnh /start - tự động hiển thị keyboard và chào mừng
                    if (messageText.ToLower().Trim() == "/start" || messageText.ToLower().Trim() == "/help")
                    {
                        string welcomeMessage;
                        if (isLandlord)
                        {
                            welcomeMessage = @"🤖 *Chào mừng Chủ trọ!*

Bạn có thể sử dụng các nút trên bàn phím để:
📊 *Thống kê* - Xem thống kê hệ thống
🔔 *Gửi thông báo* - Gửi thông báo cho người thuê
🏠 *Phòng trống* - Xem danh sách phòng trống

Hoặc gửi tin nhắn để được hỗ trợ!";
                        }
                        else
                        {
                            welcomeMessage = @"🤖 *Chào mừng Người thuê!*

Bạn có thể sử dụng các nút trên bàn phím để:
💸 *Hóa đơn* - Xem hóa đơn của bạn
🛠 *Báo sự cố* - Báo cáo sự cố cần sửa chữa
🔑 *Mở cửa* - Mở cửa từ xa (đang phát triển)

Hoặc gửi tin nhắn để được hỗ trợ!";
                        }

                        await botClient.SendMessage(
                            chatId: chatId,
                            text: welcomeMessage,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                            replyMarkup: keyboard,
                            cancellationToken: cancellationToken);
                        return;
                    }

                    // Keyword detection cho Tenant
                    if (!isLandlord)
                    {
                        var keywords = new[] { "hư", "hỏng", "sửa", "broken", "repair", "fix" };
                        if (keywords.Any(k => messageText.ToLower().Contains(k)))
                        {
                            await botClient.SendMessage(
                                chatId: chatId,
                                text: "🔧 Phát hiện bạn đang báo sự cố!\n\n" +
                                      "Vui lòng sử dụng nút '🛠 Báo sự cố' để báo cáo chi tiết hơn.",
                                replyMarkup: keyboard,
                                cancellationToken: cancellationToken);
                            return;
                        }
                    }

                    // Kiểm tra nếu là button text
                    string response;
                    if (isLandlord)
                    {
                        // Kiểm tra nếu là button của Landlord
                        if (messageText == "📊 Thống kê" || messageText == "🔔 Gửi thông báo" || messageText == "🏠 Phòng trống")
                        {
                            response = await HandleLandlordButtonAsync(messageText, user);
                        }
                        else
                        {
                            response = await ProcessMessageAsync(messageText, chatId, user, isLandlord, context);
                        }
                    }
                    else
                    {
                        // Kiểm tra nếu là button của Tenant
                        if (messageText == "💸 Hóa đơn" || messageText == "🛠 Báo sự cố" || messageText == "🔑 Mở cửa")
                        {
                            response = await HandleTenantButtonAsync(messageText, user);
                        }
                        else
                        {
                            response = await ProcessMessageAsync(messageText, chatId, user, isLandlord, context);
                        }
                    }

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: response,
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken);

                    // Lưu lịch sử chat
                    await SaveChatHistoryAsync(chatId, user.MaNguoiDung, messageText, response, null, user.VaiTro, "text", context);
                }
                else
                {
                    // Nếu không phải tin nhắn text, vẫn hiển thị keyboard
                    var keyboard = isLandlord ? GetLandlordKeyboard() : GetTenantKeyboard();
                    var defaultResponse = "Vui lòng sử dụng các nút trên bàn phím hoặc gửi tin nhắn để được hỗ trợ.";
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: defaultResponse,
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tin nhắn Telegram");
            }
        }

        // Tạo keyboard cho Landlord (Chủ trọ)
        private ReplyKeyboardMarkup GetLandlordKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "📊 Thống kê", "🔔 Gửi thông báo" },
                new KeyboardButton[] { "🏠 Phòng trống" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        // Tạo keyboard cho Tenant (Người thuê)
        private ReplyKeyboardMarkup GetTenantKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "💸 Hóa đơn", "🛠 Báo sự cố" },
                new KeyboardButton[] { "🔑 Mở cửa" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        // Xử lý callback query (khi click button)
        private async Task HandleCallbackQueryAsync(
            ITelegramBotClient botClient,
            CallbackQuery callbackQuery,
            Models.User user,
            bool isLandlord,
            CancellationToken cancellationToken)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;

            // Telegram.Bot v22 uses non-Async method names that still return Task
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);

            // Xử lý logic dựa trên button được click
            string response = "";
            
            if (isLandlord)
            {
                response = await HandleLandlordButtonAsync(data, user);
            }
            else
            {
                response = await HandleTenantButtonAsync(data, user);
            }

            await botClient.SendMessage(
                chatId: chatId,
                text: response,
                replyMarkup: isLandlord ? GetLandlordKeyboard() : GetTenantKeyboard(),
                cancellationToken: cancellationToken);
        }

        // Xử lý button cho Landlord
        private async Task<string> HandleLandlordButtonAsync(string buttonData, Models.User user)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            switch (buttonData)
            {
                case "thongke":
                case "📊 Thống kê":
                    var totalRooms = await context.Phong.CountAsync();
                    var occupiedRooms = await context.Phong.CountAsync(p => p.TrangThai == 1);
                    var totalTenants = await context.NguoiThue.CountAsync();
                    var totalRevenue = await context.HoaDon.SumAsync(h => (decimal?)h.TongTien) ?? 0;
                    
                    return $"📊 *Thống kê hệ thống:*\n\n" +
                           $"🏠 Tổng số phòng: {totalRooms}\n" +
                           $"🔑 Phòng đang thuê: {occupiedRooms}\n" +
                           $"👥 Tổng người thuê: {totalTenants}\n" +
                           $"💰 Tổng doanh thu: {totalRevenue:N0} ₫";

                case "guithongbao":
                case "🔔 Gửi thông báo":
                    return "🔔 *Gửi thông báo*\n\n" +
                           "Vui lòng nhập nội dung thông báo bạn muốn gửi.\n" +
                           "Ví dụ: 'Cúp điện từ 8h-12h ngày mai'";

                case "phongtrong":
                case "🏠 Phòng trống":
                    var emptyRooms = await context.Phong
                        .Include(p => p.LoaiPhong)
                        .Where(p => p.TrangThai == 0)
                        .Take(10)
                        .Select(p => new { p.TenPhong, LoaiPhong = p.LoaiPhong.TenLoaiPhong, p.GiaPhong })
                        .ToListAsync();

                    if (!emptyRooms.Any())
                    {
                        return "🏠 Không có phòng trống nào trong hệ thống.";
                    }

                    var roomList = "🏠 *Danh sách phòng trống:*\n\n";
                    foreach (var room in emptyRooms)
                    {
                        roomList += $"• {room.TenPhong} ({room.LoaiPhong})\n" +
                                   $"  Giá: {room.GiaPhong:N0} ₫/tháng\n\n";
                    }
                    return roomList;

                default:
                    return "Lệnh không hợp lệ. Vui lòng chọn một trong các tùy chọn trên bàn phím.";
            }
        }

        // Xử lý button cho Tenant
        private async Task<string> HandleTenantButtonAsync(string buttonData, Models.User user)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            switch (buttonData)
            {
                case "hoadon":
                case "💸 Hóa đơn":
                    if (user.NguoiThue == null)
                    {
                        return "❌ Không tìm thấy thông tin người thuê của bạn.";
                    }

                    var bills = await context.HoaDon
                        .Include(h => h.Phong)
                        .Where(h => h.MaNguoiThue == user.NguoiThue.MaNguoiThue)
                        .OrderByDescending(h => h.NgayLap)
                        .Take(5)
                        .Select(h => new
                        {
                            h.MaHoaDon,
                            h.NgayLap,
                            h.TongTien,
                            RoomNumber = h.Phong.TenPhong,
                            h.KyHoaDon,
                            IsPaid = context.ThanhToan
                                .Any(t => t.MaHoaDon == h.MaHoaDon && t.TongTien >= h.TongTien)
                        })
                        .ToListAsync();

                    if (!bills.Any())
                    {
                        return "💸 Bạn chưa có hóa đơn nào.";
                    }

                    var billList = "💸 *Hóa đơn của bạn:*\n\n";
                    foreach (var bill in bills)
                    {
                        var status = bill.IsPaid ? "✅ Đã thanh toán" : "❌ Chưa thanh toán";
                        billList += $"• HĐ #{bill.MaHoaDon} - {bill.RoomNumber}\n" +
                                   $"  Kỳ: {bill.KyHoaDon}\n" +
                                   $"  Tổng: {bill.TongTien:N0} ₫\n" +
                                   $"  {status}\n\n";
                    }
                    return billList;

                case "baosuco":
                case "🛠 Báo sự cố":
                    return "🛠 *Báo sự cố*\n\n" +
                           "Vui lòng mô tả chi tiết sự cố bạn gặp phải:\n" +
                           "• Vị trí (phòng số, khu vực)\n" +
                           "• Loại sự cố (điện, nước, thiết bị...)\n" +
                           "• Mô tả chi tiết\n\n" +
                           "Ví dụ: 'Phòng 101, bóng đèn phòng khách bị cháy'";

                case "mocua":
                case "🔑 Mở cửa":
                    return "🔑 *Mở cửa từ xa*\n\n" +
                           "Tính năng này đang được phát triển.\n" +
                           "Vui lòng liên hệ trực tiếp với chủ trọ để được hỗ trợ.";

                default:
                    return "Lệnh không hợp lệ. Vui lòng chọn một trong các tùy chọn trên bàn phím.";
            }
        }

        private async Task<string> ProcessMessageAsync(string messageText, long chatId, Models.User user, bool isLandlord, ApplicationDbContext context)
        {
            var command = messageText.ToLower().Trim();
            var originalMessage = messageText;

            // Lệnh /start hoặc /help
            if (command == "/start" || command == "/help")
            {
                if (isLandlord)
                {
                    return @"🤖 *Chào mừng Chủ trọ!*

Bạn có thể sử dụng các nút trên bàn phím hoặc hỏi tôi:
📊 *Thống kê* - ""Xem thống kê"", ""Có bao nhiêu phòng?"", ""Doanh thu thế nào?""
🔔 *Gửi thông báo* - ""Gửi thông báo cho người thuê""
🏠 *Phòng trống* - ""Phòng nào còn trống?"", ""Danh sách phòng""
💰 *Doanh thu* - ""Doanh thu tháng này"", ""Tổng doanh thu""
📋 *Hợp đồng* - ""Hợp đồng sắp hết hạn"", ""Hợp đồng đang hoạt động""

Hoặc hỏi tôi bất cứ điều gì về hệ thống!";
                }
                else
                {
                    return @"🤖 *Chào mừng Người thuê!*

Bạn có thể sử dụng các nút trên bàn phím hoặc hỏi tôi:
💸 *Hóa đơn* - ""Xem hóa đơn"", ""Hóa đơn chưa thanh toán"", ""Tôi nợ bao nhiêu?""
🛠 *Báo sự cố* - ""Báo sự cố"", ""Có vấn đề với..."", ""Cần sửa...""
📊 *Thông tin phòng* - ""Thông tin phòng của tôi"", ""Tiền phòng""
💡 *Tiện ích* - ""Tiền điện"", ""Tiền nước"", ""Chỉ số điện nước""

Hoặc hỏi tôi bất cứ điều gì!";
                }
            }

            // Lệnh /thongke
            if (command == "/thongke")
            {
                try
                {
                    var totalRooms = await context.Phong.CountAsync();
                    var occupiedRooms = await context.Phong.CountAsync(p => p.TrangThai == 1);
                    var totalTenants = await context.NguoiThue.CountAsync();
                    var totalRevenue = await context.HoaDon.SumAsync(h => (decimal?)h.TongTien) ?? 0;

                    return $"📊 *Thống kê hệ thống:*\n\n" +
                           $"🏠 Tổng số phòng: {totalRooms}\n" +
                           $"🔑 Phòng đang thuê: {occupiedRooms}\n" +
                           $"👥 Tổng người thuê: {totalTenants}\n" +
                           $"💰 Tổng doanh thu: {totalRevenue:N0} ₫";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lấy thống kê");
                    return "❌ Có lỗi xảy ra khi lấy thống kê. Vui lòng thử lại sau.";
                }
            }

            // Lệnh /phong
            if (command == "/phong")
            {
                try
                {
                    var rooms = await context.Phong
                        .Include(p => p.LoaiPhong)
                        .Take(10)
                        .Select(p => new
                        {
                            p.TenPhong,
                            p.TrangThai,
                            LoaiPhong = p.LoaiPhong.TenLoaiPhong
                        })
                        .ToListAsync();

                    if (!rooms.Any())
                    {
                        return "📋 Chưa có phòng nào trong hệ thống.";
                    }

                    var roomList = "🏠 *Danh sách phòng:*\n\n";
                    foreach (var room in rooms)
                    {
                        var status = room.TrangThai == 0 ? "🟢 Trống" : 
                                    room.TrangThai == 1 ? "🔵 Đang thuê" : "🟡 Bảo trì";
                        roomList += $"• {room.TenPhong} ({room.LoaiPhong}) - {status}\n";
                    }

                    return roomList;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lấy danh sách phòng");
                    return "❌ Có lỗi xảy ra khi lấy danh sách phòng.";
                }
            }

            // Sử dụng Ollama (Local AI) để hiểu intent và generate response thông minh
            try
            {
                var enableLocalAI = _configuration.GetValue<bool>("LocalAI:EnableLocalAI", false);
                
                if (enableLocalAI)
                {
                    using var aiScope = _serviceScopeFactory.CreateScope();
                    
                    try
                    {
                        var localAIService = aiScope.ServiceProvider.GetRequiredService<ILocalAIChatService>();
                        
                        // Lấy context data từ database
                        var contextData = await GetContextDataAsync(user, isLandlord, context);
                        
                        // Lấy lịch sử chat gần đây để AI có context tốt hơn
                        var chatHistory = await GetRecentChatHistoryAsync(chatId, context);
                        if (!string.IsNullOrEmpty(chatHistory))
                        {
                            contextData = $"{chatHistory}\n\n{contextData}";
                        }
                        
                        // Sử dụng AI để hiểu intent
                        string intent;
                        try
                        {
                            intent = await localAIService.UnderstandIntentAsync(originalMessage, isLandlord);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error understanding intent, using keyword matching");
                            intent = "keyword";
                        }
                        
                        // Nếu AI detect được intent rõ ràng, lấy data và generate response
                        if (intent != "keyword" && intent != "other" && intent != "greeting")
                        {
                            var dataForIntent = await GetDataForIntentAsync(intent, user, isLandlord, context, originalMessage);
                            string aiResponse;
                            try
                            {
                                aiResponse = await localAIService.GetAIResponseAsync(
                                    originalMessage, 
                                    user.VaiTro, 
                                    dataForIntent, 
                                    isLandlord
                                );
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error generating AI response, using keyword matching");
                                aiResponse = null;
                            }
                            
                            if (!string.IsNullOrEmpty(aiResponse))
                            {
                                _logger.LogInformation("Ollama AI generated response for intent: {Intent}", intent);
                                // Lưu lịch sử chat với intent
                                await SaveChatHistoryAsync(chatId, user.MaNguoiDung, originalMessage, aiResponse, intent, user.VaiTro, "text", context);
                                return aiResponse;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Ollama AI service not available, using keyword matching");
                    }
                }
                
                // Fallback: Sử dụng keyword matching nếu AI không available hoặc không hiểu
                if (!isLandlord)
                {
                    return await ProcessTenantIntentAsync(originalMessage, command, user, context);
                }
                else
                {
                    return await ProcessLandlordIntentAsync(originalMessage, command, user, context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi sử dụng AI, fallback về keyword matching");
                // Fallback về keyword matching
                if (!isLandlord)
                {
                    return await ProcessTenantIntentAsync(originalMessage, command, user, context);
                }
                else
                {
                    return await ProcessLandlordIntentAsync(originalMessage, command, user, context);
                }
            }
        }

        // Lấy context data từ database để cung cấp cho AI
        private async Task<string> GetContextDataAsync(Models.User user, bool isLandlord, ApplicationDbContext context)
        {
            try
            {
                if (isLandlord)
                {
                    var totalRooms = await context.Phong.CountAsync();
                    var occupiedRooms = await context.Phong.CountAsync(p => p.TrangThai == 1);
                    var emptyRooms = await context.Phong.CountAsync(p => p.TrangThai == 0);
                    var totalTenants = await context.NguoiThue.CountAsync();
                    var totalRevenue = await context.HoaDon.SumAsync(h => (decimal?)h.TongTien) ?? 0;
                    var currentMonth = DateTime.Now.Month;
                    var currentYear = DateTime.Now.Year;
                    var monthlyRevenue = await context.HoaDon
                        .Where(h => h.NgayLap.Month == currentMonth && h.NgayLap.Year == currentYear)
                        .SumAsync(h => (decimal?)h.TongTien) ?? 0;

                    return $"Tổng số phòng: {totalRooms}, Đang thuê: {occupiedRooms}, Trống: {emptyRooms}, " +
                           $"Tổng người thuê: {totalTenants}, Tổng doanh thu: {totalRevenue:N0} VNĐ, " +
                           $"Doanh thu tháng {currentMonth}/{currentYear}: {monthlyRevenue:N0} VNĐ";
                }
                else
                {
                    if (user.NguoiThue == null)
                    {
                        return "Không có thông tin người thuê";
                    }

                    var tenantId = user.NguoiThue.MaNguoiThue;
                    var bills = await context.HoaDon
                        .Include(h => h.Phong)
                        .Where(h => h.MaNguoiThue == tenantId)
                        .OrderByDescending(h => h.NgayLap)
                        .Take(3)
                        .Select(h => new
                        {
                            h.MaHoaDon,
                            h.TongTien,
                            RoomNumber = h.Phong.TenPhong,
                            h.KyHoaDon,
                            IsPaid = context.ThanhToan.Any(t => t.MaHoaDon == h.MaHoaDon && t.TongTien >= h.TongTien)
                        })
                        .ToListAsync();

                    var unpaidTotal = bills.Where(b => !b.IsPaid).Sum(b => b.TongTien);
                    var contract = await context.HopDong
                        .Include(h => h.Phong)
                        .Where(h => h.MaNguoiThue == tenantId && h.NgayKetThuc >= DateTime.Now)
                        .FirstOrDefaultAsync();

                    return $"Hóa đơn gần nhất: {bills.Count} hóa đơn, " +
                           $"Tổng nợ: {unpaidTotal:N0} VNĐ, " +
                           $"Phòng: {(contract?.Phong.TenPhong ?? "Chưa có")}, " +
                           $"Hợp đồng hết hạn: {(contract?.NgayKetThuc.HasValue == true ? contract.NgayKetThuc.Value.ToString("dd/MM/yyyy") : "N/A")}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy context data");
                return "Không thể lấy dữ liệu từ hệ thống";
            }
        }

        // Lấy data chi tiết cho từng intent
        private async Task<string> GetDataForIntentAsync(string intent, Models.User user, bool isLandlord, ApplicationDbContext context, string originalMessage = "")
        {
            try
            {
                if (isLandlord)
                {
                    switch (intent)
                    {
                        case "statistics":
                            var stats = await GetLandlordStatisticsAsync(context);
                            return stats;
                        case "empty_rooms":
                            var rooms = await GetEmptyRoomsDataAsync(context);
                            return rooms;
                        case "revenue":
                        case "revenue_report":
                            var revenue = await GetRevenueDataAsync(context);
                            return revenue;
                        case "contracts":
                            var contracts = await GetContractsDataAsync(context);
                            return contracts;
                        case "unpaid_bills":
                            var unpaid = await GetUnpaidBillsDataAsync(context);
                            return unpaid;
                        case "tenant_info":
                            // Intent này sẽ được xử lý trong message để extract phòng số
                            return await GetContextDataAsync(user, isLandlord, context);
                        default:
                            return await GetContextDataAsync(user, isLandlord, context);
                    }
                }
                else
                {
                    if (user.NguoiThue == null) return "Không có thông tin người thuê";
                    var tenantId = user.NguoiThue.MaNguoiThue;

                    switch (intent)
                    {
                        case "bills":
                            var bills = await GetTenantBillsDataAsync(tenantId, context);
                            return bills;
                        case "contract_info":
                            var contractInfo = await GetTenantContractInfoDataAsync(tenantId, context);
                            return contractInfo;
                        case "room_info":
                            var roomInfo = await GetTenantRoomInfoDataAsync(tenantId, context);
                            return roomInfo;
                        case "utilities":
                            var utilities = await GetTenantUtilitiesDataAsync(tenantId, context);
                            return utilities;
                        case "report_issue":
                            // Intent này sẽ được xử lý riêng để tạo record SuCo
                            return await GetContextDataAsync(user, isLandlord, context);
                        case "qa":
                            // Q&A sẽ được xử lý bởi AI với context data
                            return await GetContextDataAsync(user, isLandlord, context);
                        default:
                            return await GetContextDataAsync(user, isLandlord, context);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy data cho intent: {Intent}", intent);
                return await GetContextDataAsync(user, isLandlord, context);
            }
        }

        // Helper methods để lấy data chi tiết
        private async Task<string> GetLandlordStatisticsAsync(ApplicationDbContext context)
        {
            var totalRooms = await context.Phong.CountAsync();
            var occupiedRooms = await context.Phong.CountAsync(p => p.TrangThai == 1);
            var emptyRooms = await context.Phong.CountAsync(p => p.TrangThai == 0);
            var totalTenants = await context.NguoiThue.CountAsync();
            var totalRevenue = await context.HoaDon.SumAsync(h => (decimal?)h.TongTien) ?? 0;
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = await context.HoaDon
                .Where(h => h.NgayLap.Month == currentMonth && h.NgayLap.Year == currentYear)
                .SumAsync(h => (decimal?)h.TongTien) ?? 0;

            return $"Thống kê hệ thống:\n" +
                   $"- Tổng số phòng: {totalRooms}\n" +
                   $"- Phòng đang thuê: {occupiedRooms}\n" +
                   $"- Phòng trống: {emptyRooms}\n" +
                   $"- Tổng người thuê: {totalTenants}\n" +
                   $"- Tổng doanh thu: {totalRevenue:N0} VNĐ\n" +
                   $"- Doanh thu tháng {currentMonth}/{currentYear}: {monthlyRevenue:N0} VNĐ";
        }

        private async Task<string> GetEmptyRoomsDataAsync(ApplicationDbContext context)
        {
            var emptyRooms = await context.Phong
                .Include(p => p.LoaiPhong)
                .Where(p => p.TrangThai == 0)
                .Select(p => new { p.TenPhong, LoaiPhong = p.LoaiPhong.TenLoaiPhong, p.GiaPhong })
                .ToListAsync();

            if (!emptyRooms.Any())
            {
                return "Không có phòng nào còn trống";
            }

            var result = $"Danh sách phòng trống ({emptyRooms.Count} phòng):\n";
            foreach (var room in emptyRooms)
            {
                result += $"- {room.TenPhong} ({room.LoaiPhong})";
                result += $" - {room.GiaPhong:N0} VNĐ/tháng";
                result += "\n";
            }
            return result;
        }

        private async Task<string> GetRevenueDataAsync(ApplicationDbContext context)
        {
            var totalRevenue = await context.HoaDon.SumAsync(h => (decimal?)h.TongTien) ?? 0;
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = await context.HoaDon
                .Where(h => h.NgayLap.Month == currentMonth && h.NgayLap.Year == currentYear)
                .SumAsync(h => (decimal?)h.TongTien) ?? 0;

            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;
            var lastMonthRevenue = await context.HoaDon
                .Where(h => h.NgayLap.Month == lastMonth && h.NgayLap.Year == lastMonthYear)
                .SumAsync(h => (decimal?)h.TongTien) ?? 0;

            return $"Doanh thu:\n" +
                   $"- Tổng doanh thu: {totalRevenue:N0} VNĐ\n" +
                   $"- Tháng {currentMonth}/{currentYear}: {monthlyRevenue:N0} VNĐ\n" +
                   $"- Tháng {lastMonth}/{lastMonthYear}: {lastMonthRevenue:N0} VNĐ";
        }

        private async Task<string> GetContractsDataAsync(ApplicationDbContext context)
        {
            var today = DateTime.Now.Date;
            var expiringContracts = await context.HopDong
                .Include(h => h.Phong)
                .Include(h => h.NguoiThue)
                .Where(h => h.NgayKetThuc >= today && h.NgayKetThuc <= today.AddDays(30))
                .OrderBy(h => h.NgayKetThuc)
                .Select(h => new
                {
                    RoomNumber = h.Phong.TenPhong,
                    TenantName = h.NguoiThue.HoTen,
                    h.NgayKetThuc
                })
                .ToListAsync();

            if (!expiringContracts.Any())
            {
                return "Không có hợp đồng nào sắp hết hạn trong 30 ngày tới";
            }

            var result = $"Hợp đồng sắp hết hạn ({expiringContracts.Count} hợp đồng):\n";
            foreach (var contract in expiringContracts)
            {
                if (contract.NgayKetThuc.HasValue)
                {
                    var daysLeft = (contract.NgayKetThuc.Value - today).Days;
                    result += $"- Phòng {contract.RoomNumber} - {contract.TenantName}: Hết hạn {contract.NgayKetThuc.Value:dd/MM/yyyy} (Còn {daysLeft} ngày)\n";
                }
            }
            return result;
        }

        private async Task<string> GetUnpaidBillsDataAsync(ApplicationDbContext context)
        {
            var unpaidBills = await context.HoaDon
                .Include(h => h.Phong)
                .Include(h => h.NguoiThue)
                .Where(h => !context.ThanhToan.Any(t => t.MaHoaDon == h.MaHoaDon && t.TongTien >= h.TongTien))
                .OrderByDescending(h => h.NgayLap)
                .Take(10)
                .Select(h => new
                {
                    h.MaHoaDon,
                    h.TongTien,
                    RoomNumber = h.Phong.TenPhong,
                    TenantName = h.NguoiThue.HoTen,
                    h.KyHoaDon,
                    h.NgayLap
                })
                .ToListAsync();

            if (!unpaidBills.Any())
            {
                return "Tất cả hóa đơn đã được thanh toán đầy đủ";
            }

            var totalUnpaid = unpaidBills.Sum(b => b.TongTien);
            var result = $"Hóa đơn chưa thanh toán ({unpaidBills.Count} hóa đơn, Tổng: {totalUnpaid:N0} VNĐ):\n";
            foreach (var bill in unpaidBills)
            {
                var daysOverdue = (DateTime.Now - bill.NgayLap).Days;
                result += $"- HĐ #{bill.MaHoaDon}: {bill.RoomNumber} - {bill.TenantName}, Kỳ: {bill.KyHoaDon}, Số tiền: {bill.TongTien:N0} VNĐ";
                if (daysOverdue > 0)
                {
                    result += $", Quá hạn {daysOverdue} ngày";
                }
                result += "\n";
            }
            return result;
        }

        private async Task<string> GetTenantBillsDataAsync(int tenantId, ApplicationDbContext context)
        {
            var bills = await context.HoaDon
                .Include(h => h.Phong)
                .Where(h => h.MaNguoiThue == tenantId)
                .OrderByDescending(h => h.NgayLap)
                .Take(5)
                .Select(h => new
                {
                    h.MaHoaDon,
                    h.NgayLap,
                    h.TongTien,
                    RoomNumber = h.Phong.TenPhong,
                    h.KyHoaDon,
                    IsPaid = context.ThanhToan.Any(t => t.MaHoaDon == h.MaHoaDon && t.TongTien >= h.TongTien)
                })
                .ToListAsync();

            if (!bills.Any())
            {
                return "Bạn chưa có hóa đơn nào";
            }

            var unpaidBills = bills.Where(b => !b.IsPaid).ToList();
            var totalUnpaid = unpaidBills.Sum(b => b.TongTien);

            var result = $"Hóa đơn của bạn ({bills.Count} hóa đơn):\n";
            foreach (var bill in bills)
            {
                var status = bill.IsPaid ? "Đã thanh toán" : "Chưa thanh toán";
                result += $"- HĐ #{bill.MaHoaDon} - {bill.RoomNumber}, Kỳ: {bill.KyHoaDon}, {bill.TongTien:N0} VNĐ - {status}\n";
            }
            if (unpaidBills.Any())
            {
                result += $"\nTổng nợ: {totalUnpaid:N0} VNĐ ({unpaidBills.Count} hóa đơn chưa thanh toán)";
            }
            return result;
        }

        private async Task<string> GetTenantRoomInfoDataAsync(int tenantId, ApplicationDbContext context)
        {
            var contract = await context.HopDong
                .Include(h => h.Phong)
                .Include(h => h.Phong.LoaiPhong)
                .Where(h => h.MaNguoiThue == tenantId && h.NgayKetThuc >= DateTime.Now)
                .OrderByDescending(h => h.NgayBatDau)
                .FirstOrDefaultAsync();

            if (contract == null)
            {
                return "Không tìm thấy hợp đồng đang hoạt động";
            }

            var daysLeft = contract.NgayKetThuc.HasValue ? (contract.NgayKetThuc.Value - DateTime.Now).Days : 0;
            return $"Thông tin phòng:\n" +
                   $"- Phòng: {contract.Phong.TenPhong}\n" +
                   $"- Loại: {contract.Phong.LoaiPhong.TenLoaiPhong}\n" +
                   $"- Tiền phòng: {contract.Phong.GiaPhong:N0} VNĐ/tháng\n" +
                   $"- Ngày bắt đầu: {contract.NgayBatDau:dd/MM/yyyy}\n" +
                   $"{(contract.NgayKetThuc.HasValue ? $"- Ngày kết thúc: {contract.NgayKetThuc.Value:dd/MM/yyyy}\n" : "")}" +
                   $"{(daysLeft > 0 ? $"- Còn lại: {daysLeft} ngày" : "")}";
        }

        private async Task<string> GetTenantUtilitiesDataAsync(int tenantId, ApplicationDbContext context)
        {
            var contract = await context.HopDong
                .Where(h => h.MaNguoiThue == tenantId)
                .OrderByDescending(h => h.NgayBatDau)
                .FirstOrDefaultAsync();

            if (contract == null)
            {
                return "Không tìm thấy thông tin phòng";
            }

            var roomId = contract.MaPhong;
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var electricity = await context.ChiSoDien
                .Where(c => c.MaPhong == roomId && c.NgayThangDien.Month == currentMonth && c.NgayThangDien.Year == currentYear)
                .OrderByDescending(c => c.NgayThangDien)
                .FirstOrDefaultAsync();

            var water = await context.ChiSoNuoc
                .Where(c => c.MaPhong == roomId && c.NgayThangNuoc.Month == currentMonth && c.NgayThangNuoc.Year == currentYear)
                .OrderByDescending(c => c.NgayThangNuoc)
                .FirstOrDefaultAsync();

            var result = "Chỉ số điện nước tháng này:\n";
            if (electricity != null)
            {
                result += $"- Điện: {electricity.SoDienTieuThu} kWh, Tiền: {electricity.TienDien:N0} VNĐ (Ngày: {electricity.NgayThangDien:dd/MM/yyyy})\n";
            }
            else
            {
                result += "- Điện: Chưa có chỉ số\n";
            }

            if (water != null)
            {
                result += $"- Nước: {water.SoNuocTieuThu} m³, Tiền: {water.TienNuoc:N0} VNĐ (Ngày: {water.NgayThangNuoc:dd/MM/yyyy})";
            }
            else
            {
                result += "- Nước: Chưa có chỉ số";
            }

            return result;
        }

        private async Task<string> GetTenantContractInfoDataAsync(int tenantId, ApplicationDbContext context)
        {
            try
            {
                var contract = await context.HopDong
                    .Include(h => h.Phong)
                    .Where(h => h.MaNguoiThue == tenantId && (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now))
                    .OrderByDescending(h => h.NgayBatDau)
                    .FirstOrDefaultAsync();

                if (contract == null)
                {
                    // Thử tìm hợp đồng gần nhất (kể cả đã hết hạn)
                    var latestContract = await context.HopDong
                        .Include(h => h.Phong)
                        .Where(h => h.MaNguoiThue == tenantId)
                        .OrderByDescending(h => h.NgayBatDau)
                        .FirstOrDefaultAsync();

                    if (latestContract == null)
                    {
                        return "Không tìm thấy hợp đồng nào trong hệ thống";
                    }

                    // Trả về thông tin hợp đồng gần nhất (có thể đã hết hạn)
                    var daysLeft = latestContract.NgayKetThuc.HasValue ? (latestContract.NgayKetThuc.Value - DateTime.Now).Days : 0;
                    var roomName = latestContract.Phong?.TenPhong ?? $"Phòng #{latestContract.MaPhong}";
                    
                    return $"📋 **Thông tin Hợp đồng:**\n" +
                           $"- Ngày bắt đầu: {latestContract.NgayBatDau:dd/MM/yyyy}\n" +
                           $"{(latestContract.NgayKetThuc.HasValue ? $"- Ngày kết thúc: {latestContract.NgayKetThuc.Value:dd/MM/yyyy}\n" : "- Ngày kết thúc: Chưa xác định\n")}" +
                           $"{(daysLeft != 0 ? $"- Còn lại: {daysLeft} ngày\n" : "")}" +
                           $"- Tiền cọc: {latestContract.TienCoc:N0} VNĐ\n" +
                           $"- Phòng: {roomName}\n" +
                           $"{(daysLeft < 0 ? "⚠️ Hợp đồng đã hết hạn" : "")}";
                }

                var contractDaysLeft = contract.NgayKetThuc.HasValue ? (contract.NgayKetThuc.Value - DateTime.Now).Days : 0;
                var roomName2 = contract.Phong?.TenPhong ?? $"Phòng #{contract.MaPhong}";
                
                return $"📋 **Thông tin Hợp đồng:**\n" +
                       $"- Ngày bắt đầu: {contract.NgayBatDau:dd/MM/yyyy}\n" +
                       $"{(contract.NgayKetThuc.HasValue ? $"- Ngày kết thúc: {contract.NgayKetThuc.Value:dd/MM/yyyy}\n" : "- Ngày kết thúc: Chưa xác định\n")}" +
                       $"{(contractDaysLeft > 0 ? $"- Còn lại: {contractDaysLeft} ngày\n" : "")}" +
                       $"- Tiền cọc: {contract.TienCoc:N0} VNĐ\n" +
                       $"- Phòng: {roomName2}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin hợp đồng cho tenant {TenantId}", tenantId);
                return "❌ Có lỗi xảy ra khi tải thông tin hợp đồng. Vui lòng thử lại sau hoặc liên hệ chủ trọ.";
            }
        }

        // Helper method để extract số phòng từ message
        private string ExtractRoomNumber(string message)
        {
            // Tìm pattern "phòng XXX" hoặc "phong XXX" hoặc "P.XXX" hoặc "PXXX"
            var patterns = new[] { @"phòng\s+(\d+)", @"phong\s+(\d+)", @"P\.(\d+)", @"P(\d+)" };
            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }
            return null;
        }

        // Helper method để extract tiêu đề sự cố từ message
        private string ExtractIssueTitle(string message)
        {
            // Tìm các từ khóa phổ biến về sự cố
            var keywords = new[] { "rò rỉ", "ro ri", "chảy", "cháy", "chay", "hỏng", "hong", "hư", "hu", "sự cố", "su co" };
            foreach (var keyword in keywords)
            {
                if (message.ToLower().Contains(keyword))
                {
                    // Lấy phần câu chứa từ khóa và 20 ký tự xung quanh
                    var index = message.ToLower().IndexOf(keyword);
                    var start = Math.Max(0, index - 20);
                    var length = Math.Min(message.Length - start, 60);
                    var title = message.Substring(start, length).Trim();
                    if (title.Length > 50) title = title.Substring(0, 50) + "...";
                    return title;
                }
            }
            // Nếu không tìm thấy từ khóa, lấy 50 ký tự đầu
            return message.Length > 50 ? message.Substring(0, 50) + "..." : message;
        }

        // Xử lý intent cho Tenant
        private async Task<string> ProcessTenantIntentAsync(string originalMessage, string command, Models.User user, ApplicationDbContext context)
        {
            try
            {
                if (user.NguoiThue == null)
                {
                    return "❌ Không tìm thấy thông tin người thuê của bạn. Vui lòng liên hệ chủ trọ.";
                }

                var tenantId = user.NguoiThue.MaNguoiThue;

                // Intent: Xem hóa đơn
                if (command.Contains("hóa đơn") || command.Contains("hoa don") || command.Contains("hoadon") ||
                    command.Contains("tiền phòng") || command.Contains("tien phong") ||
                    command.Contains("nợ") || command.Contains("no") || command.Contains("chưa thanh toán"))
                {
                    var bills = await context.HoaDon
                        .Include(h => h.Phong)
                        .Where(h => h.MaNguoiThue == tenantId)
                        .OrderByDescending(h => h.NgayLap)
                        .Take(5)
                        .Select(h => new
                        {
                            h.MaHoaDon,
                            h.NgayLap,
                            h.TongTien,
                            RoomNumber = h.Phong.TenPhong,
                            h.KyHoaDon,
                            IsPaid = context.ThanhToan
                                .Any(t => t.MaHoaDon == h.MaHoaDon && t.TongTien >= h.TongTien)
                        })
                        .ToListAsync();

                    if (!bills.Any())
                    {
                        return "💸 Bạn chưa có hóa đơn nào trong hệ thống.";
                    }

                    var unpaidBills = bills.Where(b => !b.IsPaid).ToList();
                    var totalUnpaid = unpaidBills.Sum(b => b.TongTien);

                    var billList = "💸 *Hóa đơn của bạn:*\n\n";
                    foreach (var bill in bills)
                    {
                        var status = bill.IsPaid ? "✅ Đã thanh toán" : "❌ Chưa thanh toán";
                        billList += $"• HĐ #{bill.MaHoaDon} - {bill.RoomNumber}\n" +
                                   $"  Kỳ: {bill.KyHoaDon}\n" +
                                   $"  Tổng: {bill.TongTien:N0} ₫\n" +
                                   $"  {status}\n\n";
                    }

                    if (unpaidBills.Any())
                    {
                        billList += $"⚠️ *Tổng nợ:* {totalUnpaid:N0} ₫\n";
                        billList += $"📋 *Số hóa đơn chưa thanh toán:* {unpaidBills.Count}\n\n";
                        billList += "💡 *Gợi ý:* Vui lòng thanh toán sớm để tránh phát sinh lãi suất.";
                    }

                    return billList;
                }

                // Intent: Xem thông tin hợp đồng
                if (command.Contains("hợp đồng") || command.Contains("hop dong") || 
                    command.Contains("hợp đồng của tôi") || command.Contains("hop dong cua toi") ||
                    command.Contains("hết hạn") || command.Contains("het han"))
                {
                    try
                    {
                        var contract = await context.HopDong
                            .Include(h => h.Phong)
                            .Where(h => h.MaNguoiThue == tenantId && (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now))
                            .OrderByDescending(h => h.NgayBatDau)
                            .FirstOrDefaultAsync();

                        if (contract == null)
                        {
                            // Thử tìm hợp đồng gần nhất
                            contract = await context.HopDong
                                .Include(h => h.Phong)
                                .Where(h => h.MaNguoiThue == tenantId)
                                .OrderByDescending(h => h.NgayBatDau)
                                .FirstOrDefaultAsync();

                            if (contract == null)
                            {
                                return "❌ Không tìm thấy hợp đồng nào trong hệ thống.";
                            }
                        }

                        var daysLeft = contract.NgayKetThuc.HasValue ? (contract.NgayKetThuc.Value - DateTime.Now).Days : 0;
                        var roomName = contract.Phong?.TenPhong ?? $"Phòng #{contract.MaPhong}";
                        var roomPrice = contract.Phong?.GiaPhong ?? 0;

                        var contractInfo = $"📋 *Thông tin Hợp đồng của bạn:*\n\n" +
                                          $"📅 Ngày bắt đầu: {contract.NgayBatDau:dd/MM/yyyy}\n" +
                                          $"{(contract.NgayKetThuc.HasValue ? $"📅 Ngày kết thúc: {contract.NgayKetThuc.Value:dd/MM/yyyy}\n" : "📅 Ngày kết thúc: Chưa xác định\n")}" +
                                          $"{(daysLeft > 0 ? $"⏰ Còn lại: {daysLeft} ngày\n" : daysLeft < 0 ? $"⚠️ Đã hết hạn: {Math.Abs(daysLeft)} ngày\n" : "")}" +
                                          $"💰 Tiền cọc: {contract.TienCoc:N0} VNĐ\n" +
                                          $"🏠 Phòng: {roomName}\n" +
                                          $"{(roomPrice > 0 ? $"💵 Giá phòng: {roomPrice:N0} VNĐ/tháng\n" : "")}";

                        if (daysLeft > 0 && daysLeft <= 30)
                        {
                            contractInfo += $"\n⚠️ *Cảnh báo:* Hợp đồng sẽ hết hạn sau {daysLeft} ngày!";
                        }
                        else if (daysLeft < 0)
                        {
                            contractInfo += $"\n⚠️ *Lưu ý:* Hợp đồng đã hết hạn. Vui lòng liên hệ chủ trọ để gia hạn.";
                        }

                        return contractInfo;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi lấy thông tin hợp đồng cho tenant {TenantId}", tenantId);
                        return "❌ Có lỗi xảy ra khi tải thông tin hợp đồng. Vui lòng thử lại sau hoặc liên hệ chủ trọ.";
                    }
                }

                // Intent: Xem thông tin phòng
                if (command.Contains("phòng") || command.Contains("phong") || command.Contains("thông tin phòng") ||
                    command.Contains("thong tin phong") || command.Contains("phòng của tôi"))
                {
                    try
                    {
                        var contract = await context.HopDong
                            .Include(h => h.Phong)
                            .ThenInclude(p => p.LoaiPhong)
                            .Where(h => h.MaNguoiThue == tenantId && (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now))
                            .OrderByDescending(h => h.NgayBatDau)
                            .FirstOrDefaultAsync();

                        if (contract == null)
                        {
                            return "❌ Không tìm thấy hợp đồng đang hoạt động của bạn.";
                        }

                        var roomName = contract.Phong?.TenPhong ?? $"Phòng #{contract.MaPhong}";
                        var roomType = contract.Phong?.LoaiPhong?.TenLoaiPhong ?? "Chưa xác định";
                        var roomPrice = contract.Phong?.GiaPhong ?? 0;

                        var roomInfo = $"🏠 *Thông tin phòng của bạn:*\n\n" +
                                      $"📋 Phòng: *{roomName}*\n" +
                                      $"🏷️ Loại: {roomType}\n" +
                                      $"📅 Ngày bắt đầu: {contract.NgayBatDau:dd/MM/yyyy}\n" +
                                      $"{(contract.NgayKetThuc.HasValue ? $"📅 Ngày kết thúc: {contract.NgayKetThuc.Value:dd/MM/yyyy}\n" : "")}" +
                                      $"{(roomPrice > 0 ? $"💰 Tiền phòng: {roomPrice:N0} ₫/tháng\n" : "")}";

                        var daysLeft = contract.NgayKetThuc.HasValue ? (contract.NgayKetThuc.Value - DateTime.Now).Days : 0;
                        if (daysLeft > 0 && daysLeft <= 30)
                        {
                            roomInfo += $"\n⚠️ Hợp đồng sẽ hết hạn sau *{daysLeft}* ngày!";
                        }

                        return roomInfo;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi lấy thông tin phòng cho tenant {TenantId}", tenantId);
                        return "❌ Có lỗi xảy ra khi tải thông tin phòng. Vui lòng thử lại sau.";
                    }
                }

                // Intent: Xem chỉ số điện nước
                if (command.Contains("điện") || command.Contains("dien") || command.Contains("nước") || 
                    command.Contains("nuoc") || command.Contains("chỉ số") || command.Contains("chi so") ||
                    command.Contains("tiền điện") || command.Contains("tien dien") ||
                    command.Contains("tiền nước") || command.Contains("tien nuoc"))
                {
                    var contract = await context.HopDong
                        .Include(h => h.Phong)
                        .Where(h => h.MaNguoiThue == tenantId)
                        .OrderByDescending(h => h.NgayBatDau)
                        .FirstOrDefaultAsync();

                    if (contract == null)
                    {
                        return "❌ Không tìm thấy thông tin phòng của bạn.";
                    }

                    var roomId = contract.MaPhong;
                    var currentMonth = DateTime.Now.Month;
                    var currentYear = DateTime.Now.Year;

                    var electricity = await context.ChiSoDien
                        .Where(c => c.MaPhong == roomId && 
                                   c.NgayThangDien.Month == currentMonth && 
                                   c.NgayThangDien.Year == currentYear)
                        .OrderByDescending(c => c.NgayThangDien)
                        .FirstOrDefaultAsync();

                    var water = await context.ChiSoNuoc
                        .Where(c => c.MaPhong == roomId && 
                                   c.NgayThangNuoc.Month == currentMonth && 
                                   c.NgayThangNuoc.Year == currentYear)
                        .OrderByDescending(c => c.NgayThangNuoc)
                        .FirstOrDefaultAsync();

                    var info = "⚡💧 *Chỉ số điện nước tháng này:*\n\n";

                    if (electricity != null)
                    {
                        info += $"⚡ *Điện:*\n" +
                               $"  Chỉ số: {electricity.SoDienTieuThu} kWh\n" +
                               $"  Tiền điện: {electricity.TienDien:N0} ₫\n" +
                               $"  Ngày ghi: {electricity.NgayThangDien:dd/MM/yyyy}\n\n";
                    }
                    else
                    {
                        info += "⚡ *Điện:* Chưa có chỉ số tháng này\n\n";
                    }

                    if (water != null)
                    {
                        info += $"💧 *Nước:*\n" +
                               $"  Chỉ số: {water.SoNuocTieuThu} m³\n" +
                               $"  Tiền nước: {water.TienNuoc:N0} ₫\n" +
                               $"  Ngày ghi: {water.NgayThangNuoc:dd/MM/yyyy}\n";
                    }
                    else
                    {
                        info += "💧 *Nước:* Chưa có chỉ số tháng này";
                    }

                    return info;
                }

                // Intent: Báo sự cố
                if (command.Contains("báo") || command.Contains("bao") || command.Contains("sự cố") ||
                    command.Contains("su co") || command.Contains("hư") || command.Contains("hỏng") ||
                    command.Contains("sửa") || command.Contains("sua") || command.Contains("vấn đề") ||
                    command.Contains("van de") || command.Contains("cần sửa") || command.Contains("can sua") ||
                    command.Contains("rò rỉ") || command.Contains("ro ri") || command.Contains("chảy") ||
                    command.Contains("cháy") || command.Contains("chay") || command.Contains("hỏng"))
                {
                    // Lấy thông tin phòng của người thuê
                    var contract = await context.HopDong
                        .Include(h => h.Phong)
                        .Where(h => h.MaNguoiThue == tenantId && (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now))
                        .OrderByDescending(h => h.NgayBatDau)
                        .FirstOrDefaultAsync();

                    if (contract == null)
                    {
                        return "❌ Không tìm thấy thông tin phòng của bạn. Vui lòng liên hệ chủ trọ.";
                    }

                    // Tạo record SuCo
                    var suCo = new Models.SuCo
                    {
                        MaNguoiThue = tenantId,
                        MaPhong = contract.MaPhong,
                        TieuDe = ExtractIssueTitle(originalMessage),
                        MoTa = originalMessage,
                        NgayBaoCao = DateTime.Now,
                        TrangThai = "Chờ xử lý"
                    };

                    context.SuCo.Add(suCo);
                    await context.SaveChangesAsync();

                    return $"🛠️ ✅ *Đã ghi nhận sự cố của bạn:*\n\n" +
                           $"📋 Mã sự cố: #SC{suCo.MaSuCo:D4}\n" +
                           $"📍 Vị trí: {contract.Phong.TenPhong}\n" +
                           $"📝 Mô tả: {suCo.TieuDe}\n" +
                           $"⏰ Thời gian: {suCo.NgayBaoCao:dd/MM/yyyy HH:mm}\n" +
                           $"📊 Trạng thái: {suCo.TrangThai}\n\n" +
                           $"Chúng tôi sẽ xử lý sớm nhất có thể. Cảm ơn bạn đã báo tin!";
                }

                // Intent: Thanh toán
                if (command.Contains("thanh toán") || command.Contains("thanh toan") || 
                    command.Contains("trả tiền") || command.Contains("tra tien") ||
                    command.Contains("nộp tiền") || command.Contains("nop tien"))
                {
                    var unpaidBills = await context.HoaDon
                        .Include(h => h.Phong)
                        .Where(h => h.MaNguoiThue == tenantId && 
                                   !context.ThanhToan.Any(t => t.MaHoaDon == h.MaHoaDon && t.TongTien >= h.TongTien))
                        .OrderByDescending(h => h.NgayLap)
                        .Take(5)
                        .Select(h => new
                        {
                            h.MaHoaDon,
                            h.TongTien,
                            RoomNumber = h.Phong.TenPhong,
                            h.KyHoaDon
                        })
                        .ToListAsync();

                    if (!unpaidBills.Any())
                    {
                        return "✅ Bạn không có hóa đơn nào cần thanh toán. Cảm ơn bạn!";
                    }

                    var total = unpaidBills.Sum(b => b.TongTien);
                    var response = "💳 *Thanh toán hóa đơn:*\n\n";
                    response += $"💰 *Tổng cần thanh toán:* {total:N0} ₫\n\n";
                    response += "📋 *Danh sách hóa đơn:*\n";
                    foreach (var bill in unpaidBills)
                    {
                        response += $"• HĐ #{bill.MaHoaDon} - {bill.RoomNumber}\n";
                        response += $"  Kỳ: {bill.KyHoaDon}\n";
                        response += $"  Số tiền: {bill.TongTien:N0} ₫\n\n";
                    }
                    response += "💡 *Cách thanh toán:*\n";
                    response += "1. Đăng nhập vào hệ thống\n";
                    response += "2. Vào mục 'Thanh toán'\n";
                    response += "3. Chọn hóa đơn cần thanh toán\n";
                    response += "4. Chọn phương thức thanh toán (MoMo, chuyển khoản...)\n\n";
                    response += "Hoặc liên hệ trực tiếp với chủ trọ để được hỗ trợ!";

                    return response;
                }

                // Intent: Chào hỏi
                if (command.Contains("xin chào") || command.Contains("chào") || command.Contains("chao") ||
                    command.Contains("hello") || command.Contains("hi") || command.Contains("hey"))
                {
                    return "👋 Xin chào! Tôi là bot hỗ trợ của hệ thống quản lý nhà trọ.\n\n" +
                           "Bạn có thể hỏi tôi về:\n" +
                           "• 💸 Hóa đơn và thanh toán\n" +
                           "• 🏠 Thông tin phòng\n" +
                           "• ⚡💧 Chỉ số điện nước\n" +
                           "• 🛠 Báo sự cố\n\n" +
                           "Hoặc sử dụng các nút trên bàn phím!";
                }

                // Intent: Cảm ơn
                if (command.Contains("cảm ơn") || command.Contains("cam on") || command.Contains("thanks") ||
                    command.Contains("thank you"))
                {
                    return "😊 Không có gì! Nếu bạn cần hỗ trợ thêm, đừng ngại hỏi tôi nhé!";
                }

                // Default response với gợi ý
                return "🤔 Tôi chưa hiểu rõ câu hỏi của bạn. Bạn có thể:\n\n" +
                       "• 💸 Hỏi về hóa đơn: \"Xem hóa đơn\", \"Tôi nợ bao nhiêu?\"\n" +
                       "• 🏠 Hỏi về phòng: \"Thông tin phòng của tôi\"\n" +
                       "• ⚡💧 Hỏi về điện nước: \"Chỉ số điện\", \"Tiền nước\"\n" +
                       "• 🛠 Báo sự cố: \"Báo sự cố\", \"Cần sửa...\"\n\n" +
                       "Hoặc sử dụng các nút trên bàn phím để được hỗ trợ nhanh hơn!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý intent cho Tenant");
                return "❌ Có lỗi xảy ra. Vui lòng thử lại sau hoặc liên hệ trực tiếp với chủ trọ.";
            }
        }

        // Xử lý intent cho Landlord
        private async Task<string> ProcessLandlordIntentAsync(string originalMessage, string command, Models.User user, ApplicationDbContext context)
        {
            try
            {
                // Intent: Thống kê tổng quan
                if (command.Contains("thống kê") || command.Contains("thong ke") || command.Contains("statistics") ||
                    command.Contains("có bao nhiêu") || command.Contains("co bao nhieu") ||
                    command.Contains("tổng số") || command.Contains("tong so") ||
                    command.Contains("bao nhiêu phòng") || command.Contains("bao nhieu phong"))
                {
                    var totalRooms = await context.Phong.CountAsync();
                    var occupiedRooms = await context.Phong.CountAsync(p => p.TrangThai == 1);
                    var emptyRooms = await context.Phong.CountAsync(p => p.TrangThai == 0);
                    var maintenanceRooms = await context.Phong.CountAsync(p => p.TrangThai == 2);
                    var totalTenants = await context.NguoiThue.CountAsync();
                    var totalRevenue = await context.HoaDon.SumAsync(h => (decimal?)h.TongTien) ?? 0;
                    var activeContracts = await context.HopDong.CountAsync(h => h.NgayKetThuc >= DateTime.Now);

                    var currentMonth = DateTime.Now.Month;
                    var currentYear = DateTime.Now.Year;
                    var monthlyRevenue = await context.HoaDon
                        .Where(h => h.NgayLap.Month == currentMonth && h.NgayLap.Year == currentYear)
                        .SumAsync(h => (decimal?)h.TongTien) ?? 0;

                    return $"📊 *Thống kê hệ thống:*\n\n" +
                           $"🏠 *Phòng:*\n" +
                           $"  • Tổng số: {totalRooms}\n" +
                           $"  • Đang thuê: {occupiedRooms} ({(totalRooms > 0 ? (occupiedRooms * 100 / totalRooms) : 0)}%)\n" +
                           $"  • Còn trống: {emptyRooms}\n" +
                           $"  • Bảo trì: {maintenanceRooms}\n\n" +
                           $"👥 *Người thuê:*\n" +
                           $"  • Tổng số: {totalTenants}\n" +
                           $"  • Hợp đồng đang hoạt động: {activeContracts}\n\n" +
                           $"💰 *Doanh thu:*\n" +
                           $"  • Tổng doanh thu: {totalRevenue:N0} ₫\n" +
                           $"  • Doanh thu tháng này: {monthlyRevenue:N0} ₫\n\n" +
                           $"💡 *Tỷ lệ lấp đầy:* {(totalRooms > 0 ? (occupiedRooms * 100.0 / totalRooms) : 0):F1}%";
                }

                // Intent: Xem phòng trống
                if (command.Contains("phòng trống") || command.Contains("phong trong") ||
                    command.Contains("phòng nào trống") || command.Contains("phong nao trong") ||
                    command.Contains("còn trống") || command.Contains("con trong") ||
                    command.Contains("danh sách phòng") || command.Contains("danh sach phong"))
                {
                    var emptyRooms = await context.Phong
                        .Include(p => p.LoaiPhong)
                        .Where(p => p.TrangThai == 0)
                        .Select(p => new
                        {
                            p.TenPhong,
                            LoaiPhong = p.LoaiPhong.TenLoaiPhong,
                            p.GiaPhong
                        })
                        .ToListAsync();

                    if (!emptyRooms.Any())
                    {
                        return "🏠 *Phòng trống:*\n\n" +
                               "❌ Hiện tại không có phòng nào còn trống.\n\n" +
                               "💡 *Gợi ý:* Bạn có thể kiểm tra các phòng đang bảo trì hoặc liên hệ với người thuê để gia hạn hợp đồng.";
                    }

                    var response = $"🏠 *Danh sách phòng trống:* ({emptyRooms.Count} phòng)\n\n";
                    foreach (var room in emptyRooms)
                    {
                        response += $"• {room.TenPhong} ({room.LoaiPhong})\n";
                        response += $"  Giá: {room.GiaPhong:N0} ₫/tháng\n";
                        response += "\n";
                    }

                    response += "💡 *Gợi ý:* Có thể đăng tin cho thuê hoặc cập nhật thông tin phòng để thu hút người thuê mới!";
                    return response;
                }

                // Intent: Doanh thu
                if (command.Contains("doanh thu") || command.Contains("doanh thu") ||
                    command.Contains("thu nhập") || command.Contains("thu nhap") ||
                    command.Contains("tiền thu được") || command.Contains("tien thu duoc"))
                {
                    var totalRevenue = await context.HoaDon.SumAsync(h => (decimal?)h.TongTien) ?? 0;
                    var currentMonth = DateTime.Now.Month;
                    var currentYear = DateTime.Now.Year;
                    var monthlyRevenue = await context.HoaDon
                        .Where(h => h.NgayLap.Month == currentMonth && h.NgayLap.Year == currentYear)
                        .SumAsync(h => (decimal?)h.TongTien) ?? 0;

                    var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
                    var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;
                    var lastMonthRevenue = await context.HoaDon
                        .Where(h => h.NgayLap.Month == lastMonth && h.NgayLap.Year == lastMonthYear)
                        .SumAsync(h => (decimal?)h.TongTien) ?? 0;

                    var unpaidBills = await context.HoaDon
                        .Where(h => !context.ThanhToan.Any(t => t.MaHoaDon == h.MaHoaDon && t.TongTien >= h.TongTien))
                        .SumAsync(h => (decimal?)h.TongTien) ?? 0;

                    var response = $"💰 *Doanh thu hệ thống:*\n\n" +
                                   $"📊 *Tổng doanh thu:* {totalRevenue:N0} ₫\n" +
                                   $"📅 *Tháng này ({currentMonth}/{currentYear}):* {monthlyRevenue:N0} ₫\n" +
                                   $"📅 *Tháng trước ({lastMonth}/{lastMonthYear}):* {lastMonthRevenue:N0} ₫\n\n";

                    if (lastMonthRevenue > 0)
                    {
                        var change = ((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100;
                        var trend = change >= 0 ? "📈 Tăng" : "📉 Giảm";
                        response += $"{trend} {Math.Abs(change):F1}% so với tháng trước\n\n";
                    }

                    if (unpaidBills > 0)
                    {
                        response += $"⚠️ *Chưa thu được:* {unpaidBills:N0} ₫\n";
                        response += "💡 *Gợi ý:* Nên nhắc nhở người thuê thanh toán sớm!";
                    }

                    return response;
                }

                // Intent: Hợp đồng sắp hết hạn
                if (command.Contains("hợp đồng") || command.Contains("hop dong") ||
                    command.Contains("sắp hết hạn") || command.Contains("sap het han") ||
                    command.Contains("hết hạn") || command.Contains("het han"))
                {
                    var today = DateTime.Now.Date;
                    var expiringContracts = await context.HopDong
                        .Include(h => h.Phong)
                        .Include(h => h.NguoiThue)
                        .Where(h => h.NgayKetThuc >= today && h.NgayKetThuc <= today.AddDays(30))
                        .OrderBy(h => h.NgayKetThuc)
                        .Select(h => new
                        {
                            RoomNumber = h.Phong.TenPhong,
                            TenantName = h.NguoiThue.HoTen,
                            h.NgayKetThuc
                        })
                        .ToListAsync();

                    // Tính DaysLeft sau khi lấy dữ liệu
                    var contractsWithDays = expiringContracts
                        .Where(c => c.NgayKetThuc.HasValue)
                        .Select(c => new
                        {
                            c.RoomNumber,
                            c.TenantName,
                            NgayKetThuc = c.NgayKetThuc.Value,
                            DaysLeft = (c.NgayKetThuc.Value - today).Days
                        }).ToList();

                    if (!expiringContracts.Any())
                    {
                        return "✅ *Hợp đồng:*\n\n" +
                               "Không có hợp đồng nào sắp hết hạn trong 30 ngày tới.\n\n" +
                               "💡 Mọi hợp đồng đều đang hoạt động tốt!";
                    }

                    var response = $"⚠️ *Hợp đồng sắp hết hạn:* ({contractsWithDays.Count} hợp đồng)\n\n";
                    foreach (var contract in contractsWithDays)
                    {
                        response += $"• Phòng: {contract.RoomNumber}\n";
                        response += $"  Người thuê: {contract.TenantName}\n";
                        response += $"  Hết hạn: {contract.NgayKetThuc:dd/MM/yyyy}\n";
                        response += $"  Còn lại: {contract.DaysLeft} ngày\n\n";
                    }

                    response += "💡 *Gợi ý:* Nên liên hệ với người thuê để gia hạn hợp đồng sớm!";
                    return response;
                }

                // Intent: Hóa đơn chưa thanh toán
                if (command.Contains("chưa thanh toán") || command.Contains("chua thanh toan") ||
                    command.Contains("nợ") || command.Contains("no") || command.Contains("chưa trả") ||
                    command.Contains("chua tra"))
                {
                    var unpaidBills = await context.HoaDon
                        .Include(h => h.Phong)
                        .Include(h => h.NguoiThue)
                        .Where(h => !context.ThanhToan.Any(t => t.MaHoaDon == h.MaHoaDon && t.TongTien >= h.TongTien))
                        .OrderByDescending(h => h.NgayLap)
                        .Take(10)
                        .Select(h => new
                        {
                            h.MaHoaDon,
                            h.TongTien,
                            RoomNumber = h.Phong.TenPhong,
                            TenantName = h.NguoiThue.HoTen,
                            h.KyHoaDon,
                            h.NgayLap
                        })
                        .ToListAsync();

                    if (!unpaidBills.Any())
                    {
                        return "✅ *Thanh toán:*\n\n" +
                               "Tất cả hóa đơn đã được thanh toán đầy đủ!\n\n" +
                               "💡 Hệ thống đang hoạt động tốt!";
                    }

                    var totalUnpaid = unpaidBills.Sum(b => b.TongTien);
                    var response = $"⚠️ *Hóa đơn chưa thanh toán:* ({unpaidBills.Count} hóa đơn)\n\n";
                    response += $"💰 *Tổng số tiền:* {totalUnpaid:N0} ₫\n\n";

                    foreach (var bill in unpaidBills)
                    {
                        var daysOverdue = (DateTime.Now - bill.NgayLap).Days;
                        response += $"• HĐ #{bill.MaHoaDon}\n";
                        response += $"  Phòng: {bill.RoomNumber} - {bill.TenantName}\n";
                        response += $"  Kỳ: {bill.KyHoaDon}\n";
                        response += $"  Số tiền: {bill.TongTien:N0} ₫\n";
                        if (daysOverdue > 0)
                        {
                            response += $"  ⚠️ Quá hạn: {daysOverdue} ngày\n";
                        }
                        response += "\n";
                    }

                    response += "💡 *Gợi ý:* Nên gửi thông báo nhắc nhở người thuê thanh toán!";
                    return response;
                }

                // Intent: Nhắc nợ tự động
                if (command.Contains("nhắc nợ") || command.Contains("nhac no") ||
                    command.Contains("gửi nhắc nợ") || command.Contains("gui nhac no") ||
                    command.Contains("nhắc đóng tiền") || command.Contains("nhac dong tien"))
                {
                    var unpaidBills = await context.HoaDon
                        .Include(h => h.Phong)
                        .Include(h => h.NguoiThue)
                        .ThenInclude(n => n.User)
                        .Where(h => !context.ThanhToan.Any(t => t.MaHoaDon == h.MaHoaDon && t.TongTien >= h.TongTien))
                        .Select(h => new
                        {
                            h.MaHoaDon,
                            h.TongTien,
                            RoomNumber = h.Phong.TenPhong,
                            TenantName = h.NguoiThue.HoTen,
                            h.KyHoaDon,
                            h.NgayLap,
                            TelegramChatId = h.NguoiThue.User != null ? h.NguoiThue.User.TelegramChatId : (long?)null
                        })
                        .ToListAsync();

                    if (!unpaidBills.Any())
                    {
                        return "✅ Tất cả hóa đơn đã được thanh toán đầy đủ!";
                    }

                    var sentCount = 0;
                    var response = $"🔔 *Nhắc nợ tự động:*\n\n";
                    response += $"Đã gửi thông báo nhắc nợ đến {unpaidBills.Count} phòng:\n\n";

                    foreach (var bill in unpaidBills)
                    {
                        var daysOverdue = (DateTime.Now - bill.NgayLap).Days;
                        response += $"| Phòng | Số tiền | Quá hạn |\n";
                        response += $"| :--- | :--- | :--- |\n";
                        response += $"| {bill.RoomNumber} | {bill.TongTien:N0} VNĐ | {daysOverdue} ngày |\n\n";

                        // Gửi tin nhắn qua Telegram nếu có TelegramChatId
                        if (bill.TelegramChatId.HasValue)
                        {
                            try
                            {
                                var message = $"🔔 *Nhắc nhở thanh toán:*\n\n" +
                                             $"Phòng: {bill.RoomNumber}\n" +
                                             $"Hóa đơn: #{bill.MaHoaDon} - Kỳ {bill.KyHoaDon}\n" +
                                             $"Số tiền: {bill.TongTien:N0} VNĐ\n" +
                                             $"{(daysOverdue > 0 ? $"⚠️ Quá hạn: {daysOverdue} ngày\n" : "")}\n" +
                                             $"Vui lòng thanh toán sớm. Cảm ơn!";
                                
                                await _botClient.SendMessage(
                                    chatId: bill.TelegramChatId.Value,
                                    text: message,
                                    parseMode: ParseMode.Markdown
                                );
                                sentCount++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Không thể gửi tin nhắn đến TelegramChatId: {ChatId}", bill.TelegramChatId);
                            }
                        }
                    }

                    response += $"✅ Đã gửi thành công {sentCount}/{unpaidBills.Count} tin nhắn qua Telegram.";
                    return response;
                }

                // Intent: Tra cứu thông tin khách thuê
                if (command.Contains("phòng") && (command.Contains("là ai") || command.Contains("la ai") ||
                    command.Contains("ai thuê") || command.Contains("ai thue") ||
                    command.Contains("thông tin phòng") || command.Contains("thong tin phong")))
                {
                    // Extract số phòng từ message (ví dụ: "phòng 201", "phòng 101")
                    var roomNumber = ExtractRoomNumber(originalMessage);
                    if (string.IsNullOrEmpty(roomNumber))
                    {
                        return "❌ Vui lòng chỉ rõ số phòng. Ví dụ: \"Phòng 201 là ai thuê?\"";
                    }

                    var contract = await context.HopDong
                        .Include(h => h.Phong)
                        .Include(h => h.NguoiThue)
                        .Where(h => h.Phong.TenPhong.Contains(roomNumber) && 
                                   (h.NgayKetThuc == null || h.NgayKetThuc >= DateTime.Now))
                        .OrderByDescending(h => h.NgayBatDau)
                        .FirstOrDefaultAsync();

                    if (contract == null)
                    {
                        return $"❌ Không tìm thấy thông tin người thuê phòng {roomNumber}.";
                    }

                    var response = $"🔍 *Thông tin Phòng {roomNumber}:*\n\n";
                    response += $"👤 *Người thuê:* {contract.NguoiThue.HoTen}\n";
                    response += $"📞 *SĐT:* {contract.NguoiThue.SDT ?? "Chưa cập nhật"}\n";
                    response += $"📍 *Quê quán:* {contract.NguoiThue.DiaChi ?? "Chưa cập nhật"}\n";
                    response += $"📋 *Hợp đồng:*\n";
                    response += $"  - Bắt đầu: {contract.NgayBatDau:dd/MM/yyyy}\n";
                    if (contract.NgayKetThuc.HasValue)
                    {
                        response += $"  - Kết thúc: {contract.NgayKetThuc.Value:dd/MM/yyyy}\n";
                        var daysLeft = (contract.NgayKetThuc.Value - DateTime.Now).Days;
                        response += $"  - Còn lại: {daysLeft} ngày\n";
                    }
                    response += $"  - Tiền cọc: {contract.TienCoc:N0} VNĐ\n";

                    return response;
                }

                // Intent: Gửi thông báo
                if (command.Contains("gửi thông báo") || command.Contains("gui thong bao") ||
                    command.Contains("thông báo") || command.Contains("thong bao") ||
                    command.Contains("thông báo cho") || command.Contains("thong bao cho"))
                {
                    return "🔔 *Gửi thông báo:*\n\n" +
                           "Để gửi thông báo cho người thuê:\n\n" +
                           "1️⃣ Đăng nhập vào hệ thống\n" +
                           "2️⃣ Vào mục 'Thông báo'\n" +
                           "3️⃣ Tạo thông báo mới\n" +
                           "4️⃣ Chọn đối tượng nhận (tất cả hoặc từng người)\n" +
                           "5️⃣ Nhập nội dung và gửi\n\n" +
                           "💡 *Gợi ý:* Thông báo sẽ được gửi qua hệ thống và có thể kèm theo email/SMS.";
                }

                // Intent: Chào hỏi
                if (command.Contains("xin chào") || command.Contains("chào") || command.Contains("chao") ||
                    command.Contains("hello") || command.Contains("hi") || command.Contains("hey"))
                {
                    return "👋 Xin chào Chủ trọ! Tôi là bot hỗ trợ quản lý.\n\n" +
                           "Bạn có thể hỏi tôi về:\n" +
                           "• 📊 Thống kê hệ thống\n" +
                           "• 🏠 Phòng trống\n" +
                           "• 💰 Doanh thu\n" +
                           "• 📋 Hợp đồng\n" +
                           "• 💳 Hóa đơn chưa thanh toán\n" +
                           "• 🔔 Gửi thông báo\n\n" +
                           "Hoặc sử dụng các nút trên bàn phím!";
                }

                // Intent: Cảm ơn
                if (command.Contains("cảm ơn") || command.Contains("cam on") || command.Contains("thanks") ||
                    command.Contains("thank you"))
                {
                    return "😊 Không có gì! Chúc bạn quản lý tốt! Nếu cần hỗ trợ thêm, cứ hỏi tôi nhé!";
                }

                // Default response với gợi ý
                return "🤔 Tôi chưa hiểu rõ câu hỏi của bạn. Bạn có thể:\n\n" +
                       "• 📊 Hỏi về thống kê: \"Xem thống kê\", \"Có bao nhiêu phòng?\"\n" +
                       "• 🏠 Hỏi về phòng: \"Phòng nào còn trống?\", \"Danh sách phòng\"\n" +
                       "• 💰 Hỏi về doanh thu: \"Doanh thu tháng này\", \"Tổng doanh thu\"\n" +
                       "• 📋 Hỏi về hợp đồng: \"Hợp đồng sắp hết hạn\"\n" +
                       "• 💳 Hỏi về thanh toán: \"Hóa đơn chưa thanh toán\", \"Ai còn nợ?\"\n\n" +
                       "Hoặc sử dụng các nút trên bàn phím để được hỗ trợ nhanh hơn!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý intent cho Landlord");
                return "❌ Có lỗi xảy ra. Vui lòng thử lại sau.";
            }
        }

        // =====================================
        // XỬ LÝ ẢNH (OCR BIÊN LAI CHUYỂN KHOẢN)
        // =====================================
        private async Task HandlePhotoMessageAsync(ITelegramBotClient botClient, Message message, Models.User user, bool isLandlord, ApplicationDbContext context, CancellationToken cancellationToken)
        {
            try
            {
                if (isLandlord)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "⚠️ Tính năng OCR biên lai chỉ dành cho người thuê. Chủ trọ vui lòng sử dụng hệ thống web để xử lý thanh toán.",
                        cancellationToken: cancellationToken);
                    return;
                }

                if (user.NguoiThue == null)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "❌ Không tìm thấy thông tin người thuê. Vui lòng liên hệ admin.",
                        cancellationToken: cancellationToken);
                    return;
                }

                // Lấy ảnh có độ phân giải cao nhất
                var photo = message.Photo.OrderByDescending(p => p.FileSize).FirstOrDefault();
                if (photo == null)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "❌ Không thể tải ảnh. Vui lòng thử lại.",
                        cancellationToken: cancellationToken);
                    return;
                }

                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "📸 Đã nhận ảnh biên lai. Hệ thống đang xử lý OCR để đọc thông tin...",
                    cancellationToken: cancellationToken);

                // Download ảnh từ Telegram
                var file = await botClient.GetFile(photo.FileId, cancellationToken);
                if (file == null)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "❌ Không thể tải ảnh từ Telegram. Vui lòng thử lại.",
                        cancellationToken: cancellationToken);
                    return;
                }

                using var memoryStream = new MemoryStream();
                if (string.IsNullOrWhiteSpace(file.FilePath))
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "❌ Không thể tải ảnh từ Telegram (FilePath rỗng). Vui lòng thử lại.",
                        cancellationToken: cancellationToken);
                    return;
                }

                await botClient.DownloadFile(file.FilePath, memoryStream, cancellationToken);
                memoryStream.Position = 0;

                // Gọi OCR service để đọc biên lai chuyển khoản
                using var ocrScope = _serviceScopeFactory.CreateScope();
                var ocrService = ocrScope.ServiceProvider.GetRequiredService<IOCRAPIService>();

                var receiptResult = await ocrService.RecognizeReceiptFromStreamAsync(memoryStream, "receipt.jpg");
                
                if (!receiptResult.Success)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"❌ Không thể đọc được thông tin từ ảnh: {receiptResult.Message}\n\nVui lòng đảm bảo ảnh rõ ràng và chứa thông tin chuyển khoản.",
                        cancellationToken: cancellationToken);
                    return;
                }

                // Hiển thị thông tin đã đọc được
                var receiptInfo = $"📄 *Thông tin biên lai:*\n\n";
                if (receiptResult.SoTien.HasValue)
                {
                    receiptInfo += $"💰 Số tiền: {receiptResult.SoTien.Value:N0} VNĐ\n";
                }
                if (!string.IsNullOrEmpty(receiptResult.SoTaiKhoan))
                {
                    receiptInfo += $"🏦 Số tài khoản: {receiptResult.SoTaiKhoan}\n";
                }
                if (!string.IsNullOrEmpty(receiptResult.TenNguoiNhan))
                {
                    receiptInfo += $"👤 Người nhận: {receiptResult.TenNguoiNhan}\n";
                }
                if (receiptResult.NgayGiaoDich.HasValue)
                {
                    receiptInfo += $"📅 Ngày giao dịch: {receiptResult.NgayGiaoDich.Value:dd/MM/yyyy}\n";
                }
                if (!string.IsNullOrEmpty(receiptResult.NoiDungChuyenKhoan))
                {
                    receiptInfo += $"📝 Nội dung: {receiptResult.NoiDungChuyenKhoan}\n";
                }

                // Match với hóa đơn chưa thanh toán
                if (receiptResult.SoTien.HasValue)
                {
                    var tenantId = user.NguoiThue.MaNguoiThue;
                    
                    // Tìm hóa đơn chưa thanh toán của người thuê này
                    var unpaidBills = await context.HoaDon
                        .Include(h => h.Phong)
                        .Where(h => h.MaNguoiThue == tenantId)
                        .Where(h => !context.ThanhToan.Any(t => 
                            t.MaHoaDon == h.MaHoaDon && 
                            t.TongTien >= h.TongTien))
                        .OrderByDescending(h => h.NgayLap)
                        .ToListAsync();

                    // Tìm hóa đơn có số tiền gần khớp (sai số ±5%)
                    var matchedBill = unpaidBills.FirstOrDefault(h =>
                    {
                        var difference = Math.Abs((decimal)h.TongTien - receiptResult.SoTien.Value);
                        var percentage = (difference / h.TongTien) * 100;
                        return percentage <= 5; // Cho phép sai số 5%
                    });

                    if (matchedBill != null)
                    {
                        // Tạo bản ghi thanh toán với trạng thái "Chờ duyệt" (0 = Chờ duyệt, 1 = Đã duyệt, 2 = Đã hủy)
                        var thanhToan = new Models.ThanhToan
                        {
                            MaHoaDon = matchedBill.MaHoaDon,
                            MaNguoiThue = tenantId,
                            TongTien = receiptResult.SoTien.Value,
                            NgayThanhToan = receiptResult.NgayGiaoDich ?? DateTime.Now,
                            HinhThucThanhToan = "Chuyển khoản",
                            TrangThai = 0, // 0 = Chờ duyệt, cần admin duyệt
                            GhiChu = $"OCR từ Telegram Bot. Số TK: {receiptResult.SoTaiKhoan}, Nội dung: {receiptResult.NoiDungChuyenKhoan}"
                        };

                        context.ThanhToan.Add(thanhToan);
                        await context.SaveChangesAsync();

                        receiptInfo += $"\n✅ *Đã tìm thấy hóa đơn phù hợp:*\n";
                        receiptInfo += $"📋 Hóa đơn #{matchedBill.MaHoaDon}\n";
                        receiptInfo += $"🏠 Phòng: {matchedBill.Phong?.TenPhong}\n";
                        receiptInfo += $"💰 Số tiền hóa đơn: {matchedBill.TongTien:N0} VNĐ\n";
                        receiptInfo += $"📅 Kỳ: {matchedBill.KyHoaDon}\n\n";
                        receiptInfo += $"⏳ *Trạng thái:* Đã gửi yêu cầu duyệt. Admin sẽ xem xét và xác nhận thanh toán.";
                    }
                    else
                    {
                        receiptInfo += $"\n⚠️ *Không tìm thấy hóa đơn phù hợp.*\n";
                        receiptInfo += $"Bạn có {unpaidBills.Count} hóa đơn chưa thanh toán.\n";
                        receiptInfo += $"Vui lòng kiểm tra lại số tiền hoặc liên hệ admin để được hỗ trợ.";
                    }
                }
                else
                {
                    receiptInfo += $"\n⚠️ Không đọc được số tiền từ ảnh. Vui lòng liên hệ admin để xác nhận thanh toán.";
                }

                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: receiptInfo,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý ảnh biên lai");
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Có lỗi xảy ra khi xử lý ảnh. Vui lòng thử lại sau.",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task HandleDocumentMessageAsync(ITelegramBotClient botClient, Message message, Models.User user, bool isLandlord, ApplicationDbContext context, CancellationToken cancellationToken)
        {
            // Xử lý tương tự như ảnh
            await HandlePhotoMessageAsync(botClient, message, user, isLandlord, context, cancellationToken);
        }

        // =====================================
        // LƯU LỊCH SỬ CHAT
        // =====================================
        private async Task SaveChatHistoryAsync(
            long telegramChatId, 
            int maNguoiDung, 
            string userMessage, 
            string? botResponse, 
            string? intent, 
            string? vaiTro,
            string messageType,
            ApplicationDbContext context)
        {
            try
            {
                var chatHistory = new ChatHistory
                {
                    TelegramChatId = telegramChatId,
                    MaNguoiDung = maNguoiDung,
                    UserMessage = userMessage,
                    BotResponse = botResponse,
                    Intent = intent,
                    VaiTro = vaiTro,
                    MessageType = messageType,
                    ThoiGian = DateTime.Now
                };

                context.ChatHistory.Add(chatHistory);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi lưu lịch sử chat (không ảnh hưởng đến chức năng chính)");
                // Không throw exception để không ảnh hưởng đến flow chính
            }
        }

        // =====================================
        // LẤY LỊCH SỬ CHAT GẦN ĐÂY (cho AI context)
        // =====================================
        private async Task<string> GetRecentChatHistoryAsync(long telegramChatId, ApplicationDbContext context, int limit = 5)
        {
            try
            {
                var recentChats = await context.ChatHistory
                    .Where(c => c.TelegramChatId == telegramChatId)
                    .OrderByDescending(c => c.ThoiGian)
                    .Take(limit)
                    .Select(c => new
                    {
                        c.UserMessage,
                        c.BotResponse,
                        c.ThoiGian
                    })
                    .ToListAsync();

                if (!recentChats.Any())
                {
                    return "";
                }

                var historyText = "Lịch sử hội thoại gần đây:\n";
                foreach (var chat in recentChats.OrderBy(c => c.ThoiGian))
                {
                    historyText += $"User: {chat.UserMessage}\n";
                    if (!string.IsNullOrEmpty(chat.BotResponse))
                    {
                        historyText += $"Bot: {chat.BotResponse.Substring(0, Math.Min(100, chat.BotResponse.Length))}...\n";
                    }
                    historyText += "\n";
                }

                return historyText;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi lấy lịch sử chat");
                return "";
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Lỗi API Telegram:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(errorMessage);
            return Task.CompletedTask;
        }
    }
}

