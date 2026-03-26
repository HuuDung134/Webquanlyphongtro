using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using DoAnCoSo.Services;


namespace DoAnCoSo.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        public class PhongController : ControllerBase
        {
            private readonly ApplicationDbContext _context;
            private readonly Cloudinary _cloudinary;
            private readonly IMqttDoorService _doorService;


        public PhongController(ApplicationDbContext context, Cloudinary cloudinary, IMqttDoorService doorService)
            {
                _context = context;
                _cloudinary = cloudinary;
                _doorService = doorService;

        }
        private async Task AddDoorLogAsync(int maPhong, string hanhDong, string nguoiThucHien, string ghiChu = "")
        {
            _context.LichSuDongMo.Add(new LichSuDongMo
            {
                MaPhong = maPhong,
                HanhDong = hanhDong,
                NguoiThucHien = nguoiThucHien,
                GhiChu = ghiChu ?? string.Empty
            });
            await _context.SaveChangesAsync();
        }


        // GET: api/Phong
        [HttpGet]
            public async Task<ActionResult<IEnumerable<Phong>>> GetPhong()
            {
                var list = await _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    
                    .ToListAsync();

                return Ok(list);
            }

            // GET: api/Phong/5
            [HttpGet("{id}")]
            public async Task<ActionResult<Phong>> GetPhong(int id)
            {
                var phong = await _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    //.Include(p => p.TrangThai)
                    .FirstOrDefaultAsync(p => p.MaPhong == id);

                if (phong == null)
                {
                    return NotFound();
                }

                return Ok(phong);
            }

            // GET: api/Phong/NhaTro/5
            [HttpGet("NhaTro/{nhaTroId}")]
            public async Task<ActionResult<IEnumerable<Phong>>> GetPhongByNhaTro(int nhaTroId)
            {
                var list = await _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    //.Include(p => p.TrangThai)
                    //.Where(p => p.MaNhaTro == nhaTroId)
                    .ToListAsync();

                return Ok(list);
            }
        [HttpPost("nguoi-thue-dieu-khien")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> DieuKhienCuaCaNhan([FromBody] DieuKhienRequest request)
        {
            // Lấy ID user từ Token
            var userIdStr = User.FindFirst("MaNguoiDung")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized("Không xác định được danh tính.");

            int userId = int.Parse(userIdStr);
            var now = DateTime.Now;

            // Lấy mã người thuê từ bảng NguoiThue (User ↔ NguoiThue là 1-1)
            var nguoiThue = await _context.NguoiThue.FirstOrDefaultAsync(nt => nt.MaNguoiDung == userId);
            if (nguoiThue == null) return BadRequest("Tài khoản chưa gắn với người thuê.");

            // Tìm phòng người này đang thuê
            var hopDong = await _context.HopDong
                .Include(h => h.Phong)
                .Where(h => h.MaNguoiThue == nguoiThue.MaNguoiThue && h.NgayBatDau <= now && (!h.NgayKetThuc.HasValue || h.NgayKetThuc.Value >= now))
                .OrderByDescending(h => h.NgayBatDau)
                .FirstOrDefaultAsync();

            if (hopDong == null) return BadRequest("Bạn không có hợp đồng thuê phòng hiệu lực.");

            try
            {
                if (request.HanhDong == "OPEN") await _doorService.MoCuaAsync(hopDong.MaPhong.ToString());
                else if (request.HanhDong == "CLOSE") await _doorService.DongCuaAsync(hopDong.MaPhong.ToString());
                else return BadRequest("Hành động không hợp lệ.");

                var actor = string.IsNullOrWhiteSpace(nguoiThue.HoTen) ? $"NguoiThue_{nguoiThue.MaNguoiThue}" : nguoiThue.HoTen;
                await AddDoorLogAsync(hopDong.MaPhong, request.HanhDong, actor, "Khách thuê tự điều khiển");

                return Ok(new { message = $"Đã {request.HanhDong} cửa phòng {hopDong.Phong.TenPhong}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = "Lỗi thiết bị: " + ex.Message }); }
        }

        public class DieuKhienRequest { public string HanhDong { get; set; } }

        // ==========================================
        // 3. API CHO ADMIN MỞ CỬA 
        // ==========================================
        [HttpPost("mo-cua-tu-xa/{id}")]
        public async Task<IActionResult> MoCuaTuXa(int id)
        {
            try
            {
                await _doorService.MoCuaAsync(id.ToString());
                await AddDoorLogAsync(id, "OPEN", "Admin", "Admin mở cửa từ xa");
                return Ok(new { message = "Mở cửa thành công" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("dong-cua-tu-xa/{id}")]
        public async Task<IActionResult> DongCuaTuXa(int id)
        {
            try
            {
                await _doorService.DongCuaAsync(id.ToString());
                await AddDoorLogAsync(id, "CLOSE", "Admin", "Admin đóng cửa từ xa");
                return Ok(new { message = "Đóng cửa thành công" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ==========================================
        // 3b. API XEM LỊCH SỬ ĐÓNG/MỞ CỬA
        // ==========================================
        [HttpGet("{id}/lich-su-dong-mo")]
        public async Task<ActionResult<IEnumerable<object>>> GetLichSuDongMo(int id)
        {
            var logs = await _context.LichSuDongMo
                .Where(x => x.MaPhong == id)
                .OrderByDescending(x => x.ThoiGian)
                .Select(x => new
                {
                    x.ThoiGian,
                    x.HanhDong,
                    x.NguoiThucHien,
                    x.GhiChu
                })
                .ToListAsync();

            return Ok(logs);
        }

        // Alias cho frontend cũ: /api/Phong/LichSu/{id}
        [HttpGet("LichSu/{id}")]
        public Task<ActionResult<IEnumerable<object>>> GetLichSuDongMoAlias(int id) => GetLichSuDongMo(id);

        // GET: api/Phong/TrangThai/5
        [HttpGet("TrangThai/{trangThaiId}")]
            public async Task<ActionResult<IEnumerable<Phong>>> GetPhongByTrangThai(int trangThaiId)
            {
                var list = await _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    //.Include(p => p.TrangThai)
                    .Where(p => p.TrangThai == trangThaiId)
                    .ToListAsync();

                return Ok(list);
            }

            // GET: api/Phong/Trong
            [HttpGet("Trong")]
            public async Task<ActionResult<IEnumerable<Phong>>> GetPhongTrong()
            {
                var now = DateTime.Now;

                var list = await _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    .Where(p => !_context.HopDong.Any(hd =>
                        hd.MaPhong == p.MaPhong &&
                        hd.NgayBatDau <= now &&
                        (!hd.NgayKetThuc.HasValue || hd.NgayKetThuc.Value >= now)))
                    .ToListAsync();

                return Ok(list);
            }



            // POST: api/Phong
            [HttpPost]
            public async Task<ActionResult<Phong>> PostPhong(Phong phong)
            {
                // Kiểm tra xem các khóa ngoại có tồn tại không
                var nhaTro = await _context.NhaTro.FindAsync(phong.MaNhaTro);
                if (nhaTro == null)
                {
                    return BadRequest("Nhà trọ không tồn tại");
                }

                var loaiPhong = await _context.LoaiPhong.FindAsync(phong.MaLoaiPhong);
                if (loaiPhong == null)
                {
                    return BadRequest("Loại phòng không tồn tại");
                }

                //var trangThai = await _context.TrangThai.FindAsync(phong.MaTrangThai);
                //if (trangThai == null)
                //{
                //    return BadRequest("Trạng thái không tồn tại");
                //}

                // Kiểm tra tên phòng đã tồn tại chưa (có thể kiểm tra trong cùng nhà trọ hoặc toàn bộ phòng tùy yêu cầu)
                bool tenPhongDaTonTai = await _context.Phong.AnyAsync(p => p.TenPhong == phong.TenPhong);

                if (tenPhongDaTonTai)
                {
                    return BadRequest("Tên phòng đã tồn tại, vui lòng chọn tên khác.");
                }


                // Tạo phòng mới với các thông tin cơ bản
                var phongMoi = new Phong
                {
                    MaNhaTro = phong.MaNhaTro,
                    MaLoaiPhong = phong.MaLoaiPhong,
                    TenPhong = phong.TenPhong,
                    DienTich = phong.DienTich,
                    GiaPhong = phong.GiaPhong,
                    SucChua = phong.SucChua,
                    MoTa = phong.MoTa,
                    HinhAnh = phong.HinhAnh,
                };

                _context.Phong.Add(phongMoi);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPhong), new { id = phongMoi.MaPhong }, phongMoi);
            }

            // POST: api/Phong/UploadImage
            [HttpPost("UploadImage")]
            public async Task<IActionResult> UploadImage(IFormFile file)
            {
                try
                {
                    if (file == null || file.Length == 0)
                        return BadRequest("Vui lòng chọn file ảnh.");

                    // Kiểm tra định dạng file
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                        return BadRequest("Chỉ chấp nhận file ảnh có định dạng: .jpg, .jpeg, .png, .gif");

                    // Kiểm tra kích thước file (giới hạn 5MB)
                    if (file.Length > 5 * 1024 * 1024)
                        return BadRequest("Kích thước file không được vượt quá 5MB");

                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.FileName, file.OpenReadStream()),
                        Folder = "phong_images", // Tạo thư mục riêng cho ảnh phòng
                        Transformation = new Transformation()
                            .Width(800)
                            .Height(600)
                            .Crop("fill")
                            .Quality("auto")
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return Ok(new
                        {
                            url = uploadResult.SecureUrl?.AbsoluteUri,
                            publicId = uploadResult.PublicId
                        });
                    }
                    else
                    {
                        return StatusCode(500, "Lỗi upload ảnh lên Cloudinary.");
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Lỗi xử lý ảnh: {ex.Message}");
                }
            }

            // ================================
            // PUT
            // ================================

            // PUT: api/Phong/5
            [HttpPut("{id}")]
            public async Task<IActionResult> PutPhong(int id, Phong phong)
            {
                if (id != phong.MaPhong)
                {
                    return BadRequest("Mã phòng không khớp với tham số URL.");
                }

                _context.Entry(phong).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhongExists(id))
                    {
                        return NotFound("Không tìm thấy phòng cần cập nhật.");
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }

            // ================================
            // DELETE
            // ================================

            // DELETE: api/Phong/5
            [HttpDelete("{id}")]
            public async Task<IActionResult> DeletePhong(int id)
            {
                var phong = await _context.Phong.FindAsync(id);
                if (phong == null)
                {
                    return NotFound("Không tìm thấy phòng cần xóa.");
                }

                _context.Phong.Remove(phong);
                await _context.SaveChangesAsync();

                return NoContent();
            }

            private bool PhongExists(int id) =>
                _context.Phong.Any(e => e.MaPhong == id);
        }
    }
