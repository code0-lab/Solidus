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

        private bool ProductCategoriesExists()
        {
            try
            {
                var conn = _db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ProductCategories'";
                var obj = cmd.ExecuteScalar();
                return obj != null;
            }
            catch
            {
                return false;
            }
        }

        public IActionResult OnGet(int id)
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
                    var user = _db.Users.SingleOrDefault(u => u.Id == userId);
                    if (user != null) companyId = user.CompanyId;
                }
            }
            var cat = _db.Categories.SingleOrDefault(c => c.CompanyId == companyId && c.Id == id);
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
            if (ProductCategoriesExists())
            {
                var baseQuery = _db.Products
                    .Where(p => p.CompanyId == companyId)
                    .Include(p => p.Categories)
                    .Where(p => p.CategoryId == id || p.SubCategoryId == id || p.Categories.Any(c => c.Id == id));
                TotalCount = baseQuery.Count();
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
                if (PageNumber > TotalPages) PageNumber = TotalPages;
                var skip = (PageNumber - 1) * PageSize;
                Items = baseQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip(skip)
                    .Take(PageSize)
                    .ToList();
            }
            else
            {
                var baseQuery = _db.Products
                    .Where(p => p.CompanyId == companyId)
                    .Where(p => p.CategoryId == id || p.SubCategoryId == id);
                TotalCount = baseQuery.Count();
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
                if (PageNumber > TotalPages) PageNumber = TotalPages;
                var skip = (PageNumber - 1) * PageSize;
                Items = baseQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip(skip)
                    .Take(PageSize)
                    .ToList();
            }
            return Page();
        }
    }
}
