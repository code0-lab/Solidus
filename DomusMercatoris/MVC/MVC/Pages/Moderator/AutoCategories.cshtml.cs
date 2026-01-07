using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DomusMercatorisDotnetMVC.Pages.Moderator
{
    [Authorize(Roles = "Moderator,Rex")]
    public class AutoCategoriesModel : PageModel
    {
        private readonly DomusDbContext _db;

        public AutoCategoriesModel(DomusDbContext db)
        {
            _db = db;
        }

        public List<AutoCategory> AutoCategories { get; set; } = new();
        public List<ProductCluster> ProductClusters { get; set; } = new();

        public bool IsEditing { get; set; } = false;
        public int? EditId { get; set; } = null;

        [BindProperty]
        public CreateInput Input { get; set; } = new();

        public class CreateInput
        {
            [Required(ErrorMessage = "Name is required")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
            public string Name { get; set; } = string.Empty;

            public string? Description { get; set; }

            public List<int> ProductClusterIds { get; set; } = new();

            public int? ParentId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? editId)
        {
            await LoadDataAsync();

            if (editId.HasValue)
            {
                var item = await _db.AutoCategories
                    .Include(ac => ac.ProductClusters)
                    .FirstOrDefaultAsync(ac => ac.Id == editId.Value);

                if (item != null)
                {
                    IsEditing = true;
                    EditId = editId;
                    Input = new CreateInput
                    {
                        Name = item.Name,
                        Description = item.Description,
                        ProductClusterIds = item.ProductClusters.Select(pc => pc.Id).ToList(),
                        ParentId = item.ParentId
                    };
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? editId)
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                IsEditing = editId.HasValue;
                EditId = editId;
                return Page();
            }

            if (editId.HasValue)
            {
                // Update
                var item = await _db.AutoCategories
                    .Include(ac => ac.ProductClusters)
                    .FirstOrDefaultAsync(ac => ac.Id == editId.Value);

                if (item == null) return NotFound();

                // Prevent circular reference
                if (Input.ParentId.HasValue && Input.ParentId.Value == item.Id)
                {
                     ModelState.AddModelError("Input.ParentId", "A category cannot be its own parent.");
                     await LoadDataAsync();
                     IsEditing = true;
                     EditId = editId;
                     return Page();
                }

                item.Name = Input.Name;
                item.Description = Input.Description;
                item.ParentId = Input.ParentId;
                item.UpdatedAt = DateTime.UtcNow;

                // Update clusters
                item.ProductClusters.Clear();
                if (Input.ProductClusterIds.Any())
                {
                    var clusters = await _db.ProductClusters
                        .Where(pc => Input.ProductClusterIds.Contains(pc.Id))
                        .ToListAsync();
                    foreach (var c in clusters)
                    {
                        item.ProductClusters.Add(c);
                    }
                }

                _db.AutoCategories.Update(item);
            }
            else
            {
                // Create
                var newItem = new AutoCategory
                {
                    Name = Input.Name,
                    Description = Input.Description,
                    ParentId = Input.ParentId,
                    CreatedAt = DateTime.UtcNow
                };

                if (Input.ProductClusterIds.Any())
                {
                    var clusters = await _db.ProductClusters
                        .Where(pc => Input.ProductClusterIds.Contains(pc.Id))
                        .ToListAsync();
                    foreach (var c in clusters)
                    {
                        newItem.ProductClusters.Add(c);
                    }
                }

                _db.AutoCategories.Add(newItem);
            }

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var item = await _db.AutoCategories.FindAsync(id);
            if (item != null)
            {
                _db.AutoCategories.Remove(item);
                await _db.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            AutoCategories = await _db.AutoCategories
                .Include(ac => ac.ProductClusters)
                .OrderByDescending(ac => ac.CreatedAt)
                .ToListAsync();

            // Load clusters - prioritize latest version
            // Actually, we should probably allow selecting from any cluster if needed, 
            // but sticking to the latest version logic is fine for now unless specified otherwise.
            // Let's load all unique clusters grouped by name/version or just all of them.
            // For now, let's keep the logic of "latest version" or "all if no versions".
            // But maybe user wants to assign to older clusters too? 
            // The prompt implies "insufficient training data", so maybe we just want to see all clusters.
            // Let's just load ALL clusters for now to be safe, sorted by Name.
            
            ProductClusters = await _db.ProductClusters
                .OrderBy(c => c.Name)
                .ThenByDescending(c => c.Version)
                .ToListAsync();
        }
    }
}
