using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DoAnCoSo.Data;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Services
{
    public class InvoiceBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<InvoiceBackgroundService> _logger;

        public InvoiceBackgroundService(
            IServiceProvider services,
            ILogger<InvoiceBackgroundService> logger)
        {
            _services = services;
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
                    using var scope = _services.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<DoAnCoSo.Data.ApplicationDbContext>();
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
                    using (var scope = _services.CreateScope())
                    {
                        var invoiceService = scope.ServiceProvider.GetRequiredService<HoaDonService>();
                        await invoiceService.GenerateMonthlyInvoices();
                        _logger.LogInformation("Đã chạy tạo hóa đơn tự động thành công");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Có lỗi xảy ra khi tạo hóa đơn tự động");
                }

                // Chạy mỗi ngày một lần vào lúc 00:00
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1);
                var delay = nextRun - now;
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
} 