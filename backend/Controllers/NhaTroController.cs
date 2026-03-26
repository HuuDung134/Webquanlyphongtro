using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Data;
using DoAnCoSo.Models;

namespace DoAnCoSo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NhaTroController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NhaTroController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/NhaTro
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NhaTro>>> GetNhaTro()
        {
            return await _context.NhaTro.ToListAsync();
        }

        // GET: api/NhaTro/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NhaTro>> GetNhaTro(int id)
        {
            var nhaTro = await _context.NhaTro.FindAsync(id);

            if (nhaTro == null)
            {
                return NotFound();
            }

            return nhaTro;
        }

        // POST: api/NhaTro
        [HttpPost]
        public async Task<ActionResult<NhaTro>> PostNhaTro(NhaTro nhaTro)
        {
            _context.NhaTro.Add(nhaTro);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNhaTro), new { id = nhaTro.MaNhaTro }, nhaTro);
        }

        // PUT: api/NhaTro/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutNhaTro(int id, NhaTro nhaTro)
        {
            if (id != nhaTro.MaNhaTro)
            {
                return BadRequest();
            }

            _context.Entry(nhaTro).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NhaTroExists(id))
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

        // DELETE: api/NhaTro/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNhaTro(int id)
        {
            var nhaTro = await _context.NhaTro.FindAsync(id);
            if (nhaTro == null)
            {
                return NotFound();
            }

            _context.NhaTro.Remove(nhaTro);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool NhaTroExists(int id)
        {
            return _context.NhaTro.Any(e => e.MaNhaTro == id);
        }
    }
} 