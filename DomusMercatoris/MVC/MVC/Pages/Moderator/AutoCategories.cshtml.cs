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
                    .AsNoTracking()
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

            try
            {
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
                    await UpdateProductClustersAsync(item, Input.ProductClusterIds);

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

                    await UpdateProductClustersAsync(newItem, Input.ProductClusterIds);

                    _db.AutoCategories.Add(newItem);
                }

                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log error (if logger available)
                ModelState.AddModelError("", "An error occurred while saving changes. Please try again.");
                await LoadDataAsync();
                IsEditing = editId.HasValue;
                EditId = editId;
                return Page();
            }

            return RedirectToPage();
        }

        private async Task UpdateProductClustersAsync(AutoCategory item, List<int> inputClusterIds)
        {
            item.ProductClusters ??= new List<ProductCluster>();

            // 1. Gelen ID listesini temizle
            var targetClusterIds = new HashSet<int>(
                inputClusterIds.Where(id => id > 0)
            );

            if (inputClusterIds.Contains(0))
            {
                targetClusterIds.Clear();
            }

            // 2. Silinecekleri belirle (Mevcutta var ama hedefte yok)
            // Not: ToList() ile kopyasını alıyoruz ki iterasyon sırasında koleksiyon değişmesin
            var toRemove = item.ProductClusters
                .Where(pc => !targetClusterIds.Contains(pc.Id))
                .ToList();

            foreach (var cluster in toRemove)
            {
                item.ProductClusters.Remove(cluster);
            }

            // 3. Eklenecekleri belirle (Hedefte var ama mevcutta yok)
            var existingIds = new HashSet<int>(item.ProductClusters.Select(pc => pc.Id));
            var idsToAdd = targetClusterIds
                .Where(id => !existingIds.Contains(id))
                .ToList();

            if (idsToAdd.Any())
            {
                var clustersToAdd = await _db.ProductClusters
                    .Where(pc => idsToAdd.Contains(pc.Id))
                    .ToListAsync();
                
                foreach (var c in clustersToAdd)
                {
                    item.ProductClusters.Add(c);
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var item = await _db.AutoCategories.FindAsync(id);
            if (item != null)
            {
                try 
                {
                    _db.AutoCategories.Remove(item);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Handle potential foreign key constraints
                    // For now, just redirect, or we could return with an error message in TempData
                    TempData["ErrorMessage"] = "Cannot delete category because it is in use.";
                }
            }
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            AutoCategories = await _db.AutoCategories
                .AsNoTracking()
                .Include(ac => ac.ProductClusters)
                .OrderByDescending(ac => ac.CreatedAt)
                .ToListAsync();

            ProductClusters = await _db.ProductClusters
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ThenByDescending(c => c.Version)
                .ToListAsync();
        }
    }
}
