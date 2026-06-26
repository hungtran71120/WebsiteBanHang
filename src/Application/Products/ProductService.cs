using HungStore.Application.Common;
using HungStore.Application.Products.Dtos;
using HungStore.Application.Products.Interfaces;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.Products;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IReviewRepository _reviewRepository;
    private readonly IFlashSaleRepository _flashSaleRepository;
    private readonly IOrderRepository _orderRepository;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IFileStorageService fileStorageService,
        IReviewRepository reviewRepository,
        IFlashSaleRepository flashSaleRepository,
        IOrderRepository orderRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _fileStorageService = fileStorageService;
        _reviewRepository = reviewRepository;
        _flashSaleRepository = flashSaleRepository;
        _orderRepository = orderRepository;
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(ProductFilterRequest filter)
    {
        var (items, totalCount) = await _productRepository.GetPagedAsync(
            filter.Keyword, filter.CategoryId, filter.MinPrice, filter.MaxPrice, filter.SortBy, filter.Page, filter.PageSize);

        var dtos = items.Select(MapToDto).ToList();
        await DecorateWithRatingsAndSalesAsync(dtos);
        await DecorateWithFlashSaleAsync(dtos);

        return new PagedResult<ProductDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ServiceResult<ProductDto>> GetByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy sản phẩm.");
        }

        var dto = MapToDto(product);
        await DecorateWithRatingsAndSalesAsync(new List<ProductDto> { dto });
        await DecorateWithFlashSaleAsync(new List<ProductDto> { dto });

        return ServiceResult<ProductDto>.Success(dto);
    }

    private async Task DecorateWithRatingsAndSalesAsync(List<ProductDto> dtos)
    {
        if (dtos.Count == 0)
        {
            return;
        }

        var ids = dtos.Select(d => d.Id).ToList();
        var ratings = await _reviewRepository.GetRatingSummariesAsync(ids);
        var soldCounts = await _orderRepository.GetSoldCountsAsync(ids);

        foreach (var dto in dtos)
        {
            if (ratings.TryGetValue(dto.Id, out var rating))
            {
                dto.AverageRating = rating.AverageRating;
                dto.ReviewCount = rating.ReviewCount;
            }

            dto.SoldCount = soldCounts.TryGetValue(dto.Id, out var sold) ? sold : 0;
        }
    }

    private async Task DecorateWithFlashSaleAsync(List<ProductDto> dtos)
    {
        if (dtos.Count == 0)
        {
            return;
        }

        var activeItems = await _flashSaleRepository.GetActiveItemsForProductsAsync(
            dtos.Select(d => d.Id).ToList(), DateTime.UtcNow);

        foreach (var dto in dtos)
        {
            if (activeItems.TryGetValue(dto.Id, out var item))
            {
                dto.FlashSalePrice = item.SalePrice;
                dto.FlashSaleQuantityRemaining = item.QuantityLimit - item.QuantitySold;
                dto.FlashSaleEndsAt = item.FlashSale?.EndsAt;
            }
        }
    }

    public async Task<ServiceResult<ProductDto>> CreateAsync(CreateProductRequest request)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category is null)
        {
            return ServiceResult<ProductDto>.Failure("Danh mục không tồn tại.");
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            CategoryId = request.CategoryId
        };

        await _productRepository.AddAsync(product);
        product.Category = category;

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy sản phẩm.");
        }

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category is null)
        {
            return ServiceResult<ProductDto>.Failure("Danh mục không tồn tại.");
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.CategoryId = request.CategoryId;
        product.Category = category;

        await _productRepository.UpdateAsync(product);

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return ServiceResult<bool>.Failure("Không tìm thấy sản phẩm.");
        }

        await _productRepository.DeleteAsync(product);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<ProductDto>> UpdateImageAsync(Guid id, Stream content, string contentType)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy sản phẩm.");
        }

        product.ImageUrl = await _fileStorageService.SaveProductImageAsync(content, contentType);
        await _productRepository.UpdateAsync(product);

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<ProductDto>> AddVariantOptionAsync(Guid productId, CreateVariantOptionRequest request)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy sản phẩm.");
        }

        if (product.VariantOptions.Count >= 2)
        {
            return ServiceResult<ProductDto>.Failure("Sản phẩm chỉ được tối đa 2 loại phân loại.");
        }

        if (product.VariantOptions.Any(o => o.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return ServiceResult<ProductDto>.Failure($"Loại phân loại \"{request.Name}\" đã tồn tại cho sản phẩm này.");
        }

        var distinctValues = request.Values.Select(v => v.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (distinctValues != request.Values.Count)
        {
            return ServiceResult<ProductDto>.Failure("Các giá trị trong cùng 1 loại phân loại không được trùng nhau.");
        }

        var option = new ProductVariantOption
        {
            ProductId = productId,
            Name = request.Name,
            DisplayOrder = product.VariantOptions.Count + 1,
            Values = request.Values.Select(v => new ProductVariantOptionValue { Value = v.Value }).ToList()
        };

        await _productRepository.AddVariantOptionAsync(option);

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<ProductDto>> AddVariantOptionValueAsync(Guid productId, Guid optionId, AddVariantOptionValueRequest request)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy sản phẩm.");
        }

        var option = await _productRepository.GetVariantOptionAsync(productId, optionId);
        if (option is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy loại phân loại.");
        }

        if (option.Values.Any(v => v.Value.Equals(request.Value, StringComparison.OrdinalIgnoreCase)))
        {
            return ServiceResult<ProductDto>.Failure($"Giá trị \"{request.Value}\" đã tồn tại trong loại phân loại này.");
        }

        var value = new ProductVariantOptionValue
        {
            ProductVariantOptionId = optionId,
            Value = request.Value
        };

        await _productRepository.AddVariantOptionValueAsync(value);

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<ProductDto>> DeleteVariantOptionValueAsync(Guid productId, Guid optionId, Guid valueId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy sản phẩm.");
        }

        var option = await _productRepository.GetVariantOptionAsync(productId, optionId);
        if (option is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy loại phân loại.");
        }

        var value = option.Values.FirstOrDefault(v => v.Id == valueId);
        if (value is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy giá trị phân loại.");
        }

        if (product.Variants.Any(v => v.OptionValue1Id == valueId || v.OptionValue2Id == valueId))
        {
            return ServiceResult<ProductDto>.Failure("Không thể xóa, đang có sản phẩm con sử dụng giá trị này.");
        }

        await _productRepository.DeleteVariantOptionValueAsync(value);

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<ProductDto>> DeleteVariantOptionAsync(Guid productId, Guid optionId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy sản phẩm.");
        }

        var option = await _productRepository.GetVariantOptionAsync(productId, optionId);
        if (option is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy loại phân loại.");
        }

        if (option.Values.Count > 0)
        {
            return ServiceResult<ProductDto>.Failure("Phải xóa hết giá trị trước khi xóa loại phân loại này.");
        }

        await _productRepository.DeleteVariantOptionAsync(option);

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<ProductDto>> UpdateVariantOptionValueImageAsync(Guid productId, Guid optionId, Guid valueId, Stream content, string contentType)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy sản phẩm.");
        }

        var option = await _productRepository.GetVariantOptionAsync(productId, optionId);
        if (option is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy loại phân loại.");
        }

        var value = option.Values.FirstOrDefault(v => v.Id == valueId);
        if (value is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy giá trị phân loại.");
        }

        value.ImageUrl = await _fileStorageService.SaveProductImageAsync(content, contentType);
        await _productRepository.UpdateVariantOptionValueAsync(value);

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<ProductDto>> AddVariantAsync(Guid productId, CreateProductVariantRequest request)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy sản phẩm.");
        }

        var option1 = product.VariantOptions.FirstOrDefault(o => o.DisplayOrder == 1);
        if (option1 is null)
        {
            return ServiceResult<ProductDto>.Failure("Sản phẩm chưa có loại phân loại nào, hãy tạo loại phân loại trước.");
        }

        var value1 = option1.Values.FirstOrDefault(v => v.Id == request.OptionValue1Id);
        if (value1 is null)
        {
            return ServiceResult<ProductDto>.Failure("Giá trị phân loại 1 không hợp lệ.");
        }

        var option2 = product.VariantOptions.FirstOrDefault(o => o.DisplayOrder == 2);
        ProductVariantOptionValue? value2 = null;

        if (option2 is not null)
        {
            if (request.OptionValue2Id is null)
            {
                return ServiceResult<ProductDto>.Failure("Vui lòng chọn đầy đủ giá trị phân loại thứ 2.");
            }

            value2 = option2.Values.FirstOrDefault(v => v.Id == request.OptionValue2Id.Value);
            if (value2 is null)
            {
                return ServiceResult<ProductDto>.Failure("Giá trị phân loại 2 không hợp lệ.");
            }
        }
        else if (request.OptionValue2Id is not null)
        {
            return ServiceResult<ProductDto>.Failure("Sản phẩm này chỉ có 1 loại phân loại.");
        }

        if (product.Variants.Any(v => v.OptionValue1Id == request.OptionValue1Id && v.OptionValue2Id == request.OptionValue2Id))
        {
            return ServiceResult<ProductDto>.Failure("Tổ hợp phân loại này đã tồn tại.");
        }

        var variant = new ProductVariant
        {
            ProductId = productId,
            OptionValue1Id = value1.Id,
            OptionValue1 = value1,
            OptionValue2Id = value2?.Id,
            OptionValue2 = value2,
            Stock = request.Stock
        };

        await _productRepository.AddVariantAsync(variant);

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<ProductDto>> UpdateVariantAsync(Guid productId, Guid variantId, UpdateProductVariantRequest request)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy sản phẩm.");
        }

        var variant = await _productRepository.GetVariantAsync(productId, variantId);
        if (variant is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy phân loại sản phẩm.");
        }

        variant.Stock = request.Stock;
        await _productRepository.UpdateVariantAsync(variant);

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<ProductDto>> DeleteVariantAsync(Guid productId, Guid variantId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy sản phẩm.");
        }

        var variant = await _productRepository.GetVariantAsync(productId, variantId);
        if (variant is null)
        {
            return ServiceResult<ProductDto>.Failure("Không tìm thấy phân loại sản phẩm.");
        }

        await _productRepository.DeleteVariantAsync(variant);

        return ServiceResult<ProductDto>.Success(MapToDto(product));
    }

    internal static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Variants.Count > 0 ? product.Variants.Sum(v => v.Stock) : product.Stock,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            VariantOptions = product.VariantOptions
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new ProductVariantOptionDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    DisplayOrder = o.DisplayOrder,
                    Values = o.Values.Select(v => new ProductVariantOptionValueDto
                    {
                        Id = v.Id,
                        Value = v.Value,
                        ImageUrl = v.ImageUrl
                    }).ToList()
                }).ToList(),
            Variants = product.Variants.Select(v => new ProductVariantDto
            {
                Id = v.Id,
                OptionValue1Id = v.OptionValue1Id,
                OptionValue1Text = v.OptionValue1?.Value ?? string.Empty,
                OptionValue2Id = v.OptionValue2Id,
                OptionValue2Text = v.OptionValue2?.Value,
                Stock = v.Stock
            }).ToList()
        };
    }
}
