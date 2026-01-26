using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using System;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,User")]
    public class OrdersModel : PageModel
    {
        private readonly DomusDbContext _db;

        public OrdersModel(DomusDbContext db)
        {
            _db = db;
        }

        public List<Order> Orders { get; set; } = new List<Order>();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 9;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 1;

        public async Task OnGetAsync()
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            int companyId = 0;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var cid))
            {
                companyId = cid;
            }
            else
            {
                var idClaim = User.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
                {
                    var me = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId);
                    if (me != null) companyId = me.CompanyId;
                }
            }

            if (companyId > 0)
            {
                var query = _db.Orders
                    .AsNoTracking()
                    .Where(s => s.CompanyId == companyId && s.IsPaid);

                TotalCount = await query.CountAsync();
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
                
                if (PageNumber < 1) PageNumber = 1;
                if (PageNumber > TotalPages) PageNumber = TotalPages;

                var skip = (PageNumber - 1) * PageSize;

                Orders = await query
                    .Include(s => s.User)
                    .Include(s => s.FleetingUser)
                    .Include(s => s.OrderItems)
                        .ThenInclude(sp => sp.Product)
                    .Include(s => s.OrderItems)
                        .ThenInclude(sp => sp.VariantProduct)
                    .Include(s => s.CargoTracking)
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip(skip)
                    .Take(PageSize)
                    .ToListAsync();
            }
        }
    }
}
