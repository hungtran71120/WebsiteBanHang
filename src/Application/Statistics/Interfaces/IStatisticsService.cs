using HungStore.Application.Statistics.Dtos;

namespace HungStore.Application.Statistics.Interfaces;

public interface IStatisticsService
{
    Task<DashboardStatisticsDto> GetDashboardAsync();
}
