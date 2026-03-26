using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DoAnCoSo.DTOs;
using Microsoft.AspNetCore.Cors.Infrastructure;
using DoAnCoSo.Data;
using DoAnCoSo.Services;
using System.Security.Claims;

namespace DoAnCoSo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChiSoDienController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOCRAPIService _ocr;

        // Cloudinary config
        private readonly Cloudinary _cloudinary;

        private async Task<int?> ResolvePhongFromCurrentUserAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr)) return null;
            if (!int.TryParse(userIdStr, out var userId)) return null;

            var nguoiThue = await _context.NguoiThue.FirstOrDefaultAsync(x => x.MaNguoiDung == userId);
            if (nguoiThue == null) return null;

            var today = DateTime.Now.Date;
            var hopDong = await _context.HopDong
                .Where(h => h.MaNguoiThue == nguoiThue.MaNguoiThue && (h.NgayKetThuc == null || h.NgayKetThuc > today))
                .OrderByDescending(h => h.NgayBatDau)
                .FirstOrDefaultAsync();

            return hopDong?.MaPhong;
        }

        public ChiSoDienController(
            ApplicationDbContext context,
            IOCRAPIService ocrService,
            Cloudinary cloudinary)
        {
            _context = context;
            _ocr = ocrService;
            _cloudinary = cloudinary;
        }

        // ==========================================================
        // =============== 1. LẤY TẤT CẢ CHỈ SỐ ĐIỆN =================
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.ChiSoDien
                .Include(x => x.GiaDien)
                .Include(x => x.Phong)
                .OrderByDescending(x => x.NgayThangDien)
                .Select(x => new ChiSoDienDto
                {
                    MaDien = x.MaDien,
                    MaPhong = x.MaPhong,
                    MaGiaDien = x.MaGiaDien,
                    SoDienCu = x.SoDienCu,
                    SoDienMoi = x.SoDienMoi,
                    SoDienTieuThu = x.SoDienMoi - x.SoDienCu,
                    TienDien = x.TienDien,
                    AnhChiSoDien = x.HinhAnhDien,
                    NgayThangDien = x.NgayThangDien,
                    TenPhong = x.Phong != null ? x.Phong.TenPhong : null
                })
                .ToListAsync();

            return Ok(data);
        }

        // ==========================================================
        // =============== 1b. LẤY CHI TIẾT =========================
        // ==========================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.ChiSoDien
                .Include(x => x.Phong)
                .FirstOrDefaultAsync(x => x.MaDien == id);

            if (item == null) return NotFound("Không tìm thấy chỉ số điện.");

            var dto = new ChiSoDienDto
            {
                MaDien = item.MaDien,
                MaPhong = item.MaPhong,
                MaGiaDien = item.MaGiaDien,
                SoDienCu = item.SoDienCu,
                SoDienMoi = item.SoDienMoi,
                SoDienTieuThu = item.SoDienMoi - item.SoDienCu,
                TienDien = item.TienDien,
                AnhChiSoDien = item.HinhAnhDien,
                NgayThangDien = item.NgayThangDien,
                TenPhong = item.Phong != null ? item.Phong.TenPhong : null
            };

            return Ok(dto);
        }

        // ==========================================================
        // =============== 2. UPLOAD ẢNH → OCR → TRẢ KẾT QUẢ =========
        // ==========================================================
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file, int maPhong)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file upload");

            // Nếu maPhong không được gửi hoặc = 0, cố gắng lấy từ user đăng nhập (hợp đồng đang hiệu lực)
            if (maPhong <= 0)
            {
                var phongFromUser = await ResolvePhongFromCurrentUserAsync();
                if (phongFromUser == null)
                {
                    return BadRequest("Không xác định được phòng từ tài khoản hiện tại. Vui lòng chọn phòng hoặc liên hệ quản trị.");
                }
                maPhong = phongFromUser.Value;
            }

            // Kiểm tra phòng có tồn tại không
            var phong = await _context.Phong.FindAsync(maPhong);
            if (phong == null)
            {
                return BadRequest($"Phòng với mã {maPhong} không tồn tại trong hệ thống");
            }

            // 1. UPLOAD lên CLOUDINARY
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = "chiso_dien"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult == null || uploadResult.SecureUrl == null)
                return BadRequest("Upload ảnh thất bại");

            string imageUrl = uploadResult.SecureUrl.ToString();

            // 2. OCR để lấy số điện
            // Lưu ý: file.OpenReadStream() chỉ đọc được 1 lần, cần reset stream
            Stream imageStream;
            if (file.OpenReadStream().CanSeek)
            {
                imageStream = file.OpenReadStream();
                imageStream.Position = 0;
            }
            else
            {
                // Nếu không thể seek, copy vào MemoryStream
                var ms = new MemoryStream();
                await file.OpenReadStream().CopyToAsync(ms);
                ms.Position = 0;
                imageStream = ms;
            }

            var result = await _ocr.RecognizeElectricityMeterFromStreamAsync(
                imageStream,
                file.FileName
            );

            if (!result.Success)
            {
                // Trả về thông báo lỗi chi tiết hơn
                return BadRequest(result.Message ?? "Không thể đọc được số điện từ ảnh. Vui lòng thử lại hoặc nhập thủ công.");
            }

            string rawText = result.RawText;
            string extractedNumber = result.ExtractedNumber;

            // Lấy số điện mới (có thể là số thập phân: 0.1, 0.2, ...)
            // Database lưu số nguyên, nên cần chuyển đổi:
            // - Nếu OCR trả về "0.2" (5 chữ số) → lưu là 2 (0.2 * 10)
            // - Nếu OCR trả về "1" (4 chữ số) → lưu là 1 (giữ nguyên)
            decimal soDienMoiDecimal;
            if (!decimal.TryParse(extractedNumber, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out soDienMoiDecimal))
            {
                return BadRequest($"Không tìm được số điện hợp lệ từ kết quả OCR: '{extractedNumber}'. Vui lòng nhập thủ công.");
            }
            
            // Nếu là số thập phân (có dấu chấm), nhân 10 để chuyển sang số nguyên
            // Ví dụ: 0.1 -> 1, 0.2 -> 2
            // Nếu là số nguyên, giữ nguyên
            // Ví dụ: 1 -> 1, 12345 -> 12345
            int soDienMoi;
            if (extractedNumber.Contains("."))
            {
                // Số thập phân: nhân 10
                soDienMoi = (int)Math.Round(soDienMoiDecimal * 10);
            }
            else
            {
                // Số nguyên: giữ nguyên
                soDienMoi = (int)soDienMoiDecimal;
            }


            // 3. Lấy chỉ số tháng trước
            var last = await _context.ChiSoDien
                .Where(x => x.MaPhong == maPhong)
                .OrderByDescending(x => x.NgayThangDien)
                .FirstOrDefaultAsync();

            int soDienCu = last?.SoDienMoi ?? 0;

            var tieuThu = soDienMoi - soDienCu;
            if (tieuThu < 0) tieuThu = 0;

            // Tìm bậc giá điện phù hợp
            var gd = await _context.GiaDien
                .Where(x => tieuThu >= x.TuSoDien && tieuThu <= x.DenSoDien)
                .FirstOrDefaultAsync();

            if (gd == null)
            {
                return BadRequest("Không tìm thấy bậc điện phù hợp");
            }

            var model = new ChiSoDien
            {
                MaPhong = maPhong,
                MaGiaDien = gd.MaGiaDien,
                SoDienCu = soDienCu,
                SoDienMoi = soDienMoi,
                SoDienTieuThu = tieuThu,
                TienDien = tieuThu * gd.GiaTienDien,
                NgayThangDien = DateTime.Now,
                HinhAnhDien = imageUrl
            };

            try
            {
                _context.ChiSoDien.Add(model);
                await _context.SaveChangesAsync();

                // Lấy lại thông tin giá điện để trả về đơn giá
                var giaDienInfo = await _context.GiaDien.FindAsync(model.MaGiaDien);
                
                // Tính đơn giá trung bình (vì tính theo bậc thang nên không có đơn giá cố định)
                decimal donGiaTrungBinh = model.SoDienTieuThu > 0 
                    ? model.TienDien / model.SoDienTieuThu 
                    : 0;
                
                return Ok(new
                {
                    success = true,
                    message = "Đã lưu chỉ số điện và tính tiền thành công!",
                    MaDien = model.MaDien,
                    imageUrl,
                    soDienCu,
                    soDienMoi,
                    tieuThu = model.SoDienTieuThu,
                    tienDien = model.TienDien,
                    ngayThangDien = model.NgayThangDien,
                    // Thông tin giá để frontend hiển thị đơn giá (đơn giá trung bình vì tính theo bậc thang)
                    donGia = donGiaTrungBinh,
                    giaTienDien = donGiaTrungBinh, // Đơn giá trung bình
                    bacDien = giaDienInfo?.BacDien ?? 0,
                    // Thông tin bổ sung để frontend biết đã lưu
                    daLuu = true
                });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                // Xử lý lỗi Foreign Key hoặc các lỗi database khác
                if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    if (sqlEx.Number == 547) // Foreign key constraint violation
                    {
                        return BadRequest($"Lỗi: Phòng với mã {maPhong} không tồn tại trong hệ thống. Vui lòng kiểm tra lại.");
                    }
                    return BadRequest($"Lỗi database: {sqlEx.Message}");
                }
                return BadRequest($"Lỗi khi lưu chỉ số điện: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi không xác định: {ex.Message}");
            }
        }

        private int ExtractNumber(string input)
        {
            var digits = new string(input.Where(char.IsDigit).ToArray());
            return string.IsNullOrEmpty(digits) ? -1 : int.Parse(digits);
        }

        // ==========================================================
        // =============== 3. TẠO CHỈ SỐ ĐIỆN (+ AUTO TÍNH) ==========
        // ==========================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] ChiSoDienDtoCreate dto)
        {
            // Kiểm tra phòng có tồn tại không
            var phong = await _context.Phong.FindAsync(dto.MaPhong);
            if (phong == null)
            {
                return BadRequest($"Phòng với mã {dto.MaPhong} không tồn tại trong hệ thống");
            }

            // tìm tháng trước → lấy SoDienCu
            var last = await _context.ChiSoDien
                .Where(x => x.MaPhong == dto.MaPhong)
                .OrderByDescending(x => x.NgayThangDien)
                .FirstOrDefaultAsync();

            int soDienCu = last?.SoDienMoi ?? dto.SoDienCu;

            int tieuThu = dto.SoDienMoi - soDienCu;

            // tìm giá điện theo bậc
            var gd = await _context.GiaDien
                .Where(x => tieuThu >= x.TuSoDien && tieuThu <= x.DenSoDien)
                .FirstOrDefaultAsync();

            if (gd == null)
                return BadRequest("Không tìm thấy bậc điện phù hợp");

            var model = new ChiSoDien
            {
                MaPhong = dto.MaPhong,
                SoDienCu = soDienCu,
                SoDienMoi = dto.SoDienMoi,
                MaGiaDien = gd.MaGiaDien,
                TienDien = tieuThu * gd.GiaTienDien,
                NgayThangDien = dto.NgayThangDien,
                HinhAnhDien = dto.AnhChiSoDien
            };

            try
            {
                _context.ChiSoDien.Add(model);
                await _context.SaveChangesAsync();

                return Ok("Đã thêm chỉ số điện");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                // Xử lý lỗi Foreign Key hoặc các lỗi database khác
                if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    if (sqlEx.Number == 547) // Foreign key constraint violation
                    {
                        return BadRequest($"Lỗi: Phòng với mã {dto.MaPhong} không tồn tại trong hệ thống. Vui lòng kiểm tra lại.");
                    }
                    return BadRequest($"Lỗi database: {sqlEx.Message}");
                }
                return BadRequest($"Lỗi khi lưu chỉ số điện: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi không xác định: {ex.Message}");
            }
        }
        // ==========================================================
        // =============== 4. UPDATE (SỬA CHỈ SỐ ĐIỆN) =================
        // ==========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] ChiSoDienDtoCreate dto)
        {
            var chiSo = await _context.ChiSoDien.FindAsync(id);

            if (chiSo == null)
                return NotFound("Không tìm thấy chỉ số điện.");

            // Lấy chỉ số tháng trước (ngoại trừ bản ghi hiện tại)
            var previous = await _context.ChiSoDien
                .Where(x => x.MaPhong == chiSo.MaPhong && x.MaDien != id)
                .OrderByDescending(x => x.NgayThangDien)
                .FirstOrDefaultAsync();

            int soDienCu = previous?.SoDienMoi ?? dto.SoDienCu;

            int tieuThu = dto.SoDienMoi - soDienCu;

            if (tieuThu < 0)
                return BadRequest("Số điện mới phải lớn hơn số điện cũ.");

            // lấy giá theo bậc
            var gd = await _context.GiaDien
                .Where(x => tieuThu >= x.TuSoDien && tieuThu <= x.DenSoDien)
                .FirstOrDefaultAsync();

            if (gd == null)
                return BadRequest("Không tìm thấy bậc điện phù hợp.");

            // cập nhật dữ liệu
            chiSo.SoDienCu = soDienCu;
            chiSo.SoDienMoi = dto.SoDienMoi;
            chiSo.MaGiaDien = gd.MaGiaDien;
            chiSo.TienDien = tieuThu * gd.GiaTienDien;
            chiSo.NgayThangDien = dto.NgayThangDien;

            if (!string.IsNullOrEmpty(dto.AnhChiSoDien))
                chiSo.HinhAnhDien = dto.AnhChiSoDien;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Cập nhật chỉ số điện thành công",
                soDienCu,
                soDienMoi = dto.SoDienMoi,
                tieuThu,
                tienDien = tieuThu * gd.GiaTienDien
            });
        }

    }
}
