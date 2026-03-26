using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Data;
using DoAnCoSo.Models;

namespace DoAnCoSo.Services;

public class MomoPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly IZaloService _zaloService;
    private readonly IESmsService _smsService;
    private readonly IServiceProvider _serviceProvider;

    public MomoPaymentService(
        IConfiguration configuration,
        ApplicationDbContext context,
        IZaloService zaloService,
        IESmsService smsService,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _context = context;
        _zaloService = zaloService;
        _smsService = smsService;
        _serviceProvider = serviceProvider;
        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(_configuration["Momo:PartnerCode"])) throw new ArgumentException("Momo:PartnerCode not configured.");
        if (string.IsNullOrEmpty(_configuration["Momo:AccessKey"])) throw new ArgumentException("Momo:AccessKey not configured.");
        if (string.IsNullOrEmpty(_configuration["Momo:SecretKey"])) throw new ArgumentException("Momo:SecretKey not configured.");
        if (string.IsNullOrEmpty(_configuration["Momo:Endpoint"])) throw new ArgumentException("Momo:Endpoint not configured.");
        if (string.IsNullOrEmpty(_configuration["Momo:ReturnUrl"])) throw new ArgumentException("Momo:ReturnUrl not configured.");
        if (string.IsNullOrEmpty(_configuration["Momo:NotifyUrl"])) throw new ArgumentException("Momo:NotifyUrl not configured.");
        if (string.IsNullOrEmpty(_configuration["Frontend:BaseUrl"])) throw new ArgumentException("Frontend:BaseUrl not configured.");
    }

    public async Task<string> CreatePaymentUrlAsync(ThanhToanCreateDto thanhToan)
    {
        var endpoint = _configuration["Momo:Endpoint"];
        var partnerCode = _configuration["Momo:PartnerCode"];
        var accessKey = _configuration["Momo:AccessKey"];
        var secretKey = _configuration["Momo:SecretKey"];
        var returnUrl = _configuration["Momo:ReturnUrl"];
        var notifyUrl = _configuration["Momo:NotifyUrl"];
        var orderInfo = $"Thanh toán hóa đơn #{thanhToan.MaHoaDon}";

        string requestId = Guid.NewGuid().ToString();
        string orderId = $"HD{thanhToan.MaHoaDon}_{DateTime.Now:yyyyMMddHHmmss}";
        string amount = ((int)thanhToan.TongTien).ToString();

        string rawHash = $"accessKey={accessKey}&amount={amount}&extraData=&ipnUrl={notifyUrl}&orderId={orderId}" +
                         $"&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}" +
                         $"&requestId={requestId}&requestType=captureWallet";

        string signature = HmacSha256(secretKey, rawHash);

        var requestBody = new
        {
            partnerCode,
            accessKey,
            requestId,
            amount,
            orderId,
            orderInfo,
            redirectUrl = returnUrl,
            ipnUrl = notifyUrl,
            requestType = "captureWallet",
            extraData = "",
            signature
        };

        using var http = new HttpClient();
        var response = await http.PostAsync(endpoint, new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
        var responseContent = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(responseContent);

        var payUrl = json["payUrl"]?.ToString();
        if (string.IsNullOrEmpty(payUrl))
        {
            Console.WriteLine($"Momo payment failed: {responseContent}");
            throw new Exception("Không thể tạo URL thanh toán Momo.");
        }

        return payUrl;
    }

    public async Task<IActionResult> PaymentCallbackAsync(IQueryCollection query)
    {
        try
        {
            var orderId = query["orderId"];
            var resultCode = query["resultCode"];
            var signature = query["signature"];
            var message = query["message"];
            var responseTime = query["responseTime"];

            Console.WriteLine($"Received MoMo callback - OrderId: {orderId}, ResultCode: {resultCode}, Message: {message}");

            var rawData = $"accessKey={_configuration["Momo:AccessKey"]}" +
                          $"&amount={query["amount"]}" +
                          $"&extraData={query["extraData"]}" +
                          $"&message={message}" +
                          $"&orderId={orderId}" +
                          $"&orderInfo={query["orderInfo"]}" +
                          $"&orderType={query["orderType"]}" +
                          $"&partnerCode={query["partnerCode"]}" +
                          $"&payType={query["payType"]}" +
                          $"&requestId={query["requestId"]}" +
                          $"&responseTime={responseTime}" +
                          $"&resultCode={resultCode}" +
                          $"&transId={query["transId"]}";

            var generatedSignature = HmacSha256(_configuration["Momo:SecretKey"], rawData);

            if (!string.Equals(signature, generatedSignature, StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("Momo signature mismatch");
                return new RedirectResult($"{_configuration["Frontend:BaseUrl"]}/payment-failure.html?error=InvalidSignature&orderId={orderId}");
            }

            // Tìm thanh toán theo mã hóa đơn trong orderId (format: HD{MaHoaDon}_{yyyyMMddHHmmss})
            var maHoaDon = int.Parse(orderId.ToString().Split('_')[0].Substring(2));
            var thanhToan = await _context.ThanhToan
                .FirstOrDefaultAsync(t => t.MaHoaDon == maHoaDon);

            if (thanhToan == null)
            {
                Console.WriteLine($"Thanh toán không tồn tại: {orderId}");
                return new RedirectResult($"{_configuration["Frontend:BaseUrl"]}/payment-failure.html?error=ThanhToanNotFound&orderId={orderId}");
            }

            if (resultCode == "0")
            {
                thanhToan.TrangThai = 1; // Đã thanh toán thành công (TrangThai = 1 để gửi SMS)
                thanhToan.NgayThanhToan = DateTime.Now;
                _context.ThanhToan.Update(thanhToan);
                await _context.SaveChangesAsync();

                // Gửi thông báo Zalo và SMS (nếu có thông tin liên hệ)
                await NotifyZaloPaymentSuccess(thanhToan.MaHoaDon);
                await NotifySmsPaymentSuccess(thanhToan.MaHoaDon);

                // Chuyển hướng đến trang thành công với đầy đủ thông tin
                return new RedirectResult($"{_configuration["Frontend:BaseUrl"]}/payment-success.html?{query}");
            }

            thanhToan.TrangThai = 1; // Thanh toán thất bại
            _context.ThanhToan.Update(thanhToan);
            await _context.SaveChangesAsync();

            // Chuyển hướng đến trang thất bại với đầy đủ thông tin
            return new RedirectResult($"{_configuration["Frontend:BaseUrl"]}/payment-failure.html?{query}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Momo PaymentCallback: {ex.Message}");
            return new RedirectResult($"{_configuration["Frontend:BaseUrl"]}/payment-failure.html?error=ServerError");
        }
    }

    private string HmacSha256(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash).ToLower();
    }

    private async Task NotifyZaloPaymentSuccess(int maHoaDon)
    {
        try
        {
            var hd = await _context.HoaDon
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (hd?.NguoiThue == null) return;
            var phone = hd.NguoiThue.SDT;
            if (string.IsNullOrWhiteSpace(phone)) return;

            var message = $"Thanh toán thành công hóa đơn {hd.KyHoaDon} - Phòng {hd.Phong?.TenPhong ?? ""}. Số tiền: {hd.TongTien:N0}đ. Cảm ơn bạn!";
            await _zaloService.SendPaymentSuccessAsync(phone, message);
        }
        catch
        {
            // Không chặn luồng thanh toán nếu gửi Zalo lỗi
        }
    }

    // Gửi SMS xác nhận thanh toán thành công
    private async Task NotifySmsPaymentSuccess(int maHoaDon)
    {
        try
        {
            var hd = await _context.HoaDon
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (hd?.NguoiThue == null) return;
            var phone = hd.NguoiThue.SDT;
            if (string.IsNullOrWhiteSpace(phone)) return;

            // Nội dung SMS cố định
            const string smsContent = "Cam on quy khach da su dung dich vu cua chung toi. Chuc quy khach mot ngay tot lanh!";
            const string brandname = "Baotrixemay";
            
            Console.WriteLine($"[MomoPaymentService] 📱 Đang gửi SMS đến {phone}...");
            var (success, errorMessage, codeResult) = await _smsService.SendSmsAsync(phone, smsContent, brandname);
            if (success)
            {
                Console.WriteLine($"[MomoPaymentService] ✅ Đã gửi SMS thành công đến {phone}. CodeResult: {codeResult}");
            }
            else
            {
                Console.WriteLine($"[MomoPaymentService] ⚠️ Gửi SMS thất bại đến {phone}. Lỗi: {errorMessage} (CodeResult: {codeResult})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MomoPaymentService] ❌ Lỗi gửi SMS: {ex.Message}");
            // Không chặn luồng thanh toán nếu gửi SMS lỗi
        }
    }
}
