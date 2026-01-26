using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,User")]
    public class CategoryProductsModel : PageModel
    {
        private readonly DomusDbContext _db;
        public CategoryProductsModel(DomusDbContext db)
        {
            _db = db;
        }

        public List<Product> Items { get; set; } = new();
        public string CategoryName { get; set; } = string.Empty;
        public int CategoryId { get; set; } = 0;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 1;

        private async Task<bool> ProductCategoriesExistsAsync()
        {
            try
            {
                var conn = _db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ProductCategories'";
                var obj = await cmd.ExecuteScalarAsync();
                return obj != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IActionResult> OnGetAsync(int id)
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
                    var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);
                    if (user != null) companyId = user.CompanyId;
                }
            }
            var cat = await _db.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.CompanyId == companyId && c.Id == id);
            if (cat == null)
            {
                return RedirectToPage("/Categories");
            }
            CategoryId = id;
            CategoryName = cat.Name ?? string.Empty;
            var pageStr = Request.Query["page"].ToString();
            if (!string.IsNullOrEmpty(pageStr) && int.TryParse(pageStr, out var p))
            {
                PageNumber = Math.Max(1, p);
            }
            if (await ProductCategoriesExistsAsync())
            {
                var baseQuery = _db.Products
                    .AsNoTracking()
                    .Where(p => p.CompanyId == companyId)
                    .Include(p => p.Categories)
                    .Where(p => p.CategoryId == id || p.SubCategoryId == id || p.Categories.Any(c => c.Id == id));
                TotalCount = await baseQuery.CountAsync();
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
                if (PageNumber > TotalPages) PageNumber = TotalPages;
                var skip = (PageNumber - 1) * PageSize;
                Items = await baseQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip(skip)
                    .Take(PageSize)
                    .ToListAsync();
            }
            else
            {
                var baseQuery = _db.Products
                    .AsNoTracking()
                    .Where(p => p.CompanyId == companyId)
                    .Where(p => p.CategoryId == id || p.SubCategoryId == id);
                TotalCount = await baseQuery.CountAsync();
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
                if (PageNumber > TotalPages) PageNumber = TotalPages;
                var skip = (PageNumber - 1) * PageSize;
                Items = await baseQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip(skip)
                    .Take(PageSize)
                    .ToListAsync();
            }
            return Page();
        }
    }
}
