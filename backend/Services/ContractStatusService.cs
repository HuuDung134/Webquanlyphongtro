using System;
using System.Linq;
using DoAnCoSo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ContractStatusService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContractStatusService> _logger;

    public ContractStatusService(IServiceProvider serviceProvider, ILogger<ContractStatusService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Đợi DB sẵn sàng trước khi chạy vòng lặp chính
        int attempts = 0;
        const int maxAttempts = 10;
        const int delaySeconds = 5;
        
        while (!stoppingToken.IsCancellationRequested && attempts < maxAttempts)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (await context.Database.CanConnectAsync(stoppingToken))
                    break;
            }
            catch { /* ignore và thử lại */ }
            
            attempts++;
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await UpdateRoomStatus(context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái phòng");
            }

            // Chạy lại sau 24 giờ
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task UpdateRoomStatus(ApplicationDbContext context)
    {
        var now = DateTime.Now;

        var rooms = await context.Phong.ToListAsync();
        var hopdongs = await context.HopDong.ToListAsync();

        foreach (var room in rooms)
        {
            if (room.TrangThai == 2) continue; // Giữ nguyên khi bảo trì

            var hasActiveContract = hopdongs.Any(hd =>
                hd.MaPhong == room.MaPhong &&
                hd.NgayBatDau <= now &&
                (!hd.NgayKetThuc.HasValue || hd.NgayKetThuc.Value >= now));

            room.TrangThai = hasActiveContract ? 1 : 0;
        }

        await context.SaveChangesAsync();
    }
}
