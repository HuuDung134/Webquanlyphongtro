using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace DoAnCoSo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GiaDienController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GiaDienController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/GiaDien
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GiaDien>>> GetGiaDien()
        {
            return await _context.GiaDien.OrderBy(g => g.BacDien).ToListAsync();
        }

        // GET: api/GiaDien/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GiaDien>> GetGiaDien(int id)
        {
            var giaDien = await _context.GiaDien.FindAsync(id);

            if (giaDien == null)
            {
                return NotFound();
            }

            return giaDien;
        }

        // POST: api/GiaDien
        [HttpPost]
        public async Task<ActionResult<GiaDien>> PostGiaDien(GiaDien giaDien)
        {
            // Kiểm tra bậc điện đã tồn tại
            if (await _context.GiaDien.AnyAsync(g => g.BacDien == giaDien.BacDien))
            {
                return BadRequest("Bậc điện này đã tồn tại");
            }
           
            // Kiểm tra khoảng số điện
            if (giaDien.TuSoDien >= giaDien.DenSoDien)
            {
                return BadRequest("Số điện bắt đầu phải nhỏ hơn số điện kết thúc");
            }

            // Kiểm tra khoảng số điện có bị chồng chéo
            var overlappingRange = await _context.GiaDien
                .AnyAsync(g => (giaDien.TuSoDien <= g.DenSoDien && giaDien.DenSoDien >= g.TuSoDien));
            if (overlappingRange)
            {
                return BadRequest("Khoảng số điện này đã bị chồng chéo với bậc điện khác");
            }

            _context.GiaDien.Add(giaDien);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGiaDien), new { id = giaDien.MaGiaDien }, giaDien);
        }

        // PUT: api/GiaDien/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGiaDien(int id, GiaDien giaDien)
        {
            if (id != giaDien.MaGiaDien)
            {
                return BadRequest();
            }

            // Kiểm tra bậc điện đã tồn tại (trừ bản ghi hiện tại)
            if (await _context.GiaDien.AnyAsync(g => g.BacDien == giaDien.BacDien && g.MaGiaDien != id))
            {
                return BadRequest("Bậc điện này đã tồn tại");
            }

            // Kiểm tra khoảng số điện
            if (giaDien.TuSoDien >= giaDien.DenSoDien)
            {
                return BadRequest("Số điện bắt đầu phải nhỏ hơn số điện kết thúc");
            }

            // Kiểm tra khoảng số điện có bị chồng chéo (trừ bản ghi hiện tại)
            var overlappingRange = await _context.GiaDien
                .AnyAsync(g => g.MaGiaDien != id && 
                    (giaDien.TuSoDien <= g.DenSoDien && giaDien.DenSoDien >= g.TuSoDien));
            if (overlappingRange)
            {
                return BadRequest("Khoảng số điện này đã bị chồng chéo với bậc điện khác");
            }

            _context.Entry(giaDien).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GiaDienExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/GiaDien/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGiaDien(int id)
        {
            var giaDien = await _context.GiaDien.FindAsync(id);
            if (giaDien == null)
            {
                return NotFound();
            }

            // Kiểm tra xem có đang được sử dụng trong ChiSoDien không
            var isInUse = await _context.ChiSoDien.AnyAsync(c => c.MaGiaDien == id);
            if (isInUse)
            {
                return BadRequest("Không thể xóa giá điện này vì đang được sử dụng trong chỉ số điện");
            }

            _context.GiaDien.Remove(giaDien);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GiaDienExists(int id)
        {
            return _context.GiaDien.Any(e => e.MaGiaDien == id);
        }
    }
} 