using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Models;
using DoAnCoSo.Data;

namespace DoAnCoSo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChiTietHopDongController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChiTietHopDongController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ChiTietHopDong
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChiTietHopDong>>> GetChiTietHopDongs()
        {
            return await _context.ChiTietHopDong
                .Include(c => c.HopDong)
                .Include(c => c.NguoiThue)
                .ToListAsync();
        }

        // GET: api/ChiTietHopDong/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ChiTietHopDong>> GetChiTietHopDong(int id)
        {
            var chiTietHopDong = await _context.ChiTietHopDong
                .Include(c => c.HopDong)
                .Include(c => c.NguoiThue)
                .FirstOrDefaultAsync(c => c.MaChiTietHopDong == id);

            if (chiTietHopDong == null)
            {
                return NotFound();
            }

            return chiTietHopDong;
        }

        // POST: api/ChiTietHopDong
        [HttpPost]
        public async Task<ActionResult<ChiTietHopDong>> PostChiTietHopDong(ChiTietHopDong chiTietHopDong)
        {
            // Kiểm tra xem hợp đồng và người thuê có tồn tại không
            var hopDong = await _context.HopDong.FindAsync(chiTietHopDong.MaHopDong);
            var nguoiThue = await _context.NguoiThue.FindAsync(chiTietHopDong.MaNguoiThue);

            if (hopDong == null || nguoiThue == null)
            {
                return BadRequest("Hợp đồng hoặc người thuê không tồn tại");
            }

            _context.ChiTietHopDong.Add(chiTietHopDong);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetChiTietHopDong), new { id = chiTietHopDong.MaChiTietHopDong }, chiTietHopDong);
        }

        // DELETE: api/ChiTietHopDong/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChiTietHopDong(int id)
        {
            var chiTietHopDong = await _context.ChiTietHopDong.FindAsync(id);
            if (chiTietHopDong == null)
            {
                return NotFound();
            }

            _context.ChiTietHopDong.Remove(chiTietHopDong);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ChiTietHopDongExists(int id)
        {
            return _context.ChiTietHopDong.Any(e => e.MaChiTietHopDong == id);
        }
    }
} 