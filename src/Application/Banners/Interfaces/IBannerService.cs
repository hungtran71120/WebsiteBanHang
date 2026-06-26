using HungStore.Application.Banners.Dtos;
using HungStore.Application.Common;

namespace HungStore.Application.Banners.Interfaces;

public interface IBannerService
{
    Task<IReadOnlyList<BannerDto>> GetAllAsync();
    Task<IReadOnlyList<BannerDto>> GetActiveAsync();
    Task<ServiceResult<BannerDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<BannerDto>> CreateAsync(CreateBannerRequest request);
    Task<ServiceResult<BannerDto>> UpdateAsync(Guid id, UpdateBannerRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult<BannerDto>> UpdateImageAsync(Guid id, Stream content, string contentType);
}
