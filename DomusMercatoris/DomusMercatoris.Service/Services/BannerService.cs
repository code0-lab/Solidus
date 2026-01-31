using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatoris.Service.Services
{
    public class BannerService
    {
        private readonly DomusDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public BannerService(DomusDbContext context, IMapper mapper, ICurrentUserService currentUserService)
        {
            _context = context;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<BannerDto?> GetActiveBannerAsync(int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                return null;
            }

            var banner = await _context.Banners
                .AsNoTracking()
                .Where(b => b.CompanyId == companyId && b.IsApproved && b.IsActive)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            return _mapper.Map<BannerDto>(banner);
        }

        public async Task<BannerDto?> GetAnyActiveBannerAsync()
        {
            if (_currentUserService.CompanyId.HasValue)
            {
                return await GetActiveBannerAsync(_currentUserService.CompanyId.Value);
            }

            var banner = await _context.Banners
                .AsNoTracking()
                .Where(b => b.IsApproved && b.IsActive)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            return _mapper.Map<BannerDto>(banner);
        }

        public async Task<BannerDto?> GetByIdAsync(int id, int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                return null;
            }

            var banner = await _context.Banners
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId);

            return _mapper.Map<BannerDto>(banner);
        }

        public async Task<List<BannerDto>> GetAllAsync(int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                return new List<BannerDto>();
            }

            var list = await _context.Banners
                .AsNoTracking()
                .Where(b => b.CompanyId == companyId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<BannerDto>>(list);
        }

        public async Task<List<BannerSummaryDto>> GetSummariesAsync(int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                return new List<BannerSummaryDto>();
            }

            return await _context.Banners
                .AsNoTracking()
                .Where(b => b.CompanyId == companyId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BannerSummaryDto
                {
                    Id = b.Id,
                    CompanyId = b.CompanyId,
                    Topic = b.Topic,
                    IsApproved = b.IsApproved,
                    IsActive = b.IsActive
                })
                .ToListAsync();
        }

        public async Task<BannerDto> CreateAsync(CreateBannerDto dto)
        {
            if (_currentUserService.CompanyId.HasValue && dto.CompanyId != _currentUserService.CompanyId.Value)
            {
                throw new UnauthorizedAccessException("Cannot create banner for another company.");
            }

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
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                return null;
            }

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
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                return false;
            }

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
