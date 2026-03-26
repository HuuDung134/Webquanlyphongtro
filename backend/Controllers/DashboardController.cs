using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DoAnCoSo.Models;
using DoAnCoSo.Data;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace DoAnCoSo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetDashboardStatistics()
        {
            // Lấy trạng thái phòng (dạng số) để hiển thị/đếm
            var roomStatuses = await _context.Phong
                .Select(p => new
                {
                    p.TenPhong,
                    Status = p.TrangThai // 0: trống, 1: đang thuê, 2: bảo trì
                })
                .ToListAsync();

            var totalRooms = await _context.Phong.CountAsync();
            var occupiedRooms = await _context.Phong.CountAsync(p => p.TrangThai == 1);
            var vacantRooms = totalRooms - occupiedRooms;

            var statistics = new
            {
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                VacantRooms = vacantRooms,
                TotalTenants = await _context.NguoiThue.CountAsync(),
                TotalContracts = await _context.HopDong.CountAsync(),
                TotalBills = await _context.HoaDon.CountAsync(),
                TotalRevenue = await _context.HoaDon.SumAsync(h => h.TongTien),
                RecentWaterReadings = await _context.ChiSoNuoc
                    .OrderByDescending(w => w.NgayThangNuoc)
                    .Take(5)
                    .Select(w => new
                    {
                        w.MaNuoc,
                        w.SoNuocTieuThu,
                        w.TienNuoc,
                        w.NgayThangNuoc,
                        RoomNumber = w.Phong.TenPhong
                    })
                    .ToListAsync(),
                RecentElectricityReadings = await _context.ChiSoDien
                    .OrderByDescending(e => e.NgayThangDien)
                    .Take(5)
                    .Select(e => new
                    {
                        e.MaDien,
                        e.SoDienTieuThu,
                        e.TienDien,
                        e.NgayThangDien,
                        RoomNumber = e.Phong.TenPhong
                    })
                    .ToListAsync(),
                // Thêm thông tin debug
                RoomStatuses = roomStatuses
            };

            return Ok(statistics);
        }

        [HttpGet("room-status")]
        public async Task<IActionResult> GetRoomStatus()
        {
            var data = await _context.Phong
                .GroupBy(p => p.TrangThai)
                .Select(g => new
                {
                    status = g.Key == 0 ? "Còn trống" :
                             g.Key == 1 ? "Đang thuê" :
                             g.Key == 2 ? "Bảo trì" : "Khác",
                    count = g.Count(),
                    rooms = g.Select(p => p.TenPhong).ToList()
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("monthly-revenue")]
        public async Task<IActionResult> GetMonthlyRevenue()
        {
            var monthlyRevenue = await _context.HoaDon
                .GroupBy(h => new { h.NgayLap.Year, h.NgayLap.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalRevenue = g.Sum(h => h.TongTien)
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Take(12)
                .ToListAsync();

            return Ok(monthlyRevenue);
        }

     

        [HttpGet("recent-bills")]
        public async Task<IActionResult> GetRecentBills()
        {
            var recentBills = await _context.HoaDon
                .OrderByDescending(h => h.NgayLap)
                .Take(10)
                .Select(h => new
                {
                    h.MaHoaDon,
                    h.NgayLap,
                    h.TongTien,
                    RoomNumber = h.Phong.TenPhong,
                    TenantName = h.NguoiThue.HoTen
                })
                .ToListAsync();

            return Ok(recentBills);
        }

        [HttpGet("utility-usage")]
        public async Task<IActionResult> GetUtilityUsage()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var utilityUsage = new
            {
                ElectricityUsage = await _context.ChiSoDien
                    .Where(c => c.NgayThangDien.Month == currentMonth && c.NgayThangDien.Year == currentYear)
                    .SumAsync(c => c.SoDienTieuThu),
                WaterUsage = await _context.ChiSoNuoc
                    .Where(c => c.NgayThangNuoc.Month == currentMonth && c.NgayThangNuoc.Year == currentYear)
                    .SumAsync(c => c.SoNuocTieuThu),
                ElectricityCost = await _context.ChiSoDien
                    .Where(c => c.NgayThangDien.Month == currentMonth && c.NgayThangDien.Year == currentYear)
                    .SumAsync(c => c.TienDien),
                WaterCost = await _context.ChiSoNuoc
                    .Where(c => c.NgayThangNuoc.Month == currentMonth && c.NgayThangNuoc.Year == currentYear)
                    .SumAsync(c => c.TienNuoc)
            };

            return Ok(utilityUsage);
        }

        [HttpGet("contract-status")]
        public async Task<IActionResult> GetContractStatus()
        {
            var today = DateTime.Now.Date;
            var contractStatus = new
            {
                ActiveContracts = await _context.HopDong
                    .CountAsync(h => h.NgayKetThuc >= today),
                ExpiringContracts = await _context.HopDong
                    .CountAsync(h => h.NgayKetThuc >= today && h.NgayKetThuc <= today.AddDays(30)),
                ExpiredContracts = await _context.HopDong
                    .CountAsync(h => h.NgayKetThuc < today)
            };

            return Ok(contractStatus);
        }

        [HttpGet("revenue-by-month-year")]
        public async Task<IActionResult> GetRevenueByMonthYear(int? year = null, int? month = null)
        {
            var query = _context.HoaDon.AsQueryable();

            if (year.HasValue)
            {
                query = query.Where(h => h.NgayLap.Year == year.Value);
            }

            if (month.HasValue)
            {
                query = query.Where(h => h.NgayLap.Month == month.Value);
            }

            var revenue = await query
                .GroupBy(h => new { h.NgayLap.Year, h.NgayLap.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalRevenue = g.Sum(h => h.TongTien)
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();

            return Ok(revenue);
        }

        [HttpGet("unpaid-rooms")]
        public async Task<IActionResult> GetUnpaidRooms()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var currentPeriod = $"{currentYear}-{currentMonth:D2}";

            var unpaidRooms = await _context.HoaDon
                .Where(h => h.KyHoaDon == currentPeriod)
                .Select(h => new
                {
                    RoomNumber = h.Phong.TenPhong,
                    TenantName = h.NguoiThue.HoTen,
                    Amount = h.TongTien,
                    DueDate = h.NgayLap,
                    BillType = "Hóa đơn tháng " + h.NgayLap.Month + "/" + h.NgayLap.Year
                })
                .ToListAsync();

            return Ok(unpaidRooms);
        }

        [HttpGet("expiring-contracts")]
        public async Task<IActionResult> GetExpiringContracts(int daysThreshold = 30)
        {
            var today = DateTime.Now.Date;
            var expiringDate = today.AddDays(daysThreshold);

            var expiringContracts = await _context.HopDong
                .Where(h => h.NgayKetThuc >= today && h.NgayKetThuc <= expiringDate)
                .Select(h => new
                {
                    ContractId = h.MaHopDong,
                    RoomNumber = h.Phong.TenPhong,
                    TenantName = h.NguoiThue.HoTen,
                    StartDate = h.NgayBatDau,
                    EndDate = h.NgayKetThuc,
                    DaysUntilExpiration = EF.Functions.DateDiffDay(today, h.NgayKetThuc)
                })
                .OrderBy(h => h.DaysUntilExpiration)
                .ToListAsync();

            return Ok(expiringContracts);
        }

        [HttpGet("kpi")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetKPI()
        {
            var today = DateTime.Now.Date;
            
            // 1. Tính Occupancy Rate (Tỷ lệ lấp đầy)
            var totalRooms = await _context.Phong.CountAsync();
            var occupiedRooms = await _context.Phong.CountAsync(p => p.TrangThai == 1);
            var vacantRooms = totalRooms - occupiedRooms;
            var occupancyRate = totalRooms > 0 ? (double)occupiedRooms / totalRooms * 100 : 0;

            // 2. Tính DSO (Days Sales Outstanding)
            // Lấy tất cả hóa đơn và kiểm tra trạng thái thanh toán
            var allBills = await _context.HoaDon
                .Select(h => new
                {
                    h.MaHoaDon,
                    h.NgayLap,
                    h.TongTien,
                    TotalPaid = _context.ThanhToan
                        .Where(t => t.MaHoaDon == h.MaHoaDon && t.TrangThai == 1)
                        .Sum(t => (decimal?)t.TongTien) ?? 0
                })
                .ToListAsync();

            var unpaidBills = allBills
                .Where(h => h.TotalPaid < h.TongTien)
                .ToList();

            var unpaidInvoices = unpaidBills.Count;
            var totalUnpaidAmount = unpaidBills.Sum(h => (double)(h.TongTien - h.TotalPaid));
            
            // Tính số ngày chưa thanh toán cho mỗi hóa đơn
            double totalUnpaidDays = 0;
            foreach (var bill in unpaidBills)
            {
                var daysSinceInvoice = (today - bill.NgayLap.Date).TotalDays;
                if (daysSinceInvoice > 0)
                {
                    totalUnpaidDays += daysSinceInvoice;
                }
            }

            // DSO = Tổng số ngày chưa thanh toán / Số hóa đơn chưa thanh toán
            var dso = unpaidInvoices > 0 ? totalUnpaidDays / unpaidInvoices : 0;
            var averageUnpaidDays = dso;

            // 3. Tính số sự cố/100 phòng (phòng bảo trì)
            var maintenanceRooms = await _context.Phong.CountAsync(p => p.TrangThai == 2);
            var incidentsPer100Rooms = totalRooms > 0 ? (double)maintenanceRooms / totalRooms * 100 : 0;
            var totalIncidents = maintenanceRooms; // Dùng phòng bảo trì làm sự cố

            var kpiData = new
            {
                OccupancyRate = Math.Round(occupancyRate, 2),
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                VacantRooms = vacantRooms,
                DSO = Math.Round(dso, 2),
                UnpaidInvoices = unpaidInvoices,
                TotalUnpaidAmount = Math.Round(totalUnpaidAmount, 2),
                AverageUnpaidDays = Math.Round(averageUnpaidDays, 2),
                IncidentsPer100Rooms = Math.Round(incidentsPer100Rooms, 2),
                MaintenanceRooms = maintenanceRooms,
                TotalIncidents = totalIncidents
            };

            return Ok(kpiData);
        }

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
} 