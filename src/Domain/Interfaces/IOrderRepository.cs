using ShopeeClone.Domain.Entities;
using ShopeeClone.Domain.Enums;

namespace ShopeeClone.Domain.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetByUserIdPagedAsync(string userId, OrderStatus? status, int page, int pageSize);
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetAllPagedAsync(OrderStatus? status, int page, int pageSize);
    Task<Order?> GetByIdAsync(Guid id);
    Task<bool> HasDeliveredOrderForProductAsync(string userId, Guid productId);
    Task<decimal> GetTotalRevenueAsync();
    Task<IReadOnlyDictionary<OrderStatus, int>> GetOrderCountsByStatusAsync();
    Task<IReadOnlyList<Guid>> GetPurchasedProductIdsAsync(string userId);
    Task<IReadOnlyList<Guid>> GetPurchasedCategoryIdsAsync(string userId);
    Task<IReadOnlyDictionary<Guid, int>> GetSoldCountsAsync(IReadOnlyCollection<Guid> productIds);
    Task AddStatusHistoryAsync(OrderStatusHistory history);
    Task<IReadOnlyList<OrderStatusHistory>> GetStatusHistoryAsync(Guid orderId);
    Task<IReadOnlyList<Guid>> GetOrderIdsEligibleForAutoDeliveryAsync(OrderStatus fromStatus, DateTime cutoff);
}
