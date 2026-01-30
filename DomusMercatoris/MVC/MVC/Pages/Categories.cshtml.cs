using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using DomusMercatoris.Core.Constants;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Policy = "CategoriesAccess")]
    public class CategoriesModel : PageModel
    {
        private readonly DomusDbContext _db;

        public CategoriesModel(DomusDbContext db)
        {
            _db = db;
        }

        public List<Category> Categories { get; set; } = new();
        public class CategoryNode
        {
            public Category Item { get; set; } = new Category();
            public List<CategoryNode> Children { get; set; } = new List<CategoryNode>();
        }
        public List<CategoryNode> TreeRoots { get; set; } = new();
        public Dictionary<int, List<Product>> ProductsByCategory { get; set; } = new();
        public bool IsEditing { get; set; } = false;
        public int? EditId { get; set; } = null;

        public class CreateInput
        {
            [Required]
            [StringLength(100, MinimumLength = 2)]
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public int? ParentId { get; set; }
        }

        [BindProperty]
        public CreateInput Input { get; set; } = new();

        private async Task<int> GetCompanyIdAsync()
        {
            var comp = User.FindFirst(AppConstants.CustomClaimTypes.CompanyId)?.Value;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var cid))
            {
                return cid;
            }
            
            var idClaim = User.FindFirst(AppConstants.CustomClaimTypes.UserId)?.Value;
            if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
            {
                var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);
                if (user != null) return user.CompanyId ?? 0;
            }
            return 0;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var companyId = await GetCompanyIdAsync();
            if (companyId == 0)
            {
                return RedirectToPage("/Dashboard");
            }
            await BuildTreeAsync(companyId);
            var editStr = Request.Query["editId"].ToString();
            if (!string.IsNullOrEmpty(editStr) && int.TryParse(editStr, out var eid))
            {
                var cat = await _db.Categories.SingleOrDefaultAsync(c => c.CompanyId == companyId && c.Id == eid);
                if (cat != null)
                {
                    IsEditing = true;
                    EditId = eid;
                    Input = new CreateInput
                    {
                        Name = cat.Name ?? string.Empty,
                        Description = cat.Description,
                        ParentId = cat.ParentId
                    };
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var companyId = await GetCompanyIdAsync();
            if (companyId == 0)
            {
                ModelState.AddModelError(string.Empty, "Authorization error.");
                return await OnGetAsync();
            }

            if (!ModelState.IsValid)
            {
                await BuildTreeAsync(companyId);
                return Page();
            }

            int? parentId = null;
            if (Input.ParentId.HasValue)
            {
                var exists = await _db.Categories.AnyAsync(c => c.CompanyId == companyId && c.Id == Input.ParentId.Value);
                if (exists) parentId = Input.ParentId.Value;
            }

            var entity = new Category
            {
                Name = Input.Name,
                Description = Input.Description,
                CompanyId = companyId,
                ParentId = parentId
            };

            _db.Categories.Add(entity);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Category created.";
            return RedirectToPage("/Categories");
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var companyId = await GetCompanyIdAsync();
            var editStr = Request.Form["EditId"].ToString();
            if (companyId == 0 || string.IsNullOrEmpty(editStr) || !int.TryParse(editStr, out var eid))
            {
                return RedirectToPage("/Categories");
            }
            if (!ModelState.IsValid)
            {
                await BuildTreeAsync(companyId);
                IsEditing = true;
                EditId = eid;
                return Page();
            }
            if (Input.ParentId.HasValue && Input.ParentId.Value == eid)
            {
                ModelState.AddModelError(string.Empty, "A category cannot be its own parent.");
                await BuildTreeAsync(companyId);
                IsEditing = true;
                EditId = eid;
                return Page();
            }
            var entity = await _db.Categories.SingleOrDefaultAsync(c => c.CompanyId == companyId && c.Id == eid);
            if (entity == null)
            {
                return RedirectToPage("/Categories");
            }
            int? parentId = null;
            if (Input.ParentId.HasValue)
            {
                var exists = await _db.Categories.AnyAsync(c => c.CompanyId == companyId && c.Id == Input.ParentId.Value);
                if (exists) parentId = Input.ParentId.Value;
            }
            entity.Name = Input.Name;
            entity.Description = Input.Description;
            entity.ParentId = parentId;
            
            await _db.SaveChangesAsync();
            TempData["Message"] = "Category updated.";
            return RedirectToPage("/Categories");
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var companyId = await GetCompanyIdAsync();
            var editStr = Request.Form["EditId"].ToString();
            if (companyId == 0 || string.IsNullOrEmpty(editStr) || !int.TryParse(editStr, out var eid))
            {
                return RedirectToPage("/Categories");
            }
            var entity = await _db.Categories.SingleOrDefaultAsync(c => c.CompanyId == companyId && c.Id == eid);
            if (entity == null)
            {
                return RedirectToPage("/Categories");
            }
            var hasChildren = await _db.Categories.AnyAsync(c => c.CompanyId == companyId && c.ParentId == eid);
            if (hasChildren)
            {
                ModelState.AddModelError(string.Empty, "Cannot delete: move or delete subcategories first.");
                await BuildTreeAsync(companyId);
                IsEditing = true;
                EditId = eid;
                Input = new CreateInput
                {
                    Name = entity.Name ?? string.Empty,
                    Description = entity.Description,
                    ParentId = entity.ParentId
                };
                return Page();
            }
            _db.Categories.Remove(entity);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Category deleted.";
            return RedirectToPage("/Categories");
        }

        private async Task BuildTreeAsync(int companyId)
        {
            Categories = await _db.Categories
                .Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.ParentId.HasValue)
                .ThenBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();
            
            var dict = Categories.ToDictionary(c => c.Id, c => new CategoryNode { Item = c });
            foreach (var c in Categories)
            {
                if (c.ParentId.HasValue && dict.TryGetValue(c.ParentId.Value, out var parent))
                {
                    parent.Children.Add(dict[c.Id]);
                }
            }
            TreeRoots = dict.Values.Where(n => !n.Item.ParentId.HasValue).OrderBy(n => n.Item.Name).ToList();
            ProductsByCategory = new Dictionary<int, List<Product>>();
            
            var products = await _db.Products
                .Where(p => p.CompanyId == companyId)
                .Include(p => p.Categories)
                .AsNoTracking()
                .ToListAsync();

            foreach (var p in products)
            {
                if (p.Categories != null && p.Categories.Any(c => c.CompanyId == companyId))
                {
                    foreach (var cat in p.Categories.Where(cat => cat.CompanyId == companyId))
                    {
                        if (!ProductsByCategory.TryGetValue(cat.Id, out var list))
                        {
                            list = new List<Product>();
                            ProductsByCategory[cat.Id] = list;
                        }
                        list.Add(p);
                    }
                }
                else
                {
                    // Fallback to legacy CategoryId/SubCategoryId if no Many-to-Many relation found
                    if (p.CategoryId.HasValue)
                    {
                        var cid = p.CategoryId.Value;
                        if (!ProductsByCategory.TryGetValue(cid, out var list))
                        {
                            list = new List<Product>();
                            ProductsByCategory[cid] = list;
                        }
                        list.Add(p);
                    }
                    if (p.SubCategoryId.HasValue)
                    {
                        var cid = p.SubCategoryId.Value;
                        if (!ProductsByCategory.TryGetValue(cid, out var list))
                        {
                            list = new List<Product>();
                            ProductsByCategory[cid] = list;
                        }
                        list.Add(p);
                    }
                }
            }
        }
    }
}
