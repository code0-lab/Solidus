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

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,User")]
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
        public string TreeHtml { get; set; } = string.Empty;
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

        public IActionResult OnGet()
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(comp) || !int.TryParse(comp, out var companyId))
            {
                Console.WriteLine("[Categories.OnGet] missing company id, redirecting to Dashboard");
                return RedirectToPage("/Dashboard");
            }
            BuildTree(companyId);
            var editStr = Request.Query["editId"].ToString();
            if (!string.IsNullOrEmpty(editStr) && int.TryParse(editStr, out var eid))
            {
                var cat = _db.Categories.SingleOrDefault(c => c.CompanyId == companyId && c.Id == eid);
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
            Console.WriteLine($"[Categories.OnGet] compClaim={comp}, parsedCompanyId={companyId}, categoriesCount={Categories.Count}");
            return Page();
        }

        public IActionResult OnPost()
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            Console.WriteLine($"[Categories.OnPost] compClaim={comp}");
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
            Console.WriteLine($"[Categories.OnPost] resolvedCompanyId={companyId}");
            if (companyId == 0)
            {
                ModelState.AddModelError(string.Empty, "Authorization error.");
                Console.WriteLine("[Categories.OnPost] companyId=0 authorization error");
                return OnGet();
            }
            if (Request.HasFormContentType)
            {
                foreach (var key in Request.Form.Keys)
                {
                    var val = Request.Form[key].ToString();
                    Console.WriteLine($"[Categories.OnPost] form {key}='{val}'");
                }
            }
            Console.WriteLine($"[Categories.OnPost] bound Input.Name='{Input.Name}'");
            var parentRaw = Input.ParentId.HasValue ? Input.ParentId.Value.ToString() : "null";
            Console.WriteLine($"[Categories.OnPost] input Name='{Input.Name}', DescLen={(Input.Description ?? string.Empty).Length}, ParentId={parentRaw}");
            Console.WriteLine($"[Categories.OnPost] modelStateValid={ModelState.IsValid}, errorCount={ModelState.ErrorCount}");
            foreach (var kv in ModelState)
            {
                foreach (var err in kv.Value.Errors)
                {
                    Console.WriteLine($"[Categories.OnPost] modelError key={kv.Key} error={err.ErrorMessage}");
                }
            }
            if (!ModelState.IsValid)
            {
                BuildTree(companyId);
                Console.WriteLine($"[Categories.OnPost] invalid model, categoriesCount={Categories.Count}");
                return Page();
            }

            int? parentId = null;
            if (Input.ParentId.HasValue)
            {
                var exists = _db.Categories.Any(c => c.CompanyId == companyId && c.Id == Input.ParentId.Value);
                if (exists) parentId = Input.ParentId.Value;
                Console.WriteLine($"[Categories.OnPost] parentExists={exists}, parentId={(parentId.HasValue ? parentId.Value.ToString() : "null")}");
            }

            var entity = new Category
            {
                Name = Input.Name,
                Description = Input.Description,
                CompanyId = companyId,
                ParentId = parentId
            };
            Console.WriteLine($"[Categories.OnPost] saving CompanyId={companyId}, Name='{entity.Name}', ParentId={(entity.ParentId.HasValue ? entity.ParentId.Value.ToString() : "null")}");
            try
            {
                _db.Categories.Add(entity);
                _db.SaveChanges();
            }
            catch
            {
                Console.WriteLine("[Categories.OnPost] save failed");
                ModelState.AddModelError(string.Empty, "Save failed.");
                BuildTree(companyId);
                return Page();
            }
            TempData["Message"] = "Category created.";
            Console.WriteLine("[Categories.OnPost] save ok, redirect to /Categories");
            return RedirectToPage("/Categories");
        }

        public IActionResult OnPostUpdate()
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            int companyId = (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var cid)) ? cid : 0;
            var editStr = Request.Form["EditId"].ToString();
            if (companyId == 0 || string.IsNullOrEmpty(editStr) || !int.TryParse(editStr, out var eid))
            {
                return RedirectToPage("/Categories");
            }
            if (!ModelState.IsValid)
            {
                BuildTree(companyId);
                IsEditing = true;
                EditId = eid;
                return Page();
            }
            if (Input.ParentId.HasValue && Input.ParentId.Value == eid)
            {
                ModelState.AddModelError(string.Empty, "A category cannot be its own parent.");
                BuildTree(companyId);
                IsEditing = true;
                EditId = eid;
                return Page();
            }
            var entity = _db.Categories.SingleOrDefault(c => c.CompanyId == companyId && c.Id == eid);
            if (entity == null)
            {
                return RedirectToPage("/Categories");
            }
            int? parentId = null;
            if (Input.ParentId.HasValue)
            {
                var exists = _db.Categories.Any(c => c.CompanyId == companyId && c.Id == Input.ParentId.Value);
                if (exists) parentId = Input.ParentId.Value;
            }
            entity.Name = Input.Name;
            entity.Description = Input.Description;
            entity.ParentId = parentId;
            _db.SaveChanges();
            TempData["Message"] = "Category updated.";
            return RedirectToPage("/Categories");
        }

        public IActionResult OnPostDelete()
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            int companyId = (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var cid)) ? cid : 0;
            var editStr = Request.Form["EditId"].ToString();
            if (companyId == 0 || string.IsNullOrEmpty(editStr) || !int.TryParse(editStr, out var eid))
            {
                return RedirectToPage("/Categories");
            }
            var entity = _db.Categories.SingleOrDefault(c => c.CompanyId == companyId && c.Id == eid);
            if (entity == null)
            {
                return RedirectToPage("/Categories");
            }
            var hasChildren = _db.Categories.Any(c => c.CompanyId == companyId && c.ParentId == eid);
            if (hasChildren)
            {
                ModelState.AddModelError(string.Empty, "Cannot delete: move or delete subcategories first.");
                BuildTree(companyId);
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
            _db.SaveChanges();
            TempData["Message"] = "Category deleted.";
            return RedirectToPage("/Categories");
        }

        private void BuildTree(int companyId)
        {
            Categories = _db.Categories.Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.ParentId.HasValue)
                .ThenBy(c => c.Name)
                .ToList();
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
            try
            {
                var products = _db.Products
                    .Where(p => p.CompanyId == companyId)
                    .Include(p => p.Categories)
                    .ToList();
                foreach (var p in products)
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
            }
            catch
            {
                var products = _db.Products
                    .Where(p => p.CompanyId == companyId)
                    .ToList();
                foreach (var p in products)
                {
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
            TreeHtml = BuildTreeHtml(TreeRoots);
        }

        private string BuildTreeHtml(List<CategoryNode> roots)
        {
            var sb = new StringBuilder();
            sb.Append("<ul class=\"tree-root\">\n");
            foreach (var r in roots)
            {
                AppendNode(sb, r);
            }
            sb.Append("</ul>");
            return sb.ToString();
        }

        private void AppendNode(StringBuilder sb, CategoryNode node)
        {
            var name = WebUtility.HtmlEncode(node.Item.Name ?? string.Empty);
            sb.Append($"<li class=\"tree-li\" data-node-id=\"{node.Item.Id}\" data-parent-id=\"{(node.Item.ParentId.HasValue ? node.Item.ParentId.Value.ToString() : "0")}\">");
            sb.Append($"<div class=\"tree-node\" data-target=\"children-{node.Item.Id}\" data-has-children=\"{(node.Children.Count > 0 ? "true" : "false")}\">");
            sb.Append("<span class=\"node-text\">");
            sb.Append(name);
            var count = ProductsByCategory.TryGetValue(node.Item.Id, out var items) ? items.Count : 0;
            if (count > 0)
            {
                sb.Append($" <span class=\"badge bg-light text-dark\">{count}</span>");
            }
            sb.Append("</span>");
            sb.Append("<span class=\"node-actions\">");
            sb.Append($"<a class=\"btn btn-outline-secondary btn-sm\" href=\"/Category/{node.Item.Id}/Products\" title=\"View\"><i class=\"bi bi-eye\"></i></a>");
            sb.Append($"<a class=\"btn btn-dark rounded-0 border-2 btn-sm\" href=\"/Categories?editId={node.Item.Id}\" title=\"Edit\"><i class=\"bi bi-pencil\"></i></a>");
            sb.Append($"<button type=\"button\" class=\"btn btn-dark rounded-0 border-2 btn-sm btn-cat-delete\" data-id=\"{node.Item.Id}\" title=\"Delete\"><i class=\"bi bi-trash\"></i></button>");
            sb.Append("</span>");
            sb.Append("</div>");
            if (node.Children.Count > 0)
            {
                sb.Append($"<ul id=\"children-{node.Item.Id}\" class=\"tree-children\" hidden>");
                foreach (var ch in node.Children.OrderBy(n => n.Item.Name))
                {
                    AppendNode(sb, ch);
                }
                sb.Append("</ul>");
            }
            sb.Append("</li>");
        }
    }
}
