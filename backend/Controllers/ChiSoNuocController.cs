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
    public class ChiSoNuocController : ControllerBase
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

        public ChiSoNuocController(
            ApplicationDbContext context,
            IOCRAPIService ocrService,
            Cloudinary cloudinary)
        {
            _context = context;
            _ocr = ocrService;
            _cloudinary = cloudinary;
        }

        // ==========================================================
        // =============== 1. LẤY TẤT CẢ CHỈ SỐ NƯỚC =================
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.ChiSoNuoc
                .Include(x => x.GiaNuoc)
                .Include(x => x.Phong)
                .OrderByDescending(x => x.NgayThangNuoc)
                .Select(x => new ChiSoNuocDto
                {
                    MaNuoc = x.MaNuoc,
                    MaPhong = x.MaPhong,
                    MaGiaNuoc = x.MaGiaNuoc,
                    SoNuocCu = x.SoNuocCu,
                    SoNuocMoi = x.SoNuocMoi,
                    SoNuocTieuThu = x.SoNuocMoi - x.SoNuocCu,
                    TienNuoc = x.TienNuoc,
                    AnhChiSoNuoc = x.HinhAnhNuoc,
                    NgayThangNuoc = x.NgayThangNuoc,
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
            var item = await _context.ChiSoNuoc
                .Include(x => x.Phong)
                .FirstOrDefaultAsync(x => x.MaNuoc == id);

            if (item == null) return NotFound("Không tìm thấy chỉ số nước.");

            var dto = new ChiSoNuocDto
            {
                MaNuoc = item.MaNuoc,
                MaPhong = item.MaPhong,
                MaGiaNuoc = item.MaGiaNuoc,
                SoNuocCu = item.SoNuocCu,
                SoNuocMoi = item.SoNuocMoi,
                SoNuocTieuThu = item.SoNuocMoi - item.SoNuocCu,
                TienNuoc = item.TienNuoc,
                AnhChiSoNuoc = item.HinhAnhNuoc,
                NgayThangNuoc = item.NgayThangNuoc,
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
                Folder = "chiso_nuoc"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult == null || uploadResult.SecureUrl == null)
                return BadRequest("Upload ảnh thất bại");

            string imageUrl = uploadResult.SecureUrl.ToString();

            // 2. OCR để lấy số nước
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

            var result = await _ocr.RecognizeWaterMeterFromStreamAsync(
                imageStream,
                file.FileName
            );

            if (!result.Success)
            {
                // Trả về thông báo lỗi chi tiết hơn
                return BadRequest(result.Message ?? "Không thể đọc được số nước từ ảnh. Vui lòng thử lại hoặc nhập thủ công.");
            }

            string rawText = result.RawText;
            string extractedNumber = result.ExtractedNumber;

            // Lấy số nước mới (có thể là số thập phân: 0.1, 0.2, ...)
            // Database lưu số nguyên, nên cần chuyển đổi:
            // - Nếu OCR trả về "0.1" (5 chữ số) → lưu là 1 (0.1 * 10)
            // - Nếu OCR trả về "1" (4 chữ số) → lưu là 1 (giữ nguyên)
            decimal soNuocMoiDecimal;
            if (!decimal.TryParse(extractedNumber, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out soNuocMoiDecimal))
            {
                return BadRequest($"Không tìm được số nước hợp lệ từ kết quả OCR: '{extractedNumber}'. Vui lòng nhập thủ công.");
            }
            
            // Nếu là số thập phân (có dấu chấm), nhân 10 để chuyển sang số nguyên
            // Ví dụ: 0.1 -> 1, 0.2 -> 2
            // Nếu là số nguyên, giữ nguyên
            // Ví dụ: 1 -> 1, 154 -> 154
            int soNuocMoi;
            if (extractedNumber.Contains("."))
            {
                // Số thập phân: nhân 10
                soNuocMoi = (int)Math.Round(soNuocMoiDecimal * 10);
            }
            else
            {
                // Số nguyên: giữ nguyên
                soNuocMoi = (int)soNuocMoiDecimal;
            }

            // 3. Lấy chỉ số tháng trước
            var last = await _context.ChiSoNuoc
                .Where(x => x.MaPhong == maPhong)
                .OrderByDescending(x => x.NgayThangNuoc)
                .FirstOrDefaultAsync();

            int soNuocCu = last?.SoNuocMoi ?? 0;

            var tieuThu = soNuocMoi - soNuocCu;
            if (tieuThu < 0) tieuThu = 0;

            // Lấy danh sách giá nước theo bậc
            var giaNuocList = await _context.GiaNuoc.OrderBy(g => g.BacNuoc).ToListAsync();
            if (!giaNuocList.Any())
                return BadRequest("Chưa có cấu hình giá nước");

            decimal tienNuoc = TinhTienNuoc(tieuThu, giaNuocList);

            // Chọn bậc giá phù hợp
            var giaNuoc = giaNuocList.FirstOrDefault(g => tieuThu >= g.TuSoNuoc && tieuThu <= g.DenSoNuoc)
                           ?? giaNuocList.OrderByDescending(g => g.BacNuoc).First();

            var model = new ChiSoNuoc
            {
                MaPhong = maPhong,
                SoNuocCu = soNuocCu,
                SoNuocMoi = soNuocMoi,
                SoNuocTieuThu = tieuThu,
                MaGiaNuoc = giaNuoc.MaGiaNuoc,
                TienNuoc = tienNuoc,
                NgayThangNuoc = DateTime.Now,
                HinhAnhNuoc = imageUrl
            };

            try
            {
                _context.ChiSoNuoc.Add(model);
                await _context.SaveChangesAsync();

                // Lấy lại thông tin giá nước để trả về đơn giá
                var giaNuocInfo = await _context.GiaNuoc.FindAsync(model.MaGiaNuoc);
                
                // Tính đơn giá trung bình (vì tính theo bậc thang nên không có đơn giá cố định)
                decimal donGiaTrungBinh = model.SoNuocTieuThu > 0 
                    ? model.TienNuoc / model.SoNuocTieuThu 
                    : 0;
                
                return Ok(new
                {
                    success = true,
                    message = "Đã lưu chỉ số nước và tính tiền thành công!",
                    MaNuoc = model.MaNuoc,
                    imageUrl,
                    soNuocCu,
                    soNuocMoi,
                    tieuThu = model.SoNuocTieuThu,
                    tienNuoc = model.TienNuoc,
                    ngayThangNuoc = model.NgayThangNuoc,
                    // Thông tin giá để frontend hiển thị đơn giá (đơn giá trung bình vì tính theo bậc thang)
                    donGia = donGiaTrungBinh,
                    giaTienNuoc = donGiaTrungBinh, // Đơn giá trung bình
                    bacNuoc = giaNuocInfo?.BacNuoc ?? 0,
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
                return BadRequest($"Lỗi khi lưu chỉ số nước: {ex.Message}");
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
        // =============== 3. TẠO CHỈ SỐ NƯỚC (+ AUTO TÍNH) ==========
        // ==========================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] ChiSoNuocDtoCreate dto)
        {
            // Kiểm tra phòng có tồn tại không
            var phong = await _context.Phong.FindAsync(dto.MaPhong);
            if (phong == null)
            {
                return BadRequest($"Phòng với mã {dto.MaPhong} không tồn tại trong hệ thống");
            }

            // Tìm tháng trước → lấy SoNuocCu
            var last = await _context.ChiSoNuoc
                .Where(x => x.MaPhong == dto.MaPhong)
                .OrderByDescending(x => x.NgayThangNuoc)
                .FirstOrDefaultAsync();

            int soNuocCu = last?.SoNuocMoi ?? dto.SoNuocCu;

            int tieuThu = dto.SoNuocMoi - soNuocCu;

            if (tieuThu < 0)
                return BadRequest("Số nước mới phải lớn hơn số nước cũ.");

            // Tính tiền nước theo bậc thang
            var giaNuocList = await _context.GiaNuoc.OrderBy(g => g.BacNuoc).ToListAsync();
            if (!giaNuocList.Any())
                return BadRequest("Chưa có cấu hình giá nước");

            decimal tienNuoc = TinhTienNuoc(tieuThu, giaNuocList);

            // Tìm bậc giá nước phù hợp (bậc đầu tiên mà số nước tiêu thụ nằm trong khoảng)
            var giaNuoc = giaNuocList
                .FirstOrDefault(g => tieuThu >= g.TuSoNuoc && tieuThu <= g.DenSoNuoc);

            if (giaNuoc == null)
            {
                // Nếu không tìm thấy bậc phù hợp, dùng bậc cuối cùng
                giaNuoc = giaNuocList.OrderByDescending(g => g.BacNuoc).FirstOrDefault();
            }

            if (giaNuoc == null)
                return BadRequest("Không tìm thấy bậc giá nước phù hợp");

            var model = new ChiSoNuoc
            {
                MaPhong = dto.MaPhong,
                SoNuocCu = soNuocCu,
                SoNuocMoi = dto.SoNuocMoi,
                SoNuocTieuThu = tieuThu,
                MaGiaNuoc = giaNuoc.MaGiaNuoc,
                TienNuoc = tienNuoc,
                NgayThangNuoc = dto.NgayThangNuoc,
                HinhAnhNuoc = dto.AnhChiSoNuoc
            };

            try
            {
                _context.ChiSoNuoc.Add(model);
                await _context.SaveChangesAsync();

                return Ok("Đã thêm chỉ số nước");
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
                return BadRequest($"Lỗi khi lưu chỉ số nước: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi không xác định: {ex.Message}");
            }
        }

        // ==========================================================
        // =============== 4. UPDATE (SỬA CHỈ SỐ NƯỚC) =================
        // ==========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] ChiSoNuocDtoCreate dto)
        {
            var chiSo = await _context.ChiSoNuoc.FindAsync(id);

            if (chiSo == null)
                return NotFound("Không tìm thấy chỉ số nước.");

            // Lấy chỉ số tháng trước (ngoại trừ bản ghi hiện tại)
            var previous = await _context.ChiSoNuoc
                .Where(x => x.MaPhong == chiSo.MaPhong && x.MaNuoc != id)
                .OrderByDescending(x => x.NgayThangNuoc)
                .FirstOrDefaultAsync();

            int soNuocCu = previous?.SoNuocMoi ?? dto.SoNuocCu;

            int tieuThu = dto.SoNuocMoi - soNuocCu;

            if (tieuThu < 0)
                return BadRequest("Số nước mới phải lớn hơn số nước cũ.");

            // Tính tiền nước theo bậc thang
            var giaNuocList = await _context.GiaNuoc.OrderBy(g => g.BacNuoc).ToListAsync();
            if (!giaNuocList.Any())
                return BadRequest("Chưa có cấu hình giá nước");

            decimal tienNuoc = TinhTienNuoc(tieuThu, giaNuocList);

            // Tìm bậc giá nước phù hợp
            var giaNuoc = giaNuocList
                .FirstOrDefault(g => tieuThu >= g.TuSoNuoc && tieuThu <= g.DenSoNuoc);

            if (giaNuoc == null)
            {
                // Nếu không tìm thấy bậc phù hợp, dùng bậc cuối cùng
                giaNuoc = giaNuocList.OrderByDescending(g => g.BacNuoc).FirstOrDefault();
            }

            if (giaNuoc == null)
                return BadRequest("Không tìm thấy bậc giá nước phù hợp.");

            // Cập nhật dữ liệu
            chiSo.SoNuocCu = soNuocCu;
            chiSo.SoNuocMoi = dto.SoNuocMoi;
            chiSo.SoNuocTieuThu = tieuThu;
            chiSo.MaGiaNuoc = giaNuoc.MaGiaNuoc;
            chiSo.TienNuoc = tienNuoc;
            chiSo.NgayThangNuoc = dto.NgayThangNuoc;

            if (!string.IsNullOrEmpty(dto.AnhChiSoNuoc))
                chiSo.HinhAnhNuoc = dto.AnhChiSoNuoc;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Cập nhật chỉ số nước thành công",
                soNuocCu,
                soNuocMoi = dto.SoNuocMoi,
                tieuThu,
                tienNuoc
            });
        }

        // ==========================================================
        // =============== 5. DELETE ===============================
        // ==========================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var chiSoNuoc = await _context.ChiSoNuoc.FindAsync(id);
            if (chiSoNuoc == null)
                return NotFound("Không tìm thấy chỉ số nước.");

            _context.ChiSoNuoc.Remove(chiSoNuoc);
            await _context.SaveChangesAsync();

            return Ok("Đã xóa chỉ số nước");
        }

        // ==========================================================
        // =============== HELPER: TÍNH TIỀN NƯỚC BẬC THANG =========
        // ==========================================================
        private decimal TinhTienNuoc(int soNuocTieuThu, List<GiaNuoc> giaNuocList)
        {
            decimal tongTien = 0;
            int soNuocConLai = soNuocTieuThu;

            foreach (var giaNuoc in giaNuocList.OrderBy(g => g.BacNuoc))
            {
                if (soNuocConLai <= 0) break;

                int soNuocTrongBac = Math.Min(
                    soNuocConLai,
                    giaNuoc.DenSoNuoc - giaNuoc.TuSoNuoc + 1
                );

                if (soNuocTrongBac > 0)
                    tongTien += soNuocTrongBac * giaNuoc.GiaTienNuoc;

                soNuocConLai -= soNuocTrongBac;
            }

            return tongTien;
        }
    }
}
