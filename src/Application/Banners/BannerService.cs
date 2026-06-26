using HungStore.Application.Banners.Dtos;
using HungStore.Application.Banners.Interfaces;
using HungStore.Application.Common;
using HungStore.Application.Products.Interfaces;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.Banners;

public class BannerService : IBannerService
{
    private readonly IBannerRepository _bannerRepository;
    private readonly IFileStorageService _fileStorageService;

    public BannerService(IBannerRepository bannerRepository, IFileStorageService fileStorageService)
    {
        _bannerRepository = bannerRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<IReadOnlyList<BannerDto>> GetAllAsync()
    {
        var banners = await _bannerRepository.GetAllAsync();
        return banners.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<BannerDto>> GetActiveAsync()
    {
        var banners = await _bannerRepository.GetActiveAsync();
        return banners.Select(MapToDto).ToList();
    }

    public async Task<ServiceResult<BannerDto>> GetByIdAsync(Guid id)
    {
        var banner = await _bannerRepository.GetByIdAsync(id);
        if (banner is null)
        {
            return ServiceResult<BannerDto>.Failure("Không tìm thấy banner.");
        }

        return ServiceResult<BannerDto>.Success(MapToDto(banner));
    }

    public async Task<ServiceResult<BannerDto>> CreateAsync(CreateBannerRequest request)
    {
        var banner = new Banner
        {
            Title = request.Title,
            Subtitle = request.Subtitle,
            LinkUrl = request.LinkUrl,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive
        };

        await _bannerRepository.AddAsync(banner);

        return ServiceResult<BannerDto>.Success(MapToDto(banner));
    }

    public async Task<ServiceResult<BannerDto>> UpdateAsync(Guid id, UpdateBannerRequest request)
    {
        var banner = await _bannerRepository.GetByIdAsync(id);
        if (banner is null)
        {
            return ServiceResult<BannerDto>.Failure("Không tìm thấy banner.");
        }

        banner.Title = request.Title;
        banner.Subtitle = request.Subtitle;
        banner.LinkUrl = request.LinkUrl;
        banner.DisplayOrder = request.DisplayOrder;
        banner.IsActive = request.IsActive;

        await _bannerRepository.UpdateAsync(banner);

        return ServiceResult<BannerDto>.Success(MapToDto(banner));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        var banner = await _bannerRepository.GetByIdAsync(id);
        if (banner is null)
        {
            return ServiceResult<bool>.Failure("Không tìm thấy banner.");
        }

        await _bannerRepository.DeleteAsync(banner);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<BannerDto>> UpdateImageAsync(Guid id, Stream content, string contentType)
    {
        var banner = await _bannerRepository.GetByIdAsync(id);
        if (banner is null)
        {
            return ServiceResult<BannerDto>.Failure("Không tìm thấy banner.");
        }

        banner.ImageUrl = await _fileStorageService.SaveBannerImageAsync(content, contentType);
        await _bannerRepository.UpdateAsync(banner);

        return ServiceResult<BannerDto>.Success(MapToDto(banner));
    }

    private static BannerDto MapToDto(Banner banner)
    {
        return new BannerDto
        {
            Id = banner.Id,
            ImageUrl = banner.ImageUrl,
            Title = banner.Title,
            Subtitle = banner.Subtitle,
            LinkUrl = banner.LinkUrl,
            DisplayOrder = banner.DisplayOrder,
            IsActive = banner.IsActive
        };
    }
}
