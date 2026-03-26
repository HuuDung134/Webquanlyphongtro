using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DoAnCoSo.Models;
using DoAnCoSo.Data;

namespace DoAnCoSo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TinNhanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TinNhanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------------------- Gửi tin nhắn ----------------------

        [HttpPost("admin-gui-cho-khach")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGuiChoKhach([FromBody] GuiTinNhanRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NoiDung))
                return BadRequest(new { thongBao = "Nội dung tin nhắn không được để trống" });

            if (request.MaNguoiNhan <= 0)
                return BadRequest(new { thongBao = "MaNguoiThue không hợp lệ" });

            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out int maNguoiDung))
                return Unauthorized(new { thongBao = "Không xác định được người gửi" });

            var nguoiThue = await _context.NguoiThue.FindAsync(request.MaNguoiNhan);
            if (nguoiThue == null)
                return NotFound(new { thongBao = "Không tìm thấy khách hàng" });

            var tinNhan = new TinNhan
            {
                MaNguoiGui = maNguoiDung,
                MaNguoiNhan = request.MaNguoiNhan,
                LoaiNguoiGui = "Admin",
                LoaiNguoiNhan = "NguoiThue",
                NoiDung = request.NoiDung,
                ThoiGianGui = DateTime.Now,
                DaDocAt = null
            };

            _context.TinNhan.Add(tinNhan);
            await _context.SaveChangesAsync();

            return Ok(new { id = tinNhan.Id, thongBao = "Gửi tin nhắn thành công", thoiGianGui = tinNhan.ThoiGianGui });
        }

        [HttpPost("khach-gui-cho-admin")]
        public async Task<IActionResult> KhachGuiChoAdmin([FromBody] GuiTinNhanKhachRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NoiDung))
                return BadRequest(new { thongBao = "Nội dung tin nhắn không được để trống" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int maNguoiDung))
                return Unauthorized(new { thongBao = "Không xác định được người gửi" });

            var nguoiThue = await _context.NguoiThue.FirstOrDefaultAsync(nt => nt.MaNguoiDung == maNguoiDung);
            if (nguoiThue == null)
                return NotFound(new { thongBao = "Không tìm thấy thông tin người thuê" });

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.VaiTro == "Admin");
            if (admin == null)
                return NotFound(new { thongBao = "Không tìm thấy Admin" });

            var tinNhan = new TinNhan
            {
                MaNguoiGui = nguoiThue.MaNguoiThue,
                MaNguoiNhan = admin.MaNguoiDung,
                LoaiNguoiGui = "NguoiThue",
                LoaiNguoiNhan = "Admin",
                NoiDung = request.NoiDung,
                ThoiGianGui = DateTime.Now,
                DaDocAt = null
            };

            _context.TinNhan.Add(tinNhan);
            await _context.SaveChangesAsync();

            return Ok(new { id = tinNhan.Id, thongBao = "Gửi tin nhắn thành công", thoiGianGui = tinNhan.ThoiGianGui });
        }

        // ---------------------- Lấy tin nhắn ----------------------

        [HttpGet("admin/danh-sach-hoi-thoai")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDanhSachHoiThoai()
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out int maNguoiDung))
                return Unauthorized(new { thongBao = "Không xác định được Admin" });

            // Lấy tất cả tin nhắn liên quan đến admin
            var allMessages = await _context.TinNhan
                .Where(tn => 
                    (tn.LoaiNguoiGui == "Admin" && tn.MaNguoiGui == maNguoiDung && tn.LoaiNguoiNhan == "NguoiThue") ||
                    (tn.LoaiNguoiNhan == "Admin" && tn.MaNguoiNhan == maNguoiDung && tn.LoaiNguoiGui == "NguoiThue"))
                .ToListAsync();

            // Nhóm theo maNguoiThue
            var grouped = allMessages
                .GroupBy(tn => tn.LoaiNguoiGui == "Admin" ? tn.MaNguoiNhan : tn.MaNguoiGui)
                .ToList();

            var ketQua = new List<object>();
            foreach (var group in grouped)
            {
                int maNguoiThue = group.Key;
                var messages = group.OrderByDescending(tn => tn.ThoiGianGui).ToList();
                var tinNhanCuoi = messages.FirstOrDefault();
                
                if (tinNhanCuoi == null) continue;

                var nguoiThue = await _context.NguoiThue
                    .Include(nt => nt.User)
                    .FirstOrDefaultAsync(nt => nt.MaNguoiThue == maNguoiThue);

                if (nguoiThue != null)
                {
                    var soLuongChuaDoc = messages.Count(tn => tn.LoaiNguoiGui == "NguoiThue" && tn.DaDocAt == null);
                    
                    ketQua.Add(new
                    {
                        maNguoiThue = nguoiThue.MaNguoiThue,
                        tenKhachHang = nguoiThue.User?.TenDangNhap ?? nguoiThue.HoTen ?? "Khách hàng",
                        hoTen = nguoiThue.HoTen ?? nguoiThue.User?.TenDangNhap ?? "Khách hàng",
                        soDienThoai = nguoiThue.SDT ?? "",
                        tinNhanCuoi = new
                        {
                            noiDung = tinNhanCuoi.NoiDung,
                            thoiGianGui = tinNhanCuoi.ThoiGianGui,
                            laAdminGui = tinNhanCuoi.LoaiNguoiGui == "Admin"
                        },
                        soLuongChuaDoc = soLuongChuaDoc
                    });
                }
            }

            // Sắp xếp theo thời gian tin nhắn cuối (mới nhất trước)
            ketQua = ketQua.OrderByDescending(k => 
            {
                var tinNhanCuoi = ((dynamic)k).tinNhanCuoi;
                return tinNhanCuoi?.thoiGianGui ?? DateTime.MinValue;
            }).ToList();

            return Ok(new { danhSachHoiThoai = ketQua });
        }

        [HttpGet("admin/khach-hang/{maNguoiThue}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTinNhanVoiKhach(int maNguoiThue, [FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out int maNguoiDung))
                return Unauthorized(new { thongBao = "Không xác định được Admin" });

            var admin = await _context.Users.FindAsync(maNguoiDung);
            var nguoiThue = await _context.NguoiThue.FindAsync(maNguoiThue);
            
            var tinNhans = await _context.TinNhan
                .Where(tn =>
                    (tn.MaNguoiGui == maNguoiDung && tn.LoaiNguoiGui == "Admin" &&
                     tn.MaNguoiNhan == maNguoiThue && tn.LoaiNguoiNhan == "NguoiThue") ||
                    (tn.MaNguoiGui == maNguoiThue && tn.LoaiNguoiGui == "NguoiThue" &&
                     tn.MaNguoiNhan == maNguoiDung && tn.LoaiNguoiNhan == "Admin"))
                .OrderBy(tn => tn.ThoiGianGui)
                .Skip(skip)
                .Take(take)
                .Select(tn => new
                {
                    tn.Id,
                    tn.NoiDung,
                    tn.ThoiGianGui,
                    tn.DaDocAt,
                    tn.DaThuHoi,
                    tn.DaSua,
                    tn.NoiDungGoc,
                    tn.LoaiNguoiGui,
                    tn.MaNguoiGui,
                    laAdminGui = tn.LoaiNguoiGui == "Admin" && tn.MaNguoiGui == maNguoiDung
                })
                .ToListAsync();

            // Thêm tên người gửi vào mỗi tin nhắn
            var result = tinNhans.Select(tn => new
            {
                tn.Id,
                tn.NoiDung,
                tn.ThoiGianGui,
                tn.DaDocAt,
                tn.DaThuHoi,
                tn.DaSua,
                tn.NoiDungGoc,
                tn.laAdminGui,
                tenNguoiGui = tn.LoaiNguoiGui == "Admin"
                    ? (admin != null ? admin.TenDangNhap : "Admin")
                    : (nguoiThue != null ? nguoiThue.HoTen : "Khách hàng")
            }).ToList();

            return Ok(new { danhSachTinNhan = result, tongSo = result.Count });
        }

        [HttpGet("khach/voi-admin")]
        public async Task<IActionResult> GetTinNhanVoiAdmin([FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int maNguoiDung))
                return Unauthorized(new { thongBao = "Không xác định được người dùng" });

            var nguoiThue = await _context.NguoiThue.FirstOrDefaultAsync(nt => nt.MaNguoiDung == maNguoiDung);
            if (nguoiThue == null)
                return NotFound(new { thongBao = "Không tìm thấy thông tin người thuê" });

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.VaiTro == "Admin");
            if (admin == null)
                return NotFound(new { thongBao = "Không tìm thấy Admin" });

            var tinNhans = await _context.TinNhan
                .Where(tn =>
                    (tn.MaNguoiGui == nguoiThue.MaNguoiThue && tn.LoaiNguoiGui == "NguoiThue" &&
                     tn.MaNguoiNhan == admin.MaNguoiDung && tn.LoaiNguoiNhan == "Admin") ||
                    (tn.MaNguoiGui == admin.MaNguoiDung && tn.LoaiNguoiGui == "Admin" &&
                     tn.MaNguoiNhan == nguoiThue.MaNguoiThue && tn.LoaiNguoiNhan == "NguoiThue"))
                .OrderBy(tn => tn.ThoiGianGui)
                .Skip(skip)
                .Take(take)
                .Select(tn => new
                {
                    tn.Id,
                    tn.NoiDung,
                    tn.ThoiGianGui,
                    tn.DaDocAt,
                    tn.DaThuHoi,
                    tn.DaSua,
                    tn.NoiDungGoc,
                    tn.LoaiNguoiGui,
                    tn.MaNguoiGui,
                    laKhachGui = tn.LoaiNguoiGui == "NguoiThue" && tn.MaNguoiGui == nguoiThue.MaNguoiThue
                })
                .ToListAsync();

            // Thêm tên người gửi vào mỗi tin nhắn
            var result = tinNhans.Select(tn => new
            {
                tn.Id,
                tn.NoiDung,
                tn.ThoiGianGui,
                tn.DaDocAt,
                tn.DaThuHoi,
                tn.DaSua,
                tn.NoiDungGoc,
                tn.laKhachGui,
                tenNguoiGui = tn.LoaiNguoiGui == "Admin"
                    ? (admin != null ? admin.TenDangNhap : "Admin")
                    : (nguoiThue != null ? nguoiThue.HoTen : "Khách hàng")
            }).ToList();

            return Ok(new { danhSachTinNhan = result, tongSo = result.Count });
        }

        // ---------------------- Đánh dấu đã đọc ----------------------

        [HttpPost("admin/khach-hang/{maNguoiThue}/da-doc-tat-ca")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DaDocTatCaAdmin(int maNguoiThue)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out int maNguoiDung))
                return Unauthorized(new { thongBao = "Không xác định được Admin" });

            var tinNhans = await _context.TinNhan
                .Where(tn =>
                    tn.MaNguoiGui == maNguoiThue &&
                    tn.LoaiNguoiGui == "NguoiThue" &&
                    tn.MaNguoiNhan == maNguoiDung &&
                    tn.LoaiNguoiNhan == "Admin" &&
                    tn.DaDocAt == null)
                .ToListAsync();

            foreach (var tinNhan in tinNhans)
            {
                tinNhan.DaDocAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(new { thongBao = "Đã đánh dấu đã đọc" });
        }

        [HttpPost("khach/da-doc-tat-ca")]
        public async Task<IActionResult> DaDocTatCaKhach()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int maNguoiDung))
                return Unauthorized(new { thongBao = "Không xác định được người dùng" });

            var nguoiThue = await _context.NguoiThue.FirstOrDefaultAsync(nt => nt.MaNguoiDung == maNguoiDung);
            if (nguoiThue == null)
                return NotFound(new { thongBao = "Không tìm thấy thông tin người thuê" });

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.VaiTro == "Admin");
            if (admin == null)
                return NotFound(new { thongBao = "Không tìm thấy Admin" });

            var tinNhans = await _context.TinNhan
                .Where(tn =>
                    tn.MaNguoiGui == admin.MaNguoiDung &&
                    tn.LoaiNguoiGui == "Admin" &&
                    tn.MaNguoiNhan == nguoiThue.MaNguoiThue &&
                    tn.LoaiNguoiNhan == "NguoiThue" &&
                    tn.DaDocAt == null)
                .ToListAsync();

            foreach (var tinNhan in tinNhans)
            {
                tinNhan.DaDocAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(new { thongBao = "Đã đánh dấu đã đọc" });
        }

        // ---------------------- Thu hồi tin nhắn ----------------------

        [HttpDelete("thu-hoi/{id}")]
        public async Task<IActionResult> ThuHoiTinNhan(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int maNguoiDung))
                return Unauthorized(new { thongBao = "Không xác định được người dùng" });

            var tinNhan = await _context.TinNhan.FindAsync(id);
            if (tinNhan == null)
                return NotFound(new { thongBao = "Không tìm thấy tin nhắn" });

            bool isSender = (tinNhan.LoaiNguoiGui == "Admin" && tinNhan.MaNguoiGui == maNguoiDung) ||
                            (tinNhan.LoaiNguoiGui == "NguoiThue" && tinNhan.MaNguoiGui == (await _context.NguoiThue.FirstOrDefaultAsync(nt => nt.MaNguoiDung == maNguoiDung))?.MaNguoiThue);

            if (!isSender)
                return Forbid("Bạn không có quyền thu hồi tin nhắn này");

            // Bỏ kiểm tra thời gian 5 phút - cho phép xóa bất cứ lúc nào
            _context.TinNhan.Remove(tinNhan);
            await _context.SaveChangesAsync();

            return Ok(new { thongBao = "Đã xóa tin nhắn thành công" });
        }

        // ---------------------- Sửa tin nhắn ----------------------

        [HttpPut("sua/{id}")]
        public async Task<IActionResult> SuaTinNhan(int id, [FromBody] SuaTinNhanRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NoiDung))
                return BadRequest(new { thongBao = "Nội dung không được để trống" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int maNguoiDung))
                return Unauthorized(new { thongBao = "Không xác định được người dùng" });

            var tinNhan = await _context.TinNhan.FindAsync(id);
            if (tinNhan == null)
                return NotFound(new { thongBao = "Không tìm thấy tin nhắn" });

            bool isSender = (tinNhan.LoaiNguoiGui == "Admin" && tinNhan.MaNguoiGui == maNguoiDung) ||
                            (tinNhan.LoaiNguoiGui == "NguoiThue" && tinNhan.MaNguoiGui == (await _context.NguoiThue.FirstOrDefaultAsync(nt => nt.MaNguoiDung == maNguoiDung))?.MaNguoiThue);

            if (!isSender)
                return Forbid("Bạn không có quyền sửa tin nhắn này");

            if ((DateTime.Now - tinNhan.ThoiGianGui).TotalMinutes > 5)
                return BadRequest(new { thongBao = "Chỉ có thể sửa tin nhắn trong vòng 5 phút sau khi gửi" });

            if (tinNhan.DaThuHoi)
                return BadRequest(new { thongBao = "Không thể sửa tin nhắn đã thu hồi" });

            if (!tinNhan.DaSua && string.IsNullOrEmpty(tinNhan.NoiDungGoc))
                tinNhan.NoiDungGoc = tinNhan.NoiDung;

            tinNhan.NoiDung = request.NoiDung;
            tinNhan.DaSua = true;

            await _context.SaveChangesAsync();
            return Ok(new { thongBao = "Đã sửa tin nhắn thành công" });
        }
    }

    public class SuaTinNhanRequest
    {
        public string NoiDung { get; set; } = string.Empty;
    }

    public class GuiTinNhanRequest
    {
        public int MaNguoiNhan { get; set; }
        public string NoiDung { get; set; } = string.Empty;
    }

    public class GuiTinNhanKhachRequest
    {
        public string NoiDung { get; set; } = string.Empty;
    }
}
