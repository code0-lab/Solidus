using System.Collections.Generic;
using System.Linq;
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
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 1;

        public void OnGet()
        {
            var pageStr = Request.Query["page"].ToString();
            if (!string.IsNullOrEmpty(pageStr) && int.TryParse(pageStr, out var p))
            {
                PageNumber = Math.Max(1, p);
            }

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
                    var me = _db.Users.SingleOrDefault(u => u.Id == userId);
                    if (me != null) companyId = me.CompanyId;
                }
            }

            if (companyId > 0)
            {
                var query = _db.Orders
                    .Where(s => s.CompanyId == companyId && s.IsPaid);

                TotalCount = query.Count();
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
                if (PageNumber > TotalPages) PageNumber = TotalPages;

                var skip = (PageNumber - 1) * PageSize;

                Orders = query
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
                    .ToList();
            }
        }
    }
}
