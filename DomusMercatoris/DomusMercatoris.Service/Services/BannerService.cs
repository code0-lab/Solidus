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

        public async Task<List<BannerDto>> GetAllAsync(int companyId)
        {
            var list = await _context.Banners
                .Where(b => b.CompanyId == companyId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<BannerDto>>(list);
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
    }
}
