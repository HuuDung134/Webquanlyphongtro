using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnCoSo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GiaNuocController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GiaNuocController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/GiaNuoc
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GiaNuoc>>> GetGiaNuoc()
        {
            return await _context.GiaNuoc.ToListAsync();
        }

        // GET: api/GiaNuoc/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GiaNuoc>> GetGiaNuoc(int id)
        {
            var giaNuoc = await _context.GiaNuoc.FindAsync(id);

            if (giaNuoc == null)
            {
                return NotFound();
            }

            return giaNuoc;
        }

        // POST: api/GiaNuoc
        [HttpPost]
        public async Task<ActionResult<GiaNuoc>> PostGiaNuoc(GiaNuoc giaNuoc)
        {
            _context.GiaNuoc.Add(giaNuoc);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGiaNuoc), new { id = giaNuoc.MaGiaNuoc }, giaNuoc);
        }

        // PUT: api/GiaNuoc/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGiaNuoc(int id, GiaNuoc giaNuoc)
        {
            if (id != giaNuoc.MaGiaNuoc)
            {
                return BadRequest();
            }

            _context.Entry(giaNuoc).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GiaNuocExists(id))
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

        // DELETE: api/GiaNuoc/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGiaNuoc(int id)
        {
            var giaNuoc = await _context.GiaNuoc.FindAsync(id);
            if (giaNuoc == null)
            {
                return NotFound();
            }

            _context.GiaNuoc.Remove(giaNuoc);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GiaNuocExists(int id)
        {
            return _context.GiaNuoc.Any(e => e.MaGiaNuoc == id);
        }
    }
} 