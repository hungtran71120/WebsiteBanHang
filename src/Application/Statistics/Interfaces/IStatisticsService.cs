using ShopeeClone.Application.Statistics.Dtos;

namespace ShopeeClone.Application.Statistics.Interfaces;

public interface IStatisticsService
{
    Task<DashboardStatisticsDto> GetDashboardAsync();
}
