using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DoAnCoSo.Services;

namespace DoAnCoSo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ESmsTestController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ESmsTestController> _logger;
        private readonly IESmsService _smsService;

        public ESmsTestController(IConfiguration configuration, ILogger<ESmsTestController> logger, IESmsService smsService)
        {
            _configuration = configuration;
            _logger = logger;
            _smsService = smsService;
        }

        /// <summary>
        /// Test gửi SMS qua eSMS API - Chỉ cần số điện thoại, nội dung và brandname tự động
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> TestSendSms([FromBody] ESmsTestRequest? request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        thongBao = "Request body không được để trống",
                        loiChiTiet = "Vui lòng gửi JSON với format: {\"phone\": \"0912345678\"}",
                        viDu = new { phone = "0912345678" }
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Phone))
                {
                    return BadRequest(new
                    {
                        thongBao = "Số điện thoại không được để trống",
                        loiChiTiet = "Trường 'phone' là bắt buộc"
                    });
                }

                // Nội dung và brandname cố định
                const string fixedContent = "Cam on quy khach da su dung dich vu cua chung toi. Chuc quy khach mot ngay tot lanh!";
                const string fixedBrandname = "Baotrixemay";

                _logger.LogInformation("[ESmsTestController] Đang test gửi SMS đến {Phone}", request.Phone);

                // Sử dụng ESmsService để gửi SMS với nội dung và brandname cố định
                var (success, errorMessage, codeResult) = await _smsService.SendSmsAsync(request.Phone, fixedContent, fixedBrandname);

                if (success)
                {
                    return Ok(new
                    {
                        thanhCong = true,
                        thongBao = "Gửi SMS thành công",
                        soDienThoai = request.Phone,
                        noiDung = fixedContent,
                        brandname = fixedBrandname,
                        codeResult = codeResult,
                        thoiGian = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        thanhCong = false,
                        thongBao = "Gửi SMS thất bại",
                        soDienThoai = request.Phone,
                        loiChiTiet = errorMessage,
                        codeResult = codeResult
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ESmsTestController] Lỗi khi test gửi SMS");
                return StatusCode(500, new
                {
                    thongBao = $"Lỗi: {ex.Message}",
                    loiChiTiet = ex.ToString()
                });
            }
        }

        /// <summary>
        /// Kiểm tra cấu hình eSMS
        /// </summary>
        [HttpGet("check-config")]
        public IActionResult CheckConfig()
        {
            var apiKey = _configuration["ESms:ApiKey"] ?? Environment.GetEnvironmentVariable("ESMS_API_KEY") ?? "";
            var secretKey = _configuration["ESms:SecretKey"] ?? Environment.GetEnvironmentVariable("ESMS_SECRET_KEY") ?? "";
            var brandName = _configuration["ESms:BrandName"] ?? Environment.GetEnvironmentVariable("ESMS_BRAND_NAME") ?? "Baotrixemay";
            var apiUrl = _configuration["ESms:ApiUrl"] ?? Environment.GetEnvironmentVariable("ESMS_API_URL") ?? "https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/";

            var isConfigured = !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(secretKey);

            return Ok(new
            {
                daCauHinh = isConfigured,
                thongTin = new
                {
                    apiKey = !string.IsNullOrEmpty(apiKey) ? apiKey.Substring(0, Math.Min(10, apiKey.Length)) + "..." : "Chưa cấu hình",
                    secretKey = !string.IsNullOrEmpty(secretKey) ? "Đã cấu hình" : "Chưa cấu hình",
                    brandName = brandName,
                    apiUrl = apiUrl
                },
                huongDan = isConfigured
                    ? "Cấu hình đã sẵn sàng. Bạn có thể test gửi SMS bằng endpoint POST /api/ESmsTest/test"
                    : "Vui lòng cấu hình eSMS trong appsettings.json hoặc .env file với các key: ESMS_API_KEY, ESMS_SECRET_KEY, ESMS_BRAND_NAME (tùy chọn), ESMS_API_URL (tùy chọn)"
            });
        }

    }

    /// <summary>
    /// Request model cho test eSMS - Chỉ cần số điện thoại
    /// </summary>
    public class ESmsTestRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        public string Phone { get; set; } = string.Empty;
    }
}

