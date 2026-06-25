using ShopeeClone.Application.Auth.Interfaces;
using ShopeeClone.Application.Statistics.Dtos;
using ShopeeClone.Application.Statistics.Interfaces;
using ShopeeClone.Domain.Enums;
using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Application.Statistics;

public class StatisticsService : IStatisticsService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IIdentityService _identityService;

    public StatisticsService(IOrderRepository orderRepository, IProductRepository productRepository, IIdentityService identityService)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _identityService = identityService;
    }

    public async Task<DashboardStatisticsDto> GetDashboardAsync()
    {
        var totalRevenue = await _orderRepository.GetTotalRevenueAsync();
        var countsByStatus = await _orderRepository.GetOrderCountsByStatusAsync();
        var totalProducts = await _productRepository.CountAsync();
        var totalUsers = await _identityService.CountUsersAsync();

        var ordersByStatus = Enum.GetValues<OrderStatus>()
            .ToDictionary(s => s.ToString(), s => countsByStatus.GetValueOrDefault(s));

        return new DashboardStatisticsDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = countsByStatus.Values.Sum(),
            TotalProducts = totalProducts,
            TotalUsers = totalUsers,
            OrdersByStatus = ordersByStatus
        };
    }
}
