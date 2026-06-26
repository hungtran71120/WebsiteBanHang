using HungStore.Application.Common;
using HungStore.Application.FlashSales.Dtos;
using HungStore.Application.FlashSales.Interfaces;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.FlashSales;

public class FlashSaleService : IFlashSaleService
{
    private readonly IFlashSaleRepository _flashSaleRepository;
    private readonly IProductRepository _productRepository;
    private readonly IReviewRepository _reviewRepository;

    public FlashSaleService(IFlashSaleRepository flashSaleRepository, IProductRepository productRepository, IReviewRepository reviewRepository)
    {
        _flashSaleRepository = flashSaleRepository;
        _productRepository = productRepository;
        _reviewRepository = reviewRepository;
    }

    public async Task<FlashSaleDto?> GetActiveFlashSaleAsync()
    {
        var flashSale = await _flashSaleRepository.GetActiveAsync(DateTime.UtcNow);
        return flashSale is null ? null : await MapToDtoAsync(flashSale);
    }

    public async Task<PagedResult<FlashSaleDto>> GetPagedAsync(int page, int pageSize)
    {
        var (items, totalCount) = await _flashSaleRepository.GetPagedAsync(page, pageSize);

        var dtos = new List<FlashSaleDto>();
        foreach (var item in items)
        {
            dtos.Add(await MapToDtoAsync(item));
        }

        return new PagedResult<FlashSaleDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ServiceResult<FlashSaleDto>> GetByIdAsync(Guid id)
    {
        var flashSale = await _flashSaleRepository.GetByIdAsync(id);
        if (flashSale is null)
        {
            return ServiceResult<FlashSaleDto>.Failure("Không tìm thấy flash sale.");
        }

        return ServiceResult<FlashSaleDto>.Success(await MapToDtoAsync(flashSale));
    }

    public async Task<ServiceResult<FlashSaleDto>> CreateAsync(CreateFlashSaleRequest request)
    {
        if (request.EndsAt <= request.StartsAt)
        {
            return ServiceResult<FlashSaleDto>.Failure("Thời gian kết thúc phải sau thời gian bắt đầu.");
        }

        var flashSale = new FlashSale
        {
            Name = request.Name,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            IsActive = request.IsActive
        };

        await _flashSaleRepository.AddAsync(flashSale);

        return ServiceResult<FlashSaleDto>.Success(await MapToDtoAsync(flashSale));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        var flashSale = await _flashSaleRepository.GetByIdAsync(id);
        if (flashSale is null)
        {
            return ServiceResult<bool>.Failure("Không tìm thấy flash sale.");
        }

        await _flashSaleRepository.DeleteAsync(flashSale);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<FlashSaleDto>> AddItemAsync(Guid flashSaleId, AddFlashSaleItemRequest request)
    {
        var flashSale = await _flashSaleRepository.GetByIdAsync(flashSaleId);
        if (flashSale is null)
        {
            return ServiceResult<FlashSaleDto>.Failure("Không tìm thấy flash sale.");
        }

        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product is null)
        {
            return ServiceResult<FlashSaleDto>.Failure("Không tìm thấy sản phẩm.");
        }

        if (flashSale.Items.Any(i => i.ProductId == request.ProductId))
        {
            return ServiceResult<FlashSaleDto>.Failure("Sản phẩm này đã có trong flash sale.");
        }

        if (request.SalePrice >= product.Price)
        {
            return ServiceResult<FlashSaleDto>.Failure("Giá flash sale phải thấp hơn giá gốc.");
        }

        var item = new FlashSaleItem
        {
            FlashSaleId = flashSaleId,
            ProductId = request.ProductId,
            Product = product,
            SalePrice = request.SalePrice,
            QuantityLimit = request.QuantityLimit
        };

        await _flashSaleRepository.AddItemAsync(item);

        return ServiceResult<FlashSaleDto>.Success(await MapToDtoAsync(flashSale));
    }

    public async Task<ServiceResult<FlashSaleDto>> UpdateItemAsync(Guid flashSaleId, Guid itemId, UpdateFlashSaleItemRequest request)
    {
        var flashSale = await _flashSaleRepository.GetByIdAsync(flashSaleId);
        if (flashSale is null)
        {
            return ServiceResult<FlashSaleDto>.Failure("Không tìm thấy flash sale.");
        }

        var item = await _flashSaleRepository.GetItemAsync(flashSaleId, itemId);
        if (item is null)
        {
            return ServiceResult<FlashSaleDto>.Failure("Không tìm thấy sản phẩm trong flash sale.");
        }

        item.SalePrice = request.SalePrice;
        item.QuantityLimit = request.QuantityLimit;
        await _flashSaleRepository.UpdateItemAsync(item);

        return ServiceResult<FlashSaleDto>.Success(await MapToDtoAsync(flashSale));
    }

    public async Task<ServiceResult<FlashSaleDto>> DeleteItemAsync(Guid flashSaleId, Guid itemId)
    {
        var flashSale = await _flashSaleRepository.GetByIdAsync(flashSaleId);
        if (flashSale is null)
        {
            return ServiceResult<FlashSaleDto>.Failure("Không tìm thấy flash sale.");
        }

        var item = await _flashSaleRepository.GetItemAsync(flashSaleId, itemId);
        if (item is null)
        {
            return ServiceResult<FlashSaleDto>.Failure("Không tìm thấy sản phẩm trong flash sale.");
        }

        await _flashSaleRepository.DeleteItemAsync(item);
        flashSale.Items.Remove(item);

        return ServiceResult<FlashSaleDto>.Success(await MapToDtoAsync(flashSale));
    }

    private async Task<FlashSaleDto> MapToDtoAsync(FlashSale flashSale)
    {
        var items = new List<FlashSaleItemDto>();
        foreach (var i in flashSale.Items)
        {
            var (averageRating, reviewCount) = await _reviewRepository.GetRatingSummaryAsync(i.ProductId);
            items.Add(new FlashSaleItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                ImageUrl = i.Product?.ImageUrl,
                OriginalPrice = i.Product?.Price ?? 0,
                SalePrice = i.SalePrice,
                QuantityLimit = i.QuantityLimit,
                QuantitySold = i.QuantitySold,
                AverageRating = averageRating,
                ReviewCount = reviewCount
            });
        }

        return new FlashSaleDto
        {
            Id = flashSale.Id,
            Name = flashSale.Name,
            StartsAt = flashSale.StartsAt,
            EndsAt = flashSale.EndsAt,
            IsActive = flashSale.IsActive,
            Items = items
        };
    }
}
