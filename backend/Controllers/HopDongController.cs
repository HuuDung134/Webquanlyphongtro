using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using DoAnCoSo.DTOs;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace DoAnCoSo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HopDongController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;

        public HopDongController(
            ApplicationDbContext context, 
            Cloudinary cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        // =====================================
        // GET ALL
        // =====================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HopDong>>> GetAll()
        {
            try
            {
                var list = await _context.HopDong
                    .Include(h => h.Phong)
                    .Include(h => h.NguoiThue)
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi tải danh sách hợp đồng: {ex.Message}" });
            }
        }

        // =====================================
        // GET PHÒNG KHÔNG CÓ HỢP ĐỒNG
        // =====================================
        [HttpGet("Phong/KhongCoHopDong")]
        public async Task<ActionResult<IEnumerable<Phong>>> GetPhongKhongCoHopDong()
        {
            try
            {
                var now = DateTime.Now;
                
                // Sử dụng subquery thay vì Contains để tránh lỗi SQL syntax
                var phongKhongCoHopDong = await _context.Phong
                    .Where(p => p.TrangThai != 2 && // Không bảo trì
                                !_context.HopDong.Any(hd => 
                                    hd.MaPhong == p.MaPhong && 
                                    hd.NgayBatDau <= now && 
                                    (!hd.NgayKetThuc.HasValue || hd.NgayKetThuc.Value >= now)))
                    .Include(p => p.LoaiPhong)
                    .Include(p => p.NhaTro)
                    .ToListAsync();

                return Ok(phongKhongCoHopDong);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi tải danh sách phòng: {ex.Message}" });
            }
        }

        // =====================================
        // GET NGƯỜI THUÊ KHÔNG CÓ HỢP ĐỒNG
        // =====================================
        [HttpGet("NguoiThue/KhongCoHopDong")]
        public async Task<ActionResult<IEnumerable<NguoiThue>>> GetNguoiThueKhongCoHopDong()
        {
            try
            {
                var now = DateTime.Now;
                
                // Sử dụng subquery thay vì Contains để tránh lỗi SQL syntax
                var nguoiThueKhongCoHopDong = await _context.NguoiThue
                    .Where(nt => !_context.HopDong.Any(hd => 
                        hd.MaNguoiThue == nt.MaNguoiThue && 
                        hd.NgayBatDau <= now && 
                        (!hd.NgayKetThuc.HasValue || hd.NgayKetThuc.Value >= now)))
                    .ToListAsync();

                return Ok(nguoiThueKhongCoHopDong);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi tải danh sách người thuê: {ex.Message}" });
            }
        }

        // =====================================
        // GET BY ID
        // =====================================
        [HttpGet("{id}")]
        public async Task<ActionResult<HopDong>> GetById(int id)
        {
            var hd = await _context.HopDong
                .Include(h => h.Phong)
                .Include(h => h.NguoiThue)
                .FirstOrDefaultAsync(x => x.MaHopDong == id);

            if (hd == null) return NotFound("Không tìm thấy hợp đồng.");

            return Ok(hd);
        }

        // =====================================
        // CREATE
        // =====================================
        [HttpPost]
        public async Task<ActionResult> Create(CreateHopDongDto dto)
        {
            var phong = await _context.Phong.FindAsync(dto.MaPhong);
            if (phong == null) return BadRequest("Phòng không tồn tại.");

            if (phong.TrangThai == 1)
                return BadRequest("Phòng này đang thuê, không thể tạo hợp đồng mới.");

            var nguoiThue = await _context.NguoiThue.FindAsync(dto.MaNguoiThue);
            if (nguoiThue == null) return BadRequest("Người thuê không tồn tại.");

            var hd = new HopDong
            {
                MaPhong = dto.MaPhong,
                MaNguoiThue = dto.MaNguoiThue,
                NgayBatDau = dto.NgayBatDau,
                NgayKetThuc = dto.NgayKetThuc,
                TienCoc = dto.TienCoc,
                NoiDung = dto.NoiDung
            };

            _context.HopDong.Add(hd);

            // UPDATE TRẠNG THÁI PHÒNG → ĐANG THUÊ
            phong.TrangThai = 1;

            await _context.SaveChangesAsync();

            return Ok(hd);
        }

        // =====================================
        // UPDATE
        // =====================================
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, HopDongUpdateDto dto)
        {
            if (id != dto.MaHopDong)
                return BadRequest("ID không khớp.");

            var hd = await _context.HopDong.FindAsync(id);
            if (hd == null) return NotFound("Không tìm thấy hợp đồng.");

            var phong = await _context.Phong.FindAsync(dto.MaPhong);
            if (phong == null) return BadRequest("Phòng không tồn tại.");

            hd.MaNguoiThue = dto.MaNguoiThue;
            hd.MaPhong = dto.MaPhong;
            hd.NgayBatDau = dto.NgayBatDau;
            hd.NgayKetThuc = dto.NgayKetThuc;
            hd.TienCoc = dto.TienCoc;
            hd.NoiDung = dto.NoiDung;

            // Kiểm tra hợp đồng còn hiệu lực
            if (dto.NgayKetThuc < DateTime.Now)
            {
                phong.TrangThai = 0; // Hết hạn → trả phòng
            }
            else
            {
                phong.TrangThai = 1; // Còn hiệu lực → đang thuê
            }

            await _context.SaveChangesAsync();

            return Ok(hd);
        }

        // =====================================
        // DELETE
        // =====================================
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var hd = await _context.HopDong.FindAsync(id);
            if (hd == null) return NotFound("Không tìm thấy hợp đồng.");

            var phong = await _context.Phong.FindAsync(hd.MaPhong);

            _context.HopDong.Remove(hd);

            // Khi xóa hợp đồng → trả phòng
            if (phong != null && phong.TrangThai != 2) // không đổi nếu phòng đang bảo trì
            {
                phong.TrangThai = 0;
            }

            await _context.SaveChangesAsync();

            return Ok("Xóa thành công.");
        }

        // =====================================
        // KẾT THÚC HỢP ĐỒNG (thao tác thủ công)
        // =====================================
        [HttpPut("KetThuc/{id}")]
        public async Task<ActionResult> KetThucHopDong(int id)
        {
            var hd = await _context.HopDong.FindAsync(id);
            if (hd == null) return NotFound("Không tìm thấy hợp đồng.");

            var phong = await _context.Phong.FindAsync(hd.MaPhong);
            if (phong == null) return BadRequest("Phòng không tồn tại.");

            hd.NgayKetThuc = DateTime.Now;

            if (phong.TrangThai != 2) // Nếu không bảo trì → đưa về Trống
            {
                phong.TrangThai = 0;
            }

            await _context.SaveChangesAsync();

            return Ok("Hợp đồng đã kết thúc.");
        }

    }
}
