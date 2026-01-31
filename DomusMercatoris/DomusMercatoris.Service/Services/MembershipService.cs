using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DomusMercatoris.Service.Services
{
    public class MembershipService
    {
        private readonly DomusDbContext _context;
        private readonly DomusMercatoris.Service.Interfaces.ICurrentUserService _currentUserService;

        public MembershipService(DomusDbContext context, DomusMercatoris.Service.Interfaces.ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<MembershipDto>> GetUserMembershipsAsync(long userId)
        {
            var query = _context.UserCompanyMemberships
                .Include(m => m.Company)
                .Where(m => m.UserId == userId);

            if (_currentUserService.CompanyId.HasValue)
            {
                query = query.Where(m => m.CompanyId == _currentUserService.CompanyId.Value);
            }

            return await query
                .Select(m => new MembershipDto
                {
                    Id = m.Id,
                    CompanyId = m.CompanyId,
                    CompanyName = m.Company.Name,
                    JoinedAt = m.JoinedAt
                })
                .ToListAsync();
        }

        public async Task<bool> JoinCompanyAsync(long userId, int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && companyId != _currentUserService.CompanyId.Value)
            {
                return false;
            }

            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
                return false;

            var exists = await _context.UserCompanyMemberships
                .AnyAsync(m => m.UserId == userId && m.CompanyId == companyId);

            if (exists)
                return true; // Already member

            var membership = new UserCompanyMembership
            {
                UserId = userId,
                CompanyId = companyId,
                JoinedAt = DateTime.UtcNow
            };

            _context.UserCompanyMemberships.Add(membership);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LeaveCompanyAsync(long userId, int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && companyId != _currentUserService.CompanyId.Value)
            {
                return false;
            }

            var membership = await _context.UserCompanyMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.CompanyId == companyId);

            if (membership == null)
                return false;

            _context.UserCompanyMemberships.Remove(membership);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CompanySummaryDto>> SearchCompaniesAsync(long userId, string query)
        {
            var queryable = _context.Companies
                .Where(c => c.IsActive && !c.IsBaned)
                .AsQueryable();

            if (_currentUserService.CompanyId.HasValue)
            {
                queryable = queryable.Where(c => c.CompanyId == _currentUserService.CompanyId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                queryable = queryable.Where(c => c.Name.Contains(query));
            }

            var companies = await queryable
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.CompanyId,
                    c.Name
                })
                .Take(20)
                .ToListAsync();

            var myCompanyIds = await _context.UserCompanyMemberships
                .Where(m => m.UserId == userId)
                .Select(m => m.CompanyId)
                .ToListAsync();
            
            var myCompanyIdsSet = new HashSet<int>(myCompanyIds);

            return companies.Select(c => new CompanySummaryDto
            {
                Id = c.CompanyId,
                Name = c.Name,
                IsMember = myCompanyIdsSet.Contains(c.CompanyId)
            }).ToList();
        }
    }
}
