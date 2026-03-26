using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using System.Text.Json;

namespace DoAnCoSo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NguoiThueController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JsonSerializerOptions _jsonOptions;

        public NguoiThueController(ApplicationDbContext context)
        {
            _context = context;
            _jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                WriteIndented = true
            };
        }

        // GET: api/NguoiThue
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NguoiThue>>> GetNguoiThue()
        {
            try
            {
                var nguoiThue = await _context.NguoiThue
                    .Include(n => n.User)
                    .Select(n => new
                    {
                        n.MaNguoiThue,
                        n.HoTen,
                        n.SDT,
                        n.Email,
                        n.CCCD,
                        n.DiaChi,
                        n.NgaySinh,
                        n.GioiTinh,
                        n.QuocTich,
                        n.NoiCongTac,
                        n.MaNguoiDung,
                        TaiKhoan = n.User != null ? new
                        {
                            n.User.MaNguoiDung,
                            n.User.TenDangNhap
                        } : null
                    })
                    .ToListAsync();

                return Ok(nguoiThue);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // GET: api/NguoiThue/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NguoiThue>> GetNguoiThue(int id)
        {
            try
            {
                var nguoiThue = await _context.NguoiThue
                    .Include(n => n.User)
                    .Where(n => n.MaNguoiThue == id)
                    .Select(n => new
                    {
                        n.MaNguoiThue,
                        n.HoTen,
                        n.SDT,
                        n.Email,
                        n.CCCD,
                        n.DiaChi,
                        n.NgaySinh,
                        n.GioiTinh,
                        n.QuocTich,
                        n.NoiCongTac,
                        n.MaNguoiDung,
                        TaiKhoan = n.User != null ? new
                        {
                            n.User.MaNguoiDung,
                            n.User.TenDangNhap
                        } : null
                    })
                    .FirstOrDefaultAsync();

                if (nguoiThue == null)
                {
                    return NotFound("Không tìm thấy người thuê");
                }

                return Ok(nguoiThue);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // GET: api/NguoiThue/Search?keyword=keyword
        [HttpGet("Search")]
        public async Task<ActionResult<IEnumerable<NguoiThue>>> SearchNguoiThue(string keyword)
        {
            try
            {
                if (string.IsNullOrEmpty(keyword))
                {
                    return await GetNguoiThue();
                }

                var nguoiThue = await _context.NguoiThue
                    .Include(n => n.User)
                    .Where(n => n.HoTen.Contains(keyword) || 
                               n.CCCD.Contains(keyword) || 
                               n.SDT.Contains(keyword) ||
                               n.Email.Contains(keyword))
                    .Select(n => new
                    {
                        n.MaNguoiThue,
                        n.HoTen,
                        n.SDT,
                        n.Email,
                        n.CCCD,
                        n.DiaChi,
                        n.NgaySinh,
                        n.GioiTinh,
                        n.QuocTich,
                        n.NoiCongTac,
                        n.MaNguoiDung,
                        TaiKhoan = n.User != null ? new
                        {
                            n.User.MaNguoiDung,
                            n.User.TenDangNhap

                        } : null
                    })
                    .ToListAsync();

                return Ok(nguoiThue);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // POST: api/NguoiThue
        [HttpPost]
        public async Task<ActionResult<NguoiThue>> PostNguoiThue(NguoiThue nguoiThue)
        {
            try
            {
                if (string.IsNullOrEmpty(nguoiThue.HoTen))
                {
                    return BadRequest("Họ tên không được để trống");
                }

                if (nguoiThue.MaNguoiDung.HasValue)
                {
                    var userExists = await _context.Users.AnyAsync(u => u.MaNguoiDung == nguoiThue.MaNguoiDung);
                    if (!userExists)
                    {
                        return BadRequest("Tài khoản không tồn tại");
                    }
                }
                if (_context.NguoiThue.Any(nt => nt.CCCD == nguoiThue.CCCD))
                {
                    return BadRequest("CCCD đã tồn tại.");
                }

                if (_context.NguoiThue.Any(nt => nt.SDT == nguoiThue.SDT))
                {
                    return BadRequest("SĐT đã tồn tại.");
                }
                if (!IsDuTren18(nguoiThue.NgaySinh))
                {
                    return BadRequest("Người thuê phải từ 18 tuổi trở lên.");
                }
                if (_context.NguoiThue.Any(nt => nt.Email == nguoiThue.Email))
                {
                    return BadRequest("Email đã tồn tại.");
                }

                _context.NguoiThue.Add(nguoiThue);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetNguoiThue), new { id = nguoiThue.MaNguoiThue }, nguoiThue);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // PUT: api/NguoiThue/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutNguoiThue(int id, NguoiThue nguoiThue)
        {
            try
            {
                if (id != nguoiThue.MaNguoiThue)
                {
                    return BadRequest("ID không khớp");
                }

                var existingNguoiThue = await _context.NguoiThue
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.MaNguoiThue == id);

                if (existingNguoiThue == null)
                {
                    return NotFound("Không tìm thấy người thuê");
                }

                if (nguoiThue.MaNguoiDung.HasValue)
                {
                    var userExists = await _context.Users.AnyAsync(u => u.MaNguoiDung == nguoiThue.MaNguoiDung);
                    if (!userExists)
                    {
                        return BadRequest("Tài khoản không tồn tại");
                    }
                }

                _context.Entry(nguoiThue).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NguoiThueExists(id))
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
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // DELETE: api/NguoiThue/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNguoiThue(int id)
        {
            try
            {
                var nguoiThue = await _context.NguoiThue.FindAsync(id);
                if (nguoiThue == null)
                {
                    return NotFound("Không tìm thấy người thuê");
                }

                _context.NguoiThue.Remove(nguoiThue);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }
        private bool IsDuTren18(DateTime? ngaySinh)
        {
            if (!ngaySinh.HasValue) return false;

            var tuoi = DateTime.Now.Year - ngaySinh.Value.Year;
            if (ngaySinh > DateTime.Now.AddYears(-tuoi)) tuoi--;
            return tuoi >= 18;
        }


        private bool NguoiThueExists(int id)
        {
            return _context.NguoiThue.Any(e => e.MaNguoiThue == id);
        }
    }
} 