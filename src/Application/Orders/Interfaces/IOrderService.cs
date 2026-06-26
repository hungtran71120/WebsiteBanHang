using HungStore.Application.Common;
using HungStore.Application.Orders.Dtos;
using HungStore.Domain.Enums;

namespace HungStore.Application.Orders.Interfaces;

public interface IOrderService
{
    Task<ServiceResult<OrderDto>> CreateOrderFromCartAsync(string userId, CreateOrderRequest request);
    Task<PagedResult<OrderSummaryDto>> GetMyOrdersAsync(string userId, OrderStatus? status, int page, int pageSize);
    Task<ServiceResult<OrderDto>> GetOrderByIdAsync(string userId, Guid orderId);
    Task<PagedResult<AdminOrderSummaryDto>> GetAllOrdersAsync(OrderStatus? status, int page, int pageSize);
    Task<ServiceResult<OrderDto>> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus);
    Task<ServiceResult<OrderDto>> ConfirmDeliveryAsync(string userId, Guid orderId);
    Task<int> AutoCompleteStaleShippedOrdersAsync(TimeSpan threshold);
}
