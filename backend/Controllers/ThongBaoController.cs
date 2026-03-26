using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Models;
using DoAnCoSo.Data;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace DoAnCoSo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThongBaoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public ThongBaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả thông báo còn hiệu lực
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var now = DateTime.Now;
            var list = await _context.ThongBao
                .Where(tb => !tb.ExpireAt.HasValue || tb.ExpireAt > now)
                .OrderByDescending(tb => tb.CreatedAt)
                .ToListAsync();
            return Ok(list);
        }

        // Thêm thông báo mới (chung cho tất cả)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ThongBao tb)
        {
            tb.CreatedAt = DateTime.Now;
            _context.ThongBao.Add(tb);
            await _context.SaveChangesAsync();
            return Ok(tb);
        }

        /// <summary>
        /// Tạo thông báo cho 1 người thuê (chỉ lưu vào DB, không gửi SMS/Email)
        /// </summary>
        [HttpPost("gui-cho-nguoi")]
        public async Task<IActionResult> GuiThongBaoChoNguoi([FromBody] GuiThongBaoRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new { thongBao = "Title và Content là bắt buộc" });
                }

                if (request.MaNguoiThue <= 0)
                {
                    return BadRequest(new { thongBao = "MaNguoiThue không hợp lệ" });
                }

                var nguoiThue = await _context.NguoiThue.FindAsync(request.MaNguoiThue);
                if (nguoiThue == null)
                {
                    return NotFound(new { thongBao = "Không tìm thấy người thuê" });
                }

                // Chỉ lưu thông báo vào database (hiển thị trên web)
                var thongBao = new ThongBao
                {
                    Title = request.Title,
                    Content = request.Content,
                    CreatedAt = DateTime.Now,
                    ExpireAt = request.ExpireAt
                };
                _context.ThongBao.Add(thongBao);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    thongBaoId = thongBao.Id,
                    nguoiThue = new { maNguoiThue = nguoiThue.MaNguoiThue, hoTen = nguoiThue.HoTen },
                    thongBao = "Đã lưu thông báo thành công (chỉ hiển thị trên web)"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { thongBao = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Tạo thông báo cho nhiều người thuê (chỉ lưu vào DB, không gửi SMS/Email)
        /// </summary>
        [HttpPost("gui-cho-nhieu-nguoi")]
        public async Task<IActionResult> GuiThongBaoChoNhieuNguoi([FromBody] GuiThongBaoNhieuNguoiRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new { thongBao = "Title và Content là bắt buộc" });
                }

                if (request.DanhSachMaNguoiThue == null || !request.DanhSachMaNguoiThue.Any())
                {
                    return BadRequest(new { thongBao = "DanhSachMaNguoiThue không được để trống" });
                }

                // Chỉ lưu thông báo vào database (hiển thị trên web)
                var thongBao = new ThongBao
                {
                    Title = request.Title,
                    Content = request.Content,
                    CreatedAt = DateTime.Now,
                    ExpireAt = request.ExpireAt
                };
                _context.ThongBao.Add(thongBao);
                await _context.SaveChangesAsync();

                // Lấy danh sách người thuê để trả về
                var danhSachNguoiThue = await _context.NguoiThue
                    .Where(nt => request.DanhSachMaNguoiThue.Contains(nt.MaNguoiThue))
                    .Select(nt => new { maNguoiThue = nt.MaNguoiThue, hoTen = nt.HoTen })
                    .ToListAsync();

                return Ok(new
                {
                    thongBaoId = thongBao.Id,
                    tongSoNguoi = danhSachNguoiThue.Count,
                    danhSachNguoiThue = danhSachNguoiThue,
                    thongBao = "Đã lưu thông báo thành công (chỉ hiển thị trên web)"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { thongBao = $"Lỗi: {ex.Message}" });
            }
        }

        // Xóa thông báo
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tb = await _context.ThongBao.FindAsync(id);
            if (tb == null) return NotFound();
            _context.ThongBao.Remove(tb);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // Sửa thông báo
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ThongBao tb)
        {
            var thongBao = await _context.ThongBao.FindAsync(id);
            if (thongBao == null) return NotFound();
            thongBao.Title = tb.Title;
            thongBao.Content = tb.Content;
            thongBao.ExpireAt = tb.ExpireAt;
            await _context.SaveChangesAsync();
            return Ok(thongBao);
        }
    }

    /// <summary>
    /// Request model để tạo thông báo cho 1 người (chỉ lưu vào DB)
    /// </summary>
    public class GuiThongBaoRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "MaNguoiThue là bắt buộc")]
        public int MaNguoiThue { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Title là bắt buộc")]
        public string Title { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Content là bắt buộc")]
        public string Content { get; set; } = string.Empty;

        public DateTime? ExpireAt { get; set; }
    }

    /// <summary>
    /// Request model để tạo thông báo cho nhiều người (chỉ lưu vào DB)
    /// </summary>
    public class GuiThongBaoNhieuNguoiRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "DanhSachMaNguoiThue là bắt buộc")]
        public List<int> DanhSachMaNguoiThue { get; set; } = new();

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Title là bắt buộc")]
        public string Title { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Content là bắt buộc")]
        public string Content { get; set; } = string.Empty;

        public DateTime? ExpireAt { get; set; }
    }
} 