using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using DoAnCoSo.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using DoAnCoSo.Services;
using System.IO;
using System.Globalization;
using DinkToPdf;
using DinkToPdf.Contracts;

namespace DoAnCoSo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HoaDonController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IConverter _converter;

        public HoaDonController(ApplicationDbContext context, EmailService emailService, IConverter converter)
        {
            _context = context;
            _emailService = emailService;
            _converter = converter;
        }

        


        [HttpGet("GetThongTinPhong/{phongId}")]
        public async Task<IActionResult> GetThongTinPhong(int phongId)
        {
            var phong = await _context.Phong.FindAsync(phongId);
            if (phong == null)
                return NotFound("Phòng không tồn tại");

            var hopDong = await _context.HopDong
                .Where(hd => hd.MaPhong == phongId
                      && (hd.NgayKetThuc == null || hd.NgayKetThuc > DateTime.Now))
                .OrderByDescending(hd => hd.NgayBatDau)
                .FirstOrDefaultAsync();
            if (hopDong == null)
                return BadRequest("Phòng chưa có hợp đồng thuê hợp lệ");

            var nguoiThue = await _context.NguoiThue.FindAsync(hopDong.MaNguoiThue);
            if (nguoiThue == null)
                return BadRequest("Không tìm thấy người thuê");

            var chiSoDien = await _context.ChiSoDien
                .Where(cd => cd.MaPhong == phongId)
                .OrderByDescending(cd => cd.NgayThangDien)
                .FirstOrDefaultAsync();
            if (chiSoDien == null)
                return BadRequest("Chưa có chỉ số điện cho phòng này");

            var chiSoNuoc = await _context.ChiSoNuoc
                .Where(cn => cn.MaPhong == phongId)
                .OrderByDescending(cn => cn.NgayThangNuoc)
                .FirstOrDefaultAsync();
            if (chiSoNuoc == null)
                return BadRequest("Chưa có chỉ số nước cho phòng này");

            // Lấy danh sách dịch vụ
            var danhSachDichVu = await _context.DichVu.ToListAsync();
            var tongTienDichVu = danhSachDichVu.Sum(dv => (decimal)dv.Tiendichvu);

            decimal tongTienHoaDon = phong.GiaPhong + chiSoDien.TienDien + chiSoNuoc.TienNuoc + tongTienDichVu;

            var hoaDon = await _context.HoaDon
                .Where(hd => hd.MaPhong == phongId)
                .OrderByDescending(hd => hd.NgayLap)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                Phong = new { phong.MaPhong, phong.TenPhong, GiaPhong = phong.GiaPhong },
                NguoiThue = new { nguoiThue.MaNguoiThue, nguoiThue.HoTen },
                TienDien = chiSoDien.TienDien,
                TienNuoc = chiSoNuoc.TienNuoc,
                DanhSachDichVu = danhSachDichVu.Select(dv => new
                {
                    dv.MaDichVu,
                    dv.TenDichVu,
                    GiaDichVu = (decimal)dv.Tiendichvu
                }),
                TongTienDichVu = tongTienDichVu,
                TongTienHoaDon = tongTienHoaDon,
                MaDien = chiSoDien.MaDien,
                MaNuoc = chiSoNuoc.MaNuoc,
                MaHoaDon = hoaDon?.MaHoaDon,
                KyHoaDon = hoaDon?.KyHoaDon
            });
        }


        // Lấy danh sách phòng chưa có hóa đơn trong tháng/năm cụ thể
        [HttpGet("GetPhongChuaCoHoaDonTrongThang")]
        public async Task<IActionResult> GetPhongChuaCoHoaDonTrongThang([FromQuery] int thang, [FromQuery] int nam)
        {
            var phongDaCoHoaDon = await _context.HoaDon
                .Where(hd => hd.NgayLap.Month == thang && hd.NgayLap.Year == nam)
                .Select(hd => hd.MaPhong)
                .ToListAsync();

            var phongChuaCoHoaDon = await _context.Phong
                .Where(p => !phongDaCoHoaDon.Contains(p.MaPhong))
                .ToListAsync();

            return Ok(phongChuaCoHoaDon);
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HoaDonDto>>> GetAllHoaDon([FromQuery] int? maNguoiThue)
        {
            var query = _context.HoaDon
                .Include(h => h.Phong)
                .Include(h => h.NguoiThue)
                .Include(h => h.ChiTietHoaDon)
                    .ThenInclude(ct => ct.DichVu)
                .AsQueryable();

            if (maNguoiThue.HasValue)
                query = query.Where(h => h.MaNguoiThue == maNguoiThue.Value);

            // Load tất cả dữ liệu vào memory trước khi xử lý
            // Load tất cả dữ liệu vào memory trước
            var hoaDons = await query.ToListAsync();

            // Lấy danh sách mã hóa đơn để query thanh toán một lần
            var maHoaDonList = hoaDons.Select(h => h.MaHoaDon).ToList();
            var thanhToans = await _context.ThanhToan
                .Where(t => maHoaDonList.Contains(t.MaHoaDon) && t.TrangThai == 1)
                .GroupBy(t => t.MaHoaDon)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(t => t.TongTien));

            // Xử lý trong memory (sau khi đã load dữ liệu)
            var result = hoaDons.Select(h =>
            {
                // Xử lý ChiTietHoaDon trong memory
                var chiTietList = h.ChiTietHoaDon?.ToList() ?? new List<ChiTietHoaDon>();
                
                var tienPhong = chiTietList
                    .Where(ct => ct.LoaiKhoan == "Phong" || ct.LoaiKhoan == "Tiền phòng" || 
                                 (ct.LoaiKhoan != null && ct.LoaiKhoan.ToLower().Contains("phòng")))
                    .Sum(ct => (decimal?)ct.SoTien) ?? 0;
                
                var tienDien = chiTietList
                    .Where(ct => ct.LoaiKhoan == "Dien" || ct.LoaiKhoan == "Tiền điện" || 
                                 (ct.LoaiKhoan != null && ct.LoaiKhoan.ToLower().Contains("điện")))
                    .Sum(ct => (decimal?)ct.SoTien) ?? 0;
                
                var tienNuoc = chiTietList
                    .Where(ct => ct.LoaiKhoan == "Nuoc" || ct.LoaiKhoan == "Tiền nước" || 
                                 (ct.LoaiKhoan != null && ct.LoaiKhoan.ToLower().Contains("nước")))
                    .Sum(ct => (decimal?)ct.SoTien) ?? 0;
                
                var tienDichVu = chiTietList
                    .Where(ct => ct.LoaiKhoan == "DichVu" || ct.LoaiKhoan == "Tiền dịch vụ" || 
                                 (ct.LoaiKhoan != null && ct.LoaiKhoan.ToLower().Contains("dịch vụ")))
                    .Sum(ct => (decimal?)ct.SoTien) ?? 0;

                return new HoaDonDto
                {
                    MaHoaDon = h.MaHoaDon,
                    MaPhong = h.MaPhong,
                    MaNguoiThue = h.MaNguoiThue,
                    TenPhong = h.Phong?.TenPhong ?? "",
                    TenNguoiThue = h.NguoiThue?.HoTen ?? "",
                    NgayLap = h.NgayLap,
                    KyHoaDon = h.KyHoaDon,
                    TienPhong = tienPhong,
                    TienDien = tienDien,
                    TienNuoc = tienNuoc,
                    TienDichVu = tienDichVu,
                    TongTien = h.TongTien,
                    ChiTietDichVu = chiTietList
                        .Where(ct => ct.LoaiKhoan == "DichVu" || ct.LoaiKhoan == "Tiền dịch vụ")
                        .Select(ct => new ChiTietDichVuDto
                        {
                            TenDichVu = ct.DichVu != null ? ct.DichVu.TenDichVu : "",
                            SoLuong = ct.SoLuong ?? 1,
                            DonGia = ct.DonGia ?? 0,
                            ThanhTien = ct.SoTien
                        }).ToList(),
                    TrangThai = thanhToans.ContainsKey(h.MaHoaDon) && thanhToans[h.MaHoaDon] >= h.TongTien ? 2 : 1
                };
            }).ToList();

            return Ok(result);
        }

        // Tạo hóa đơn mới
        [HttpPost]
        public async Task<IActionResult> CreateHoaDon([FromBody] HoaDonCreateDto request)
        {
            try
            {
                // Log request data
                Console.WriteLine($"Received request: {JsonSerializer.Serialize(request)}");

                // Kiểm tra dữ liệu đầu vào
                if (request == null)
                {
                    Console.WriteLine("Request is null");
                    return BadRequest("Dữ liệu không hợp lệ");
                }

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    Console.WriteLine($"Model validation errors: {errors}");
                    return BadRequest($"Dữ liệu không hợp lệ: {errors}");
                }

                // Kiểm tra các trường bắt buộc
                if (request.MaPhong <= 0)
                {
                    Console.WriteLine($"Invalid MaPhong: {request.MaPhong}");
                    return BadRequest("Mã phòng không hợp lệ");
                }
                if (request.MaNguoiThue <= 0)
                {
                    Console.WriteLine($"Invalid MaNguoiThue: {request.MaNguoiThue}");
                    return BadRequest("Mã người thuê không hợp lệ");
                }
                if (string.IsNullOrEmpty(request.KyHoaDon))
                {
                    Console.WriteLine("KyHoaDon is empty");
                    return BadRequest("Kỳ hóa đơn không được để trống");
                }
                if (string.IsNullOrEmpty(request.NgayLap.ToString()))
                {
                    Console.WriteLine("NgayLap is empty");
                    return BadRequest("Ngày lập không được để trống");
                }

                // Kiểm tra phòng
                var phong = await _context.Phong
                   
                    .FirstOrDefaultAsync(p => p.MaPhong == request.MaPhong);
                    
                if (phong == null)
                {
                    Console.WriteLine($"Phong not found: {request.MaPhong}");
                    return BadRequest("Phòng không tồn tại");
                }

                // Kiểm tra người thuê
                var nguoiThue = await _context.NguoiThue
                    .FirstOrDefaultAsync(nt => nt.MaNguoiThue == request.MaNguoiThue);
                    
                if (nguoiThue == null)
                {
                    Console.WriteLine($"NguoiThue not found: {request.MaNguoiThue}");
                    return BadRequest("Người thuê không tồn tại");
                }

                // Kiểm tra hợp đồng
                var hopDong = await _context.HopDong
                    .FirstOrDefaultAsync(hd => hd.MaPhong == request.MaPhong
                           && hd.MaNguoiThue == request.MaNguoiThue
                           && (hd.NgayKetThuc == null || hd.NgayKetThuc > DateTime.Now));

                if (hopDong == null)
                {
                    Console.WriteLine($"HopDong not found for Phong: {request.MaPhong}, NguoiThue: {request.MaNguoiThue}");
                    return BadRequest("Không tìm thấy hợp đồng thuê hợp lệ cho phòng này");
                }

                // Kiểm tra hóa đơn trùng
                var hoaDonTonTai = await _context.HoaDon
                    .AnyAsync(hd => hd.MaPhong == request.MaPhong
                              && hd.KyHoaDon == request.KyHoaDon);

                if (hoaDonTonTai)
                {
                    Console.WriteLine($"HoaDon already exists for Phong: {request.MaPhong}, KyHoaDon: {request.KyHoaDon}");
                    return BadRequest($"Đã tồn tại hóa đơn cho phòng này trong kỳ {request.KyHoaDon}");
                }

                // Kiểm tra chỉ số điện
                var chiSoDien = await _context.ChiSoDien
                    .Where(cd => cd.MaPhong == request.MaPhong)
                    .OrderByDescending(cd => cd.NgayThangDien)
                    .FirstOrDefaultAsync();

                if (chiSoDien == null)
                {
                    Console.WriteLine($"ChiSoDien not found for Phong: {request.MaPhong}");
                    return BadRequest("Chưa có chỉ số điện cho phòng này");
                }

                // Kiểm tra chỉ số nước
                var chiSoNuoc = await _context.ChiSoNuoc
                    .Where(cn => cn.MaPhong == request.MaPhong)
                    .OrderByDescending(cn => cn.NgayThangNuoc)
                    .FirstOrDefaultAsync();

                if (chiSoNuoc == null)
                {
                    Console.WriteLine($"ChiSoNuoc not found for Phong: {request.MaPhong}");
                    return BadRequest("Chưa có chỉ số nước cho phòng này");
                }

                // Lấy danh sách dịch vụ
                var danhSachDichVu = await _context.DichVu.ToListAsync();
                var tongTienDichVu = danhSachDichVu.Sum(dv => (decimal)dv.Tiendichvu);

                // Tính tổng tiền
                decimal tongTien = request.TienPhong + request.TienDien + request.TienNuoc + tongTienDichVu;

                Console.WriteLine($"Calculated total: {tongTien} (Phong: {request.TienPhong}, Dien: {request.TienDien}, Nuoc: {request.TienNuoc}, DichVu: {tongTienDichVu})");

                // Tạo hóa đơn mới
                var hoaDon = new HoaDon
                {
                    MaPhong = request.MaPhong,
                    MaNguoiThue = request.MaNguoiThue,
                    MaDien = chiSoDien.MaDien,
                    MaNuoc = chiSoNuoc.MaNuoc,
                    NgayLap = DateTime.Parse(request.NgayLap.ToString()),
                    KyHoaDon = request.KyHoaDon,
                    TongTien = tongTien,
                    TienDichVu = tongTienDichVu
                };

                try
                {
                    // Thêm hóa đơn vào context
                    _context.HoaDon.Add(hoaDon);
                    await _context.SaveChangesAsync();

                    // Thêm chi tiết hóa đơn
                    var chiTietHoaDon = new List<ChiTietHoaDon>
                    {
                        new ChiTietHoaDon
                        {
                            MaHoaDon = hoaDon.MaHoaDon,
                            LoaiKhoan = "Phong",
                            SoTien = request.TienPhong
                        },
                        new ChiTietHoaDon
                        {
                            MaHoaDon = hoaDon.MaHoaDon,
                            LoaiKhoan = "Dien",
                            SoTien = request.TienDien
                        },
                        new ChiTietHoaDon
                        {
                            MaHoaDon = hoaDon.MaHoaDon,
                            LoaiKhoan = "Nuoc",
                            SoTien = request.TienNuoc
                        }
                    };

                    // Thêm chi tiết dịch vụ nếu có dịch vụ hợp lệ
                    if (danhSachDichVu != null && danhSachDichVu.Count > 0)
                    {
                        foreach (var dichVu in danhSachDichVu)
                        {
                            chiTietHoaDon.Add(new ChiTietHoaDon
                            {
                                MaHoaDon = hoaDon.MaHoaDon,
                                LoaiKhoan = "DichVu",
                                MaDichVu = dichVu.MaDichVu,
                                SoLuong = 1,
                                DonGia = (decimal)dichVu.Tiendichvu,
                                SoTien = (decimal)dichVu.Tiendichvu
                            });
                        }
                    }

                    // Thêm chi tiết hóa đơn
                    _context.ChiTietHoaDon.AddRange(chiTietHoaDon);
                    await _context.SaveChangesAsync();

                    // Gửi email thông báo cho người thuê nếu có email
                    if (nguoiThue != null && !string.IsNullOrEmpty(nguoiThue.Email))
                    {
                        var subject = $"[Nhắc nhở] Hóa đơn phòng {phong.TenPhong} kỳ {request.KyHoaDon}";
                        var body = $"Xin chào {nguoiThue.HoTen},<br>Bạn đã có hóa đơn mới cho phòng <b>{phong.TenPhong}</b> kỳ <b>{request.KyHoaDon}</b>.<br>Tổng tiền: <b>{tongTien:N0} VNĐ</b>.<br>Vui lòng thanh toán đúng hạn.<br>Trân trọng!";
                        try { await _emailService.SendEmailAsync(nguoiThue.Email, subject, body); } catch { /* ignore lỗi gửi mail */ }
                    }

                    return Ok(new
                    {
                        Message = "Tạo hóa đơn thành công",
                        MaHoaDon = hoaDon.MaHoaDon,
                        TongTien = hoaDon.TongTien,
                        KyHoaDon = hoaDon.KyHoaDon
                    });
                }
                catch (DbUpdateException ex)
                {
                    // Nếu lỗi xảy ra sau khi đã tạo hóa đơn, xóa hóa đơn đó
                    if (hoaDon.MaHoaDon > 0)
                    {
                        var hoaDonToDelete = await _context.HoaDon.FindAsync(hoaDon.MaHoaDon);
                        if (hoaDonToDelete != null)
                        {
                            _context.HoaDon.Remove(hoaDonToDelete);
                            await _context.SaveChangesAsync();
                        }
                    }

                    var innerException = ex.InnerException;
                    var errorMessage = innerException?.Message ?? ex.Message;
                    
                    // Log chi tiết lỗi
                    Console.WriteLine($"DbUpdateException: {ex.Message}");
                    if (innerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {innerException.Message}");
                        Console.WriteLine($"Stack Trace: {innerException.StackTrace}");
                    }
                    
                    return StatusCode(500, $"Lỗi khi tạo hóa đơn: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                
                // Log chi tiết lỗi
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                return StatusCode(500, $"Lỗi khi tạo hóa đơn: {ex.Message}");
            }
        }

        // Cập nhật hóa đơn
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHoaDon(int id, [FromBody] HoaDonUpdateDto dto)
        {
            if (id != dto.MaHoaDon)
                return BadRequest("ID không khớp.");

            var hoaDon = await _context.HoaDon
                .Include(h => h.ChiTietHoaDon)
                .FirstOrDefaultAsync(h => h.MaHoaDon == id);

            if (hoaDon == null)
                return NotFound("Không tìm thấy hóa đơn.");

            var phong = await _context.Phong.FindAsync(dto.MaPhong);
            if (phong == null)
                return BadRequest("Phòng không tồn tại.");

            // Lấy danh sách dịch vụ
            var danhSachDichVu = await _context.DichVu.ToListAsync();
            var tongTienDichVu = danhSachDichVu.Sum(dv => (decimal)dv.Tiendichvu);

            // Cập nhật thông tin cơ bản
            hoaDon.MaNguoiThue = dto.MaNguoiThue;
            hoaDon.MaPhong = dto.MaPhong;
            hoaDon.NgayLap = dto.NgayLap;
            hoaDon.KyHoaDon = dto.KyHoaDon;
            hoaDon.TongTien = dto.TienPhong + dto.TienDien + dto.TienNuoc + tongTienDichVu;
            hoaDon.TienDichVu = tongTienDichVu;

            // Cập nhật chi tiết hóa đơn
            var chiTietPhong = hoaDon.ChiTietHoaDon.FirstOrDefault(ct => ct.LoaiKhoan == "Phong");
            if (chiTietPhong != null)
                chiTietPhong.SoTien = dto.TienPhong;

            var chiTietDien = hoaDon.ChiTietHoaDon.FirstOrDefault(ct => ct.LoaiKhoan == "Dien");
            if (chiTietDien != null)
                chiTietDien.SoTien = dto.TienDien;

            var chiTietNuoc = hoaDon.ChiTietHoaDon.FirstOrDefault(ct => ct.LoaiKhoan == "Nuoc");
            if (chiTietNuoc != null)
                chiTietNuoc.SoTien = dto.TienNuoc;

            // Cập nhật chi tiết dịch vụ
            var chiTietDichVu = hoaDon.ChiTietHoaDon.Where(ct => ct.LoaiKhoan == "DichVu").ToList();
            foreach (var ct in chiTietDichVu)
            {
                _context.ChiTietHoaDon.Remove(ct);
            }

            foreach (var dichVu in danhSachDichVu)
            {
                hoaDon.ChiTietHoaDon.Add(new ChiTietHoaDon
                {
                    LoaiKhoan = "DichVu",
                    MaDichVu = dichVu.MaDichVu,
                    SoLuong = 1,
                    DonGia = (decimal)dichVu.Tiendichvu,
                    SoTien = (decimal)dichVu.Tiendichvu
                });
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Xoá hóa đơn
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHoaDon(int id)
        {
            var hoaDon = await _context.HoaDon
                .Include(h => h.ChiTietHoaDon)
                .FirstOrDefaultAsync(h => h.MaHoaDon == id);

            if (hoaDon == null)
                return NotFound();

            _context.ChiTietHoaDon.RemoveRange(hoaDon.ChiTietHoaDon);
            _context.HoaDon.Remove(hoaDon);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // API kiểm tra phòng đã có hóa đơn kỳ này chưa
        [HttpGet("CheckHoaDon/{maPhong}/{kyHoaDon}")]
        public async Task<ActionResult<bool>> CheckHoaDon(int maPhong, string kyHoaDon)
        {
            try
            {
                var hasBill = await _context.HoaDon
                    .AnyAsync(h => h.MaPhong == maPhong && h.KyHoaDon == kyHoaDon);
                return Ok(hasBill);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi kiểm tra hóa đơn: {ex.Message}" });
            }
        }

        // In hóa đơn (xuất HTML đơn giản, có thể chuyển sang PDF nếu muốn)
        [HttpGet("Print/{id}")]
        public async Task<IActionResult> PrintHoaDon(int id)
        {
            var hoaDon = await _context.HoaDon
                .Include(h => h.Phong)
                .Include(h => h.NguoiThue)
                .Include(h => h.ChiTietHoaDon)
                    .ThenInclude(ct => ct.DichVu)
                .FirstOrDefaultAsync(h => h.MaHoaDon == id);

            if (hoaDon == null)
                return NotFound("Không tìm thấy hóa đơn.");

            // Tạo HTML đơn giản cho hóa đơn
            var html = $@"
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <title>Hóa đơn #{hoaDon.Phong.TenPhong}</title>
                    <style>
                        body {{ font-family: Arial; }}
                        table, th, td {{ border: 1px solid black; border-collapse: collapse; }}
                        th, td {{ padding: 8px; }}
                    </style>
                </head>
                <body>
                    <h2>HÓA ĐƠN THANH TOÁN</h2>
                    <p><b>Phòng:</b> {hoaDon.Phong.TenPhong}</p>
                    <p><b>Người thuê:</b> {hoaDon.NguoiThue.HoTen}</p>
                    <p><b>Kỳ hóa đơn:</b> {hoaDon.KyHoaDon}</p>
                    <p><b>Ngày lập:</b> {hoaDon.NgayLap:dd/MM/yyyy}</p>
                    <table>
                        <tr>
                            <th>Khoản</th>
                            <th>Số tiền</th>
                        </tr>
                        <tr><td>Tiền phòng</td><td>{hoaDon.ChiTietHoaDon.FirstOrDefault(x=>x.LoaiKhoan=="Phong")?.SoTien:N0}</td></tr>
                        <tr><td>Tiền điện</td><td>{hoaDon.ChiTietHoaDon.FirstOrDefault(x=>x.LoaiKhoan=="Dien")?.SoTien:N0}</td></tr>
                        <tr><td>Tiền nước</td><td>{hoaDon.ChiTietHoaDon.FirstOrDefault(x=>x.LoaiKhoan=="Nuoc")?.SoTien:N0}</td></tr>
                        <tr><td>Dịch vụ</td><td>{hoaDon.ChiTietHoaDon.Where(x=>x.LoaiKhoan=="DichVu").Sum(x=>x.SoTien):N0}</td></tr>
                        <tr><th>Tổng cộng</th><th>{hoaDon.TongTien:N0}</th></tr>
                    </table>
                </body>
                </html>
            ";

            return Content(html, "text/html; charset=utf-8");
        }

        // In hóa đơn ra PDF
        [HttpGet("PrintPdf/{id}")]
        public async Task<IActionResult> PrintHoaDonPdf(int id)
        {
            var hoaDon = await _context.HoaDon
                .Include(h => h.Phong)
                .Include(h => h.NguoiThue)
                .Include(h => h.ChiTietHoaDon)
                    .ThenInclude(ct => ct.DichVu)
                .FirstOrDefaultAsync(h => h.MaHoaDon == id);

            if (hoaDon == null)
                return NotFound("Không tìm thấy hóa đơn.");

            var html = $@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Hóa đơn #{hoaDon.MaHoaDon}</title>
                    <style>
                        body {{ font-family: Arial; }}
                        table, th, td {{ border: 1px solid black; border-collapse: collapse; }}
                        th, td {{ padding: 8px; }}
                    </style>
                </head>
                <body>
                    <h2>HÓA ĐƠN THANH TOÁN</h2>
                    <p><b>Phòng:</b> {hoaDon.Phong.TenPhong}</p>
                    <p><b>Người thuê:</b> {hoaDon.NguoiThue.HoTen}</p>
                    <p><b>Kỳ hóa đơn:</b> {hoaDon.KyHoaDon}</p>
                    <p><b>Ngày lập:</b> {hoaDon.NgayLap:dd/MM/yyyy}</p>
                    <table>
                        <tr>
                            <th>Khoản</th>
                            <th>Số tiền</th>
                        </tr>
                        <tr><td>Tiền phòng</td><td>{hoaDon.ChiTietHoaDon.FirstOrDefault(x=>x.LoaiKhoan=="Phong")?.SoTien:N0}</td></tr>
                        <tr><td>Tiền điện</td><td>{hoaDon.ChiTietHoaDon.FirstOrDefault(x=>x.LoaiKhoan=="Dien")?.SoTien:N0}</td></tr>
                        <tr><td>Tiền nước</td><td>{hoaDon.ChiTietHoaDon.FirstOrDefault(x=>x.LoaiKhoan=="Nuoc")?.SoTien:N0}</td></tr>
                        <tr><td>Dịch vụ</td><td>{hoaDon.ChiTietHoaDon.Where(x=>x.LoaiKhoan=="DichVu").Sum(x=>x.SoTien):N0}</td></tr>
                        <tr><th>Tổng cộng</th><th>{hoaDon.TongTien:N0}</th></tr>
                    </table>
                </body>
                </html>
            ";

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    PaperSize = DinkToPdf.PaperKind.A4,
                    Orientation = DinkToPdf.Orientation.Portrait,
                    DocumentTitle = $"HoaDon_{hoaDon.MaHoaDon}.pdf"
                },
                Objects = {
                    new ObjectSettings() {
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            var pdf = _converter.Convert(doc);
            return File(pdf, "application/pdf", $"HoaDon_{hoaDon.MaHoaDon}.pdf");
        }
    }
}