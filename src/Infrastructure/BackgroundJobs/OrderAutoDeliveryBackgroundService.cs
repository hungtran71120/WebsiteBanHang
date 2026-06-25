using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopeeClone.Application.Orders.Interfaces;

namespace ShopeeClone.Infrastructure.BackgroundJobs;

public class OrderAutoDeliveryBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan AutoDeliveryThreshold = TimeSpan.FromDays(7);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderAutoDeliveryBackgroundService> _logger;

    public OrderAutoDeliveryBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OrderAutoDeliveryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var completedCount = await orderService.AutoCompleteStaleShippedOrdersAsync(AutoDeliveryThreshold);
                if (completedCount > 0)
                {
                    _logger.LogInformation("Auto-completed {Count} order(s) that were Shipped for more than {Days} days.", completedCount, AutoDeliveryThreshold.TotalDays);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run order auto-delivery sweep.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
