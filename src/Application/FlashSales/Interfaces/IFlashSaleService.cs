using ShopeeClone.Application.Common;
using ShopeeClone.Application.FlashSales.Dtos;

namespace ShopeeClone.Application.FlashSales.Interfaces;

public interface IFlashSaleService
{
    Task<FlashSaleDto?> GetActiveFlashSaleAsync();
    Task<PagedResult<FlashSaleDto>> GetPagedAsync(int page, int pageSize);
    Task<ServiceResult<FlashSaleDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<FlashSaleDto>> CreateAsync(CreateFlashSaleRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult<FlashSaleDto>> AddItemAsync(Guid flashSaleId, AddFlashSaleItemRequest request);
    Task<ServiceResult<FlashSaleDto>> UpdateItemAsync(Guid flashSaleId, Guid itemId, UpdateFlashSaleItemRequest request);
    Task<ServiceResult<FlashSaleDto>> DeleteItemAsync(Guid flashSaleId, Guid itemId);
}
