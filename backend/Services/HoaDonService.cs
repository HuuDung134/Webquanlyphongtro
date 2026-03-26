using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using System.Linq;
using System.Collections.Generic;

namespace DoAnCoSo.Services
{
    public class HoaDonService
    {
        private readonly ApplicationDbContext _context;

        public HoaDonService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task GenerateMonthlyInvoices()
        {
            // Lấy tất cả hợp đồng đang hoạt động (không có ngày kết thúc hoặc ngày kết thúc > hiện tại)
            var activeContracts = await _context.HopDong
                .Include(h => h.Phong)
                .Include(h => h.NguoiThue)
                .Where(h => h.NgayKetThuc == null || h.NgayKetThuc > DateTime.Now)
                .ToListAsync();

            var currentDate = DateTime.Now;

            // Lấy tổng tiền dịch vụ chung
            decimal tongTienDichVuChung = await _context.DichVu
                .SumAsync(dv => (decimal?)dv.Tiendichvu) ?? 0m;

            foreach (var contract in activeContracts)
            {
                // Tính ngày bắt đầu tính tiền (1 tháng sau ngày bắt đầu hợp đồng)
                var startBillingDate = contract.NgayBatDau.AddMonths(1);

                // Kiểm tra xem đã đến ngày tính tiền chưa
                if (currentDate >= startBillingDate)
                {
                    // Tạo kỳ hóa đơn theo định dạng "YYYY-MM"
                    var kyHoaDon = $"{currentDate.Year}-{currentDate.Month:D2}";

                    // Kiểm tra xem đã có hóa đơn cho tháng này chưa
                    var existingInvoice = await _context.HoaDon
                        .Where(h => h.MaPhong == contract.MaPhong &&
                                  h.KyHoaDon == kyHoaDon)
                        .FirstOrDefaultAsync();

                    if (existingInvoice == null)
                    {
                        // Lấy chỉ số điện và nước mới nhất
                        var chiSoDien = await _context.ChiSoDien
                            .Where(cd => cd.MaPhong == contract.MaPhong)
                            .OrderByDescending(cd => cd.NgayThangDien)
                            .FirstOrDefaultAsync();

                        var chiSoNuoc = await _context.ChiSoNuoc
                            .Where(cn => cn.MaPhong == contract.MaPhong)
                            .OrderByDescending(cn => cn.NgayThangNuoc)
                            .FirstOrDefaultAsync();

                        if (chiSoDien != null && chiSoNuoc != null)
                        {
                            // Tạo hóa đơn mới
                            var newInvoice = new HoaDon
                            {
                                MaNguoiThue = contract.MaNguoiThue,
                                MaPhong = contract.MaPhong,
                                MaDien = chiSoDien.MaDien,
                                MaNuoc = chiSoNuoc.MaNuoc,
                                NgayLap = currentDate,
                                KyHoaDon = kyHoaDon,
                                TienDichVu = tongTienDichVuChung,
                                TongTien = contract.Phong.GiaPhong + chiSoDien.TienDien + chiSoNuoc.TienNuoc + tongTienDichVuChung
                            };

                            // Tạo chi tiết hóa đơn
                            var chiTietHoaDon = new List<ChiTietHoaDon>
                            {
                                new ChiTietHoaDon
                                {
                                    LoaiKhoan = "Tiền phòng",
                                    SoTien = contract.Phong.GiaPhong
                                },
                                new ChiTietHoaDon
                                {
                                    LoaiKhoan = "Tiền điện",
                                    SoTien = chiSoDien.TienDien
                                },
                                new ChiTietHoaDon
                                {
                                    LoaiKhoan = "Tiền nước",
                                    SoTien = chiSoNuoc.TienNuoc
                                },
                                new ChiTietHoaDon
                                {
                                    LoaiKhoan = "Tiền dịch vụ",
                                    SoTien = tongTienDichVuChung
                                }
                            };

                            newInvoice.ChiTietHoaDon = chiTietHoaDon;
                            _context.HoaDon.Add(newInvoice);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
} 