using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Enums;
using DomusMercatoris.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DomusMercatoris.Service.Services
{
    public class BlacklistService
    {
        private readonly DomusDbContext _context;

        public BlacklistService(DomusDbContext context)
        {
            _context = context;
        }

        public async Task<BlacklistStatus> GetStatusAsync(int companyId, long customerId)
        {
            var entry = await _context.CompanyCustomerBlacklists
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.CustomerId == customerId);

            return entry?.Status ?? BlacklistStatus.None;
        }

        public async Task SetStatusAsync(int companyId, long customerId, BlacklistStatus status)
        {
            var entry = await _context.CompanyCustomerBlacklists
                .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.CustomerId == customerId);

            if (entry == null)
            {
                if (status == BlacklistStatus.None) return; // Nothing to create

                entry = new CompanyCustomerBlacklist
                {
                    CompanyId = companyId,
                    CustomerId = customerId,
                    Status = status,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CompanyCustomerBlacklists.Add(entry);
            }
            else
            {
                if (status == BlacklistStatus.None)
                {
                    _context.CompanyCustomerBlacklists.Remove(entry);
                }
                else
                {
                    entry.Status = status;
                    entry.UpdatedAt = DateTime.UtcNow;
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task BlockByCompanyAsync(int companyId, long customerId)
        {
            var status = await GetStatusAsync(companyId, customerId);
            
            // Add CompanyBlockedCustomer flag
            var newStatus = status | BlacklistStatus.CompanyBlockedCustomer;
            
            if (newStatus == status) return; // Already blocked by company

            await SetStatusAsync(companyId, customerId, newStatus);
        }

        public async Task UnblockByCompanyAsync(int companyId, long customerId)
        {
            var status = await GetStatusAsync(companyId, customerId);

            // Remove CompanyBlockedCustomer flag
            var newStatus = status & ~BlacklistStatus.CompanyBlockedCustomer;

            if (newStatus == status) return; // Not blocked by company

            await SetStatusAsync(companyId, customerId, newStatus);
        }

        public async Task BlockByCustomerAsync(long customerId, int companyId)
        {
            var status = await GetStatusAsync(companyId, customerId);

            // Add CustomerBlockedCompany flag
            var newStatus = status | BlacklistStatus.CustomerBlockedCompany;

            if (newStatus == status) return; // Already blocked by customer

            await SetStatusAsync(companyId, customerId, newStatus);
        }

        public async Task UnblockByCustomerAsync(long customerId, int companyId)
        {
            var status = await GetStatusAsync(companyId, customerId);

            // Remove CustomerBlockedCompany flag
            var newStatus = status & ~BlacklistStatus.CustomerBlockedCompany;

            if (newStatus == status) return; // Not blocked by customer

            await SetStatusAsync(companyId, customerId, newStatus);
        }

        public async Task<bool> CanCustomerOrderAsync(long customerId, int companyId)
        {
            var status = await GetStatusAsync(companyId, customerId);
            // Customer cannot order if Company blocked them
            // Check if CompanyBlockedCustomer flag is set
            return !status.HasFlag(BlacklistStatus.CompanyBlockedCustomer);
        }

        public async Task<bool> CanCustomerViewProductsAsync(long customerId, int companyId)
        {
            var status = await GetStatusAsync(companyId, customerId);
            // Customer cannot view if Customer blocked company
            // Check if CustomerBlockedCompany flag is set
            return !status.HasFlag(BlacklistStatus.CustomerBlockedCompany);
        }

        public async Task<List<int>> GetCompaniesBlockedByCustomerAsync(long customerId)
        {
            // Bitwise check in EF Core: (Status & Flag) == Flag
            // OR (Status & Flag) != 0
            // Since Flag is 1, (Status & 1) == 1 covers 1 (1) and 3 (1|2)
            return await _context.CompanyCustomerBlacklists
                .AsNoTracking()
                .Where(b => b.CustomerId == customerId && (b.Status & BlacklistStatus.CustomerBlockedCompany) == BlacklistStatus.CustomerBlockedCompany)
                .Select(b => b.CompanyId)
                .ToListAsync();
        }

        public async Task<List<long>> GetCustomersBlockedByCompanyAsync(int companyId)
        {
            // (Status & 2) == 2 covers 2 (2) and 3 (1|2)
            return await _context.CompanyCustomerBlacklists
                .AsNoTracking()
                .Where(b => b.CompanyId == companyId && (b.Status & BlacklistStatus.CompanyBlockedCustomer) == BlacklistStatus.CompanyBlockedCustomer)
                .Select(b => b.CustomerId)
                .ToListAsync();
        }
    }
}
