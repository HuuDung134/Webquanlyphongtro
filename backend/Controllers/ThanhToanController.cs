using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using DoAnCoSo.Services;
using System;
using System.Threading.Tasks;

namespace DoAnCoSo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThanhToanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly MomoPaymentService _momoPaymentService;
        private readonly EmailService _emailService;
        private readonly IESmsService _smsService;

        public ThanhToanController(ApplicationDbContext context, MomoPaymentService momoPaymentService, EmailService emailService, IESmsService smsService)
        {
            _context = context;
            _momoPaymentService = momoPaymentService;
            _emailService = emailService;
            _smsService = smsService;
        }

        // GET: api/ThanhToan
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ThanhToan>>> GetThanhToan()
        {
            return await _context.ThanhToan
                .Include(t => t.HoaDon)
                    .ThenInclude(h => h.Phong)
                .Include(t => t.NguoiThue)
                .OrderByDescending(t => t.NgayThanhToan)
                .ToListAsync();
        }

        // GET: api/ThanhToan/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ThanhToan>> GetThanhToan(int id)
        {
            var thanhToan = await _context.ThanhToan
                .Include(t => t.HoaDon)
                    .ThenInclude(h => h.Phong)
                .Include(t => t.NguoiThue)
                .FirstOrDefaultAsync(t => t.MaThanhToan == id);

            if (thanhToan == null)
            {
                return NotFound();
            }

            return thanhToan;
        }

        // GET: api/ThanhToan/HoaDon/5
        [HttpGet("HoaDon/{hoaDonId}")]
        public async Task<ActionResult<IEnumerable<ThanhToan>>> GetThanhToanByHoaDon(int hoaDonId)
        {
            return await _context.ThanhToan
                .Include(t => t.HoaDon)
                    .ThenInclude(h => h.Phong)
                .Include(t => t.NguoiThue)
                .Where(t => t.MaHoaDon == hoaDonId)
                .OrderByDescending(t => t.NgayThanhToan)
                .ToListAsync();
        }

        // POST: api/ThanhToan
        [HttpPost]
        public async Task<ActionResult<ThanhToan>> PostThanhToan(ThanhToan thanhToan)
        {
            try
            {
                // Kiểm tra ModelState
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Kiểm tra MaHoaDon
                if (thanhToan.MaHoaDon <= 0)
                {
                    return BadRequest("Mã hóa đơn không hợp lệ");
                }

                // Kiểm tra hóa đơn tồn tại
                var hoaDon = await _context.HoaDon
                    .Include(h => h.Phong)
                    .FirstOrDefaultAsync(h => h.MaHoaDon == thanhToan.MaHoaDon);

                if (hoaDon == null)
                {
                    return BadRequest($"Không tìm thấy hóa đơn với mã {thanhToan.MaHoaDon}");
                }

                // Kiểm tra MaNguoiThue
                if (thanhToan.MaNguoiThue <= 0)
                {
                    // Tự động lấy từ hóa đơn nếu không có
                    thanhToan.MaNguoiThue = hoaDon.MaNguoiThue;
                }

                // Kiểm tra người thuê
                var nguoiThue = await _context.NguoiThue.FindAsync(thanhToan.MaNguoiThue);
                if (nguoiThue == null)
                {
                    return BadRequest($"Không tìm thấy người thuê với mã {thanhToan.MaNguoiThue}");
                }

                // Kiểm tra số tiền thanh toán
                if (thanhToan.TongTien <= 0)
                {
                    return BadRequest("Số tiền thanh toán phải lớn hơn 0");
                }

                // Tính tổng đã thanh toán (loại trừ thanh toán hiện tại nếu đang update)
                var tongDaThanhToan = await _context.ThanhToan
                    .Where(t => t.MaHoaDon == thanhToan.MaHoaDon)
                    .SumAsync(t => (decimal?)t.TongTien) ?? 0;

                if (tongDaThanhToan + thanhToan.TongTien > hoaDon.TongTien)
                {
                    return BadRequest($"Số tiền thanh toán vượt quá số tiền còn lại của hóa đơn. Tổng tiền hóa đơn: {hoaDon.TongTien:N0} VNĐ, đã thanh toán: {tongDaThanhToan:N0} VNĐ, số tiền còn lại: {hoaDon.TongTien - tongDaThanhToan:N0} VNĐ");
                }

                // Kiểm tra hình thức thanh toán
                if (string.IsNullOrWhiteSpace(thanhToan.HinhThucThanhToan))
                {
                    return BadRequest("Vui lòng chọn hình thức thanh toán");
                }

                // Cập nhật thông tin thanh toán
                thanhToan.NgayThanhToan = DateTime.Now;
                // Mặc định TrangThai = 2 (Đang chờ) cho thanh toán mới, chỉ gửi email/SMS khi được xác nhận thành công
                if (thanhToan.TrangThai == 0) thanhToan.TrangThai = 2; // Mặc định là đang chờ xác nhận
                _context.ThanhToan.Add(thanhToan);

                await _context.SaveChangesAsync();
                
                // CHỈ gửi email/SMS khi TrangThai = 1 (Đã thanh toán thành công)
                // Không gửi khi tạo mới, chỉ gửi khi được xác nhận
                if (thanhToan.TrangThai == 1)
                {
                    await SendPaymentConfirmationAsync(thanhToan, hoaDon, nguoiThue);
                }

                return CreatedAtAction(nameof(GetThanhToan), new { id = thanhToan.MaThanhToan }, thanhToan);
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Lỗi database khi tạo thanh toán: {ex.Message}");
                return BadRequest($"Lỗi khi lưu thanh toán: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi không xác định khi tạo thanh toán: {ex.Message}");
                return BadRequest($"Lỗi khi tạo thanh toán: {ex.Message}");
            }
        }

        // PUT: api/ThanhToan/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutThanhToan(int id, ThanhToan thanhToan)
        {
            try
            {
                if (id != thanhToan.MaThanhToan)
                {
                    return BadRequest("Mã thanh toán không khớp");
                }

                // Kiểm tra ModelState
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Kiểm tra thanh toán tồn tại
                var thanhToanHienTai = await _context.ThanhToan.FindAsync(id);
                if (thanhToanHienTai == null)
                {
                    return NotFound($"Không tìm thấy thanh toán với mã {id}");
                }

                // Kiểm tra số tiền thanh toán
                if (thanhToan.TongTien <= 0)
                {
                    return BadRequest("Số tiền thanh toán phải lớn hơn 0");
                }

                // Kiểm tra không cho phép thay đổi hóa đơn
                if (thanhToanHienTai.MaHoaDon != thanhToan.MaHoaDon)
                {
                    return BadRequest("Không thể thay đổi hóa đơn của thanh toán");
                }

                // Kiểm tra hình thức thanh toán
                if (string.IsNullOrWhiteSpace(thanhToan.HinhThucThanhToan))
                {
                    return BadRequest("Vui lòng chọn hình thức thanh toán");
                }

                // Kiểm tra tổng tiền không vượt quá hóa đơn
                var hoaDon = await _context.HoaDon.FindAsync(thanhToan.MaHoaDon);
                if (hoaDon != null)
                {
                    var tongDaThanhToan = await _context.ThanhToan
                        .Where(t => t.MaHoaDon == thanhToan.MaHoaDon && t.MaThanhToan != id)
                        .SumAsync(t => (decimal?)t.TongTien) ?? 0;

                    if (tongDaThanhToan + thanhToan.TongTien > hoaDon.TongTien)
                    {
                        return BadRequest($"Số tiền thanh toán vượt quá số tiền còn lại của hóa đơn. Tổng tiền hóa đơn: {hoaDon.TongTien:N0} VNĐ, đã thanh toán (khác thanh toán này): {tongDaThanhToan:N0} VNĐ, số tiền còn lại: {hoaDon.TongTien - tongDaThanhToan:N0} VNĐ");
                    }
                }

                // Lưu trạng thái cũ để kiểm tra xem có thay đổi sang "Đã thanh toán" không
                var trangThaiCu = thanhToanHienTai.TrangThai;
                
                // Cập nhật thông tin thanh toán
                thanhToanHienTai.TongTien = thanhToan.TongTien;
                thanhToanHienTai.HinhThucThanhToan = thanhToan.HinhThucThanhToan;
                thanhToanHienTai.GhiChu = thanhToan.GhiChu;
                thanhToanHienTai.TrangThai = thanhToan.TrangThai;

                _context.Entry(thanhToanHienTai).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                
                // Nếu TrangThai thay đổi từ khác sang 1 (Đã thanh toán thành công), gửi email/SMS
                if (trangThaiCu != 1 && thanhToan.TrangThai == 1)
                {
                    var hoaDonFull = await _context.HoaDon
                        .Include(h => h.Phong)
                        .FirstOrDefaultAsync(h => h.MaHoaDon == thanhToanHienTai.MaHoaDon);
                    var nguoiThue = await _context.NguoiThue.FindAsync(thanhToanHienTai.MaNguoiThue);
                    
                    if (hoaDonFull != null && nguoiThue != null)
                    {
                        await SendPaymentConfirmationAsync(thanhToanHienTai, hoaDonFull, nguoiThue);
                    }
                }

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ThanhToanExists(id))
                {
                    return NotFound($"Không tìm thấy thanh toán với mã {id}");
                }
                else
                {
                    return BadRequest("Có lỗi xảy ra khi cập nhật dữ liệu. Vui lòng thử lại.");
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Lỗi database khi cập nhật thanh toán: {ex.Message}");
                return BadRequest($"Lỗi khi lưu thanh toán: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi không xác định khi cập nhật thanh toán: {ex.Message}");
                return BadRequest($"Lỗi khi cập nhật thanh toán: {ex.Message}");
            }
        }

        // DELETE: api/ThanhToan/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteThanhToan(int id)
        {
            var thanhToan = await _context.ThanhToan.FindAsync(id);
            if (thanhToan == null)
            {
                return NotFound();
            }

            _context.ThanhToan.Remove(thanhToan);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ThanhToanExists(int id)
        {
            return _context.ThanhToan.Any(e => e.MaThanhToan == id);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateMomoPayment([FromBody] ThanhToanCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Kiểm tra hóa đơn tồn tại
                var hoaDon = await _context.HoaDon
                    .Include(h => h.Phong)
                    .FirstOrDefaultAsync(h => h.MaHoaDon == dto.MaHoaDon);

                if (hoaDon == null)
                {
                    return BadRequest("Không tìm thấy hóa đơn");
                }

                // Tự động lấy MaNguoiThue từ HoaDon nếu không được cung cấp
                if (dto.MaNguoiThue == 0)
                {
                    dto.MaNguoiThue = hoaDon.MaNguoiThue;
                }

                // Kiểm tra người thuê tồn tại
                var nguoiThue = await _context.NguoiThue.FindAsync(dto.MaNguoiThue);
                if (nguoiThue == null)
                {
                    return BadRequest("Không tìm thấy người thuê");
                }

                // Kiểm tra tổng số tiền thanh toán
                if (dto.TongTien <= 0)
                {
                    return BadRequest("Số tiền thanh toán phải lớn hơn 0");
                }

                var tongDaThanhToan = await _context.ThanhToan
                    .Where(t => t.MaHoaDon == dto.MaHoaDon)
                    .SumAsync(t => t.TongTien);

                if (tongDaThanhToan + dto.TongTien > hoaDon.TongTien)
                {
                    return BadRequest($"Số tiền thanh toán vượt quá số tiền còn lại của hóa đơn. Số tiền còn lại: {hoaDon.TongTien - tongDaThanhToan}");
                }

                var orderId = $"HD{hoaDon.MaHoaDon}_{DateTime.Now:yyyyMMddHHmmss}";
                var orderInfo = $"Thanh toan hoa don {hoaDon.MaHoaDon} - {hoaDon.Phong.TenPhong}";
                var returnUrl = $"{Request.Scheme}://{Request.Host}/payment/success";
                var ipnUrl = $"{Request.Scheme}://{Request.Host}/api/ThanhToan/Momo/IPN";

                Console.WriteLine($"Creating MoMo payment with orderId: {orderId}");

                var paymentUrl = await _momoPaymentService.CreatePaymentUrlAsync(
                    dto
                );

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    return BadRequest("Không thể tạo URL thanh toán MoMo");
                }

                // Tạo entity ThanhToan mới từ DTO
                var thanhToan = new ThanhToan
                {
                    MaHoaDon = dto.MaHoaDon,
                    MaNguoiThue = dto.MaNguoiThue,
                    TongTien = dto.TongTien,
                    NgayThanhToan = DateTime.Now,
                    HinhThucThanhToan = "MoMo",
                    GhiChu = $"Thanh toán qua MoMo - OrderId: {orderId}",
                    TrangThai = 2 // Đã thanh toán qua MoMo
                };

                _context.ThanhToan.Add(thanhToan);
                await _context.SaveChangesAsync();
                // Gửi email xác nhận thanh toán
                if (nguoiThue != null && !string.IsNullOrEmpty(nguoiThue.Email))
                {
                    var subject = $"[Xác nhận] Đã thanh toán hóa đơn phòng {hoaDon.Phong.TenPhong} kỳ {hoaDon.KyHoaDon}";
                    var body = $"Xin chào {nguoiThue.HoTen},<br>Bạn đã thanh toán thành công hóa đơn phòng <b>{hoaDon.Phong.TenPhong}</b> kỳ <b>{hoaDon.KyHoaDon}</b>.<br>Số tiền: <b>{thanhToan.TongTien:N0} VNĐ</b>.<br>Ngày thanh toán: {thanhToan.NgayThanhToan:dd/MM/yyyy HH:mm}.<br>Trân trọng!";
                    try { await _emailService.SendEmailAsync(nguoiThue.Email, subject, body); } catch { /* ignore lỗi gửi mail */ }
                }
                return Ok(new { paymentUrl, orderId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tạo thanh toán MoMo: {ex.Message}");
                return BadRequest($"Lỗi khi tạo yêu cầu thanh toán MoMo: {ex.Message}");
            }
        }


        [HttpGet("callback")]
        public async Task<IActionResult> PaymentCallback()
        {
            return await _momoPaymentService.PaymentCallbackAsync(Request.Query);
        }

        // POST: api/ThanhToan/{id}/xac-nhan
        // Endpoint để xác nhận thanh toán và gửi email/SMS
        [HttpPost("{id}/xac-nhan")]
        public async Task<IActionResult> XacNhanThanhToan(int id)
        {
            try
            {
                var thanhToan = await _context.ThanhToan
                    .Include(t => t.HoaDon)
                        .ThenInclude(h => h.Phong)
                    .Include(t => t.NguoiThue)
                    .FirstOrDefaultAsync(t => t.MaThanhToan == id);

                if (thanhToan == null)
                {
                    return NotFound("Không tìm thấy thanh toán");
                }

                // Kiểm tra xem đã được xác nhận chưa
                if (thanhToan.TrangThai == 1)
                {
                    return BadRequest("Thanh toán đã được xác nhận trước đó");
                }

                // Cập nhật trạng thái thành "Đã thanh toán"
                thanhToan.TrangThai = 1;
                thanhToan.NgayThanhToan = DateTime.Now;
                
                _context.Entry(thanhToan).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Gửi email và SMS xác nhận (không chặn response)
                if (thanhToan.HoaDon != null && thanhToan.NguoiThue != null)
                {
                    // Gửi email/SMS bất đồng bộ, không chặn response
                    // Sử dụng Task.Run để không block response nhưng vẫn đảm bảo được thực thi
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await SendPaymentConfirmationAsync(thanhToan, thanhToan.HoaDon, thanhToan.NguoiThue);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ThanhToanController] ❌ Lỗi khi gửi email/SMS: {ex.Message}");
                            Console.WriteLine($"[ThanhToanController] StackTrace: {ex.StackTrace}");
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"[ThanhToanController] ⚠️ Không gửi email/SMS: HoaDon hoặc NguoiThue null");
                    if (thanhToan.HoaDon == null) Console.WriteLine($"[ThanhToanController] HoaDon is null");
                    if (thanhToan.NguoiThue == null) Console.WriteLine($"[ThanhToanController] NguoiThue is null");
                }

                return Ok(new { message = "Đã xác nhận thanh toán thành công", thanhToan });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xác nhận thanh toán: {ex.Message}");
                return BadRequest($"Lỗi khi xác nhận thanh toán: {ex.Message}");
            }
        }

        // Helper method để gửi email và SMS xác nhận thanh toán
        private async Task SendPaymentConfirmationAsync(ThanhToan thanhToan, HoaDon hoaDon, NguoiThue nguoiThue)
        {
            // Gửi email xác nhận thanh toán
            if (nguoiThue != null && !string.IsNullOrEmpty(nguoiThue.Email))
            {
                var subject = $"[Xác nhận] Đã thanh toán hóa đơn phòng {hoaDon.Phong.TenPhong} kỳ {hoaDon.KyHoaDon}";
                var body = $"Xin chào {nguoiThue.HoTen},<br>Bạn đã thanh toán thành công hóa đơn phòng <b>{hoaDon.Phong.TenPhong}</b> kỳ <b>{hoaDon.KyHoaDon}</b>.<br>Số tiền: <b>{thanhToan.TongTien:N0} VNĐ</b>.<br>Ngày thanh toán: {thanhToan.NgayThanhToan:dd/MM/yyyy HH:mm}.<br>Trân trọng!";
                try 
                { 
                    await _emailService.SendEmailAsync(nguoiThue.Email, subject, body);
                    Console.WriteLine($"[ThanhToanController] ✅ Đã gửi email xác nhận thanh toán đến {nguoiThue.Email}");
                } 
                catch (Exception ex) 
                { 
                    Console.WriteLine($"[ThanhToanController] ❌ Lỗi gửi email: {ex.Message}");
                    Console.WriteLine($"[ThanhToanController] StackTrace: {ex.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine($"[ThanhToanController] ⚠️ Không gửi email: Email người thuê rỗng hoặc null");
            }

            // Gửi SMS xác nhận thanh toán thành công - Tự động lấy số điện thoại từ người thuê
            if (nguoiThue != null && !string.IsNullOrWhiteSpace(nguoiThue.SDT))
            {
                // Nội dung và brandname cố định
                const string smsContent = "Cam on quy khach da su dung dich vu cua chung toi. Chuc quy khach mot ngay tot lanh!";
                const string brandname = "Baotrixemay";
                
                try 
                { 
                    Console.WriteLine($"[ThanhToanController] 📱 Đang gửi SMS đến {nguoiThue.SDT}...");
                    var (success, errorMessage, codeResult) = await _smsService.SendSmsAsync(nguoiThue.SDT, smsContent, brandname);
                    if (success)
                    {
                        Console.WriteLine($"[ThanhToanController] ✅ Đã gửi SMS thành công đến {nguoiThue.SDT}. CodeResult: {codeResult}");
                    }
                    else
                    {
                        Console.WriteLine($"[ThanhToanController] ⚠️ Gửi SMS thất bại đến {nguoiThue.SDT}. Lỗi: {errorMessage} (CodeResult: {codeResult})");
                    }
                } 
                catch (Exception ex) 
                { 
                    Console.WriteLine($"[ThanhToanController] ❌ Lỗi gửi SMS: {ex.Message}");
                    Console.WriteLine($"[ThanhToanController] StackTrace: {ex.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine($"[ThanhToanController] ⚠️ Không gửi SMS: Số điện thoại người thuê rỗng hoặc null");
            }
        }
    }
}

