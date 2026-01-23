using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatoris.Service.Services
{
    public class BannerService
    {
        private readonly DomusDbContext _context;
        private readonly IMapper _mapper;

        public BannerService(DomusDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<BannerDto?> GetActiveBannerAsync(int companyId)
        {
            var banner = await _context.Banners
                .Where(b => b.CompanyId == companyId && b.IsApproved && b.IsActive)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            return banner == null ? null : _mapper.Map<BannerDto>(banner);
        }

        public async Task<BannerDto?> GetAnyActiveBannerAsync()
        {
            var banner = await _context.Banners
                .Where(b => b.IsApproved && b.IsActive)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            return banner == null ? null : _mapper.Map<BannerDto>(banner);
        }

        public async Task<BannerDto?> GetByIdAsync(int id, int companyId)
        {
            var banner = await _context.Banners
                .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId);

            return banner == null ? null : _mapper.Map<BannerDto>(banner);
        }

        public async Task<List<BannerDto>> GetAllAsync(int companyId)
        {
            var list = await _context.Banners
                .Where(b => b.CompanyId == companyId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<BannerDto>>(list);
        }

        public async Task<List<BannerSummaryDto>> GetSummariesAsync(int companyId)
        {
            var list = await _context.Banners
                .Where(b => b.CompanyId == companyId)
                .OrderByDescending(b => b.CreatedAt)
                // Burada .Select() kullanarak yalnızca belirli kolonları veritabanından çekebiliriz,
                // ancak entity'den map'lediğimiz için entity'yi çekmek EF+AutoMapper ile daha kolaydır.
                // Gerçek optimizasyon için doğrudan projeksiyon yapmalıyız.
                // Ancak 'Banner' entity'sinde HtmlContent alanı büyük.
                // HtmlContent'i çekmekten kaçınmak için doğrudan DTO'ya ya da anonim tipe projekte etmeliyiz.
                .Select(b => new Banner
                {
                    Id = b.Id,
                    CompanyId = b.CompanyId,
                    Topic = b.Topic,
                    IsApproved = b.IsApproved,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt
                    // HtmlContent is NOT selected, so it will be null/default
                })
                .ToListAsync();

            return _mapper.Map<List<BannerSummaryDto>>(list);
        }

        public async Task<BannerDto> CreateAsync(CreateBannerDto dto)
        {
            var entity = new Banner
            {
                CompanyId = dto.CompanyId,
                Topic = dto.Topic,
                HtmlContent = dto.HtmlContent,
                IsApproved = false,
                IsActive = true
            };

            _context.Banners.Add(entity);
            await _context.SaveChangesAsync();

            return _mapper.Map<BannerDto>(entity);
        }

        public async Task<BannerDto?> UpdateStatusAsync(int id, int companyId, UpdateBannerStatusDto dto)
        {
            var banner = await _context.Banners
                .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId);

            if (banner == null) return null;

            banner.IsApproved = dto.IsApproved;
            banner.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return _mapper.Map<BannerDto>(banner);
        }

        public async Task<bool> DeleteAsync(int id, int companyId)
        {
            var banner = await _context.Banners
                .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId);

            if (banner == null) return false;

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<BannerDto?> UpdateContentAsync(int id, int companyId, string htmlContent)
        {
            var banner = await _context.Banners
                .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId);

            if (banner == null) return null;

            banner.HtmlContent = htmlContent;
            await _context.SaveChangesAsync();

            return _mapper.Map<BannerDto>(banner);
        }
    }
}
