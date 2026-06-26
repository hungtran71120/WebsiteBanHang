using HungStore.Domain.Entities;

namespace HungStore.Domain.Interfaces;

public interface IFlashSaleRepository
{
    Task<FlashSale?> GetByIdAsync(Guid id);
    Task<(IReadOnlyList<FlashSale> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
    Task<FlashSale?> GetActiveAsync(DateTime now);
    Task<IReadOnlyDictionary<Guid, FlashSaleItem>> GetActiveItemsForProductsAsync(IReadOnlyCollection<Guid> productIds, DateTime now);
    Task AddAsync(FlashSale flashSale);
    Task UpdateAsync(FlashSale flashSale);
    Task DeleteAsync(FlashSale flashSale);

    Task<FlashSaleItem?> GetItemAsync(Guid flashSaleId, Guid itemId);
    Task AddItemAsync(FlashSaleItem item);
    Task UpdateItemAsync(FlashSaleItem item);
    Task DeleteItemAsync(FlashSaleItem item);
    Task<bool> TryIncrementQuantitySoldAsync(Guid flashSaleItemId, int quantity);
}
