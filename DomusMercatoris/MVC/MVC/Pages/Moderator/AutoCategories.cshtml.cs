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

using DomusMercatoris.Core.Exceptions;

using System.Net.Http;

namespace DomusMercatorisDotnetMVC.Pages.Moderator
{
    [Authorize(Roles = "Moderator,Rex")]
    public class AutoCategoriesModel : PageModel
    {
        private readonly DomusDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AutoCategoriesModel(DomusDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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

            if (editId.HasValue)
            {
                // Update
                var item = await _db.AutoCategories
                    .Include(ac => ac.ProductClusters)
                    .FirstOrDefaultAsync(ac => ac.Id == editId.Value);

                if (item == null) throw new NotFoundException($"AutoCategory {editId} not found.");

                // 1. Validation: Duplicate Name Check
                if (Input.Name != item.Name)
                {
                    var isDuplicate = await _db.AutoCategories
                        .AnyAsync(ac => ac.Name == Input.Name && ac.Id != item.Id);

                    if (isDuplicate)
                    {
                        ModelState.AddModelError("Input.Name", "A category with this name already exists.");
                        await LoadDataAsync();
                        IsEditing = true;
                        EditId = editId;
                        return Page();
                    }
                }

                // 2. Validation: Prevent circular reference (Direct & Indirect)
                if (Input.ParentId != item.ParentId)
                {
                    if (Input.ParentId.HasValue)
                    {
                        if (Input.ParentId.Value == item.Id)
                        {
                            ModelState.AddModelError("Input.ParentId", "A category cannot be its own parent.");
                            await LoadDataAsync();
                            IsEditing = true;
                            EditId = editId;
                            return Page();
                        }
                        
                        if (await IsCircularReferenceAsync(item.Id, Input.ParentId.Value))
                        {
                            ModelState.AddModelError("Input.ParentId", "Circular reference detected. A category cannot be a child of its own descendant.");
                            await LoadDataAsync();
                            IsEditing = true;
                            EditId = editId;
                            return Page();
                        }
                    }
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
                // 1. Validation: Duplicate Name Check
                var isDuplicate = await _db.AutoCategories
                    .AnyAsync(ac => ac.Name == Input.Name);

                if (isDuplicate)
                {
                    ModelState.AddModelError("Input.Name", "A category with this name already exists.");
                    await LoadDataAsync();
                    return Page();
                }

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

            // Only invalidate cache if data actually changed in DB
            var changes = await _db.SaveChangesAsync();

            if (changes > 0)
            {
                // Invalidate API Cache
                await InvalidateApiCacheAsync();
            }

            return RedirectToPage();
        }

        private async Task<bool> IsCircularReferenceAsync(int categoryId, int newParentId)
        {
            var currentParentId = newParentId;
            while (true)
            {
                if (currentParentId == categoryId) return true;

                var parent = await _db.AutoCategories
                    .AsNoTracking()
                    .Select(ac => new { ac.Id, ac.ParentId })
                    .FirstOrDefaultAsync(ac => ac.Id == currentParentId);

                if (parent == null || parent.ParentId == null) return false;
                currentParentId = parent.ParentId.Value;
            }
        }

        private async Task InvalidateApiCacheAsync()
        {
            try
            {
                var apiUrl = _configuration["ApiUrl"] ?? "http://localhost:5000";
                var client = _httpClientFactory.CreateClient();
                // Assuming the API is on the same network/host. 
                // For production, you might need an API Key or specific configuration.
                await client.PostAsync($"{apiUrl}/api/categories/auto/invalidate", null);
            }
            catch (Exception)
            {
                // Log error but don't fail the request. Cache will be stale until expiration.
            }
        }

        private async Task UpdateProductClustersAsync(AutoCategory item, List<int> inputClusterIds)
        {
            item.ProductClusters ??= new List<ProductCluster>();

            // 1. Gelen ID listesini temizle
            // Not: Veri sayısı az olduğu için (max ~200), LINQ okunabilirliği mikro-optimizasyona tercih edilmiştir.
            var targetClusterIds = new HashSet<int>(inputClusterIds.Where(id => id > 0));

            if (inputClusterIds.Contains(0))
            {
                targetClusterIds.Clear();
            }

            // 2. Silinecekleri belirle (Smart Update: Mevcutta var ama hedefte yok)
            var toRemove = item.ProductClusters
                .Where(pc => !targetClusterIds.Contains(pc.Id))
                .ToList();

            foreach (var cluster in toRemove)
            {
                item.ProductClusters.Remove(cluster);
            }

            // 3. Eklenecekleri belirle (Smart Update: Hedefte var ama mevcutta yok)
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
                _db.AutoCategories.Remove(item);
                await _db.SaveChangesAsync();
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
