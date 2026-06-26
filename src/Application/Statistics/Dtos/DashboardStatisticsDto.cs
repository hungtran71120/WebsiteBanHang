namespace HungStore.Application.Statistics.Dtos;

public class DashboardStatisticsDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalProducts { get; set; }
    public int TotalUsers { get; set; }
    public IReadOnlyDictionary<string, int> OrdersByStatus { get; set; } = new Dictionary<string, int>();
}
