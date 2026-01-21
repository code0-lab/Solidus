using System;
using DomusMercatorisDotnetMVC.Dto.ProductDto;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.Services;
using DomusMercatoris.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetMVC.Services
{
    public class ProductService
    {
        private readonly DomusDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;
        private readonly IClusteringService _clusteringService;

        public ProductService(DomusDbContext db, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, IClusteringService clusteringService)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _env = env;
            _clusteringService = clusteringService;
        }


        private async Task<(int? rootId, int? subId)> MapCategoryFallbackAsync(List<int> ids, int companyId)
        {
            // Fetch all categories for the company at once to avoid N+1 and recursive queries
            var allCats = await _db.Categories
                .Where(c => c.CompanyId == companyId)
                .Select(c => new { c.Id, c.ParentId })
                .AsNoTracking()
                .ToListAsync();

            var catDict = allCats.ToDictionary(c => c.Id, c => c.ParentId);

            var valid = ids.Where(i => catDict.ContainsKey(i)).ToList();
            if (valid.Count == 0) return (null, null);

            // Local helper to find root using dictionary
            int? GetRoot(int id)
            {
                if (!catDict.ContainsKey(id)) return null;
                var curr = id;
                var visited = new HashSet<int>();
                while (catDict[curr].HasValue)
                {
                    if (!visited.Add(curr)) break; // Prevent infinite loop
                    var pid = catDict[curr]!.Value;
                    if (!catDict.ContainsKey(pid)) break;
                    curr = pid;
                }
                return curr;
            }

            // Local helper to find depth using dictionary
            int GetDepthVal(int id)
            {
                if (!catDict.ContainsKey(id)) return 0;
                var d = 0;
                var curr = id;
                var visited = new HashSet<int>();
                while (catDict[curr].HasValue)
                {
                    if (!visited.Add(curr)) break; // Prevent infinite loop
                    d++;
                    var pid = catDict[curr]!.Value;
                    if (!catDict.ContainsKey(pid)) break;
                    curr = pid;
                }
                return d;
            }

            var roots = valid.Select(i => GetRoot(i)).Where(r => r.HasValue).Select(r => r!.Value).ToList();
            if (roots.Count == 0) return (null, null);
            
            var rootId = roots.GroupBy(x => x).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
            var sameRoot = valid.Where(i => i != rootId && GetRoot(i) == rootId).ToList();
            
            int? subId = null;
            if (sameRoot.Count > 0)
            {
                subId = sameRoot.OrderByDescending(i => GetDepthVal(i)).First();
            }
            return (rootId, subId);
        }

        private async Task<int> GetCurrentCompanyIdAsync()
        {
            var ctx = _httpContextAccessor.HttpContext;
            var compClaim = ctx?.User?.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(compClaim) && int.TryParse(compClaim, out var cid))
            {
                return cid;
            }
            
            var idClaim = ctx?.User?.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
            {
                var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId);
                if (user != null) return user.CompanyId;
            }
            return 0;
        }

        private async Task<string> SaveFileAsync(IFormFile file, string baseDir, int companyId)
        {
            var ext = System.IO.Path.GetExtension(file.FileName);
            var fileName = Guid.NewGuid().ToString("N") + ext;
            var fullPath = System.IO.Path.Combine(baseDir, fileName);
            using (var fs = new System.IO.FileStream(fullPath, System.IO.FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }
            return $"/uploads/products/{companyId}/{fileName}";
        }

        private async Task<List<string>> ProcessImagesAsync(
            int companyId,
            List<IFormFile?>? uploadedFiles,
            List<string>? imageOrder,
            List<string>? existingImages,
            List<string>? imagesToRemove)
        {
            var finalImages = new List<string>();
            var filesToSave = uploadedFiles ?? new List<IFormFile?>();
            var order = imageOrder ?? new List<string>();
            var existing = existingImages ?? new List<string>();
            var removed = new HashSet<string>((imagesToRemove ?? new List<string>()).Where(p => !string.IsNullOrWhiteSpace(p)));

            // Prepare directory
            var baseDir = System.IO.Path.Combine(_env.WebRootPath, "uploads", "products", companyId.ToString());
            if (!System.IO.Directory.Exists(baseDir))
            {
                System.IO.Directory.CreateDirectory(baseDir);
            }

            var currentSet = new HashSet<string>(existing);

            if (order.Count > 0)
            {
                foreach (var token in order)
                {
                    if (string.IsNullOrWhiteSpace(token)) continue;

                    if (token.StartsWith("new:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = token.Split(':');
                        if (parts.Length > 1 && int.TryParse(parts[1], out var idx))
                        {
                            var f = (idx >= 0 && idx < filesToSave.Count) ? filesToSave[idx] : null;
                            if (f != null && f.Length > 0)
                            {
                                var path = await SaveFileAsync(f, baseDir, companyId);
                                finalImages.Add(path);
                            }
                        }
                    }
                    else
                    {
                        if (currentSet.Contains(token) && !removed.Contains(token))
                        {
                            finalImages.Add(token);
                        }
                    }
                    if (finalImages.Count >= 4) break;
                }
            }
            else
            {
                var keptExisting = existing.Where(p => !removed.Contains(p)).ToList();
                finalImages.AddRange(keptExisting);

                var validNewFiles = filesToSave.Where(f => f != null && f.Length > 0).ToList();
                foreach (var f in validNewFiles)
                {
                    if (finalImages.Count >= 4) break;
                    var path = await SaveFileAsync(f!, baseDir, companyId);
                    finalImages.Add(path);
                }
            }

            return finalImages.Take(4).ToList();
        }

        public async Task<Product> CreateAsync(ProductCreateDto dto)
        {
            int companyId = await GetCurrentCompanyIdAsync();
            
            var savedPaths = await ProcessImagesAsync(companyId, dto.ImageFiles, dto.ImageOrder, null, null);

            var entity = new Product
            {
                Name = dto.Name,
                Sku = dto.Sku,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                SubCategoryId = dto.SubCategoryId,
                BrandId = dto.BrandId,
                Price = dto.Price,
                AutoCategoryId = dto.AutoCategoryId,
                CompanyId = companyId,
                Quantity = dto.Quantity,
                Images = savedPaths,
                CreatedAt = DateTime.UtcNow
            };
            var catIdsCreate = (dto.CategoryIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
            if (catIdsCreate.Count > 0)
            {
                var map = await MapCategoryFallbackAsync(catIdsCreate, companyId);
                entity.CategoryId = map.rootId ?? entity.CategoryId;
                entity.SubCategoryId = map.subId ?? entity.SubCategoryId;
                var cats = await _db.Categories.Where(c => c.CompanyId == companyId && catIdsCreate.Contains(c.Id)).ToListAsync();
                entity.Categories = cats;
            }
            await _db.Products.AddAsync(entity);
            await _db.SaveChangesAsync();
            
            // Extract features for clustering
            if (savedPaths.Any())
            {
                // We should run this asynchronously or await it. 
                // Since this might be slow, await ensures consistency but slows response.
                // Given "Her Ürün Eklendiğinde Çalışmalı", await is safer.
                await _clusteringService.ExtractAndStoreFeaturesAsync(entity.Id, savedPaths);
            }
            
            return entity;
        }

        public async Task<List<Product>> GetByCompanyAsync(int companyId)
        {
            return await _db.Products
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdInCompanyAsync(long id, int companyId)
        {
            return await _db.Products
                .AsNoTracking()
                .Include(p => p.AutoCategory)
                .SingleOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
        }

        public async Task<Product?> UpdateAsync(long id, ProductUpdateDto dto)
        {
            int companyId = await GetCurrentCompanyIdAsync();

            var entity = await _db.Products
                .Include(p => p.Categories)
                .SingleOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
            if (entity == null)
            {
                return null;
            }

            entity.Name = dto.Name;
            entity.Sku = dto.Sku;
            entity.Description = dto.Description;
            entity.CategoryId = dto.CategoryId;
            entity.SubCategoryId = dto.SubCategoryId;
            entity.BrandId = dto.BrandId;
            entity.AutoCategoryId = dto.AutoCategoryId;
            entity.Price = dto.Price;
            entity.Quantity = dto.Quantity;
            var ids = (dto.CategoryIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
            if (ids.Count > 0)
            {
                var map = await MapCategoryFallbackAsync(ids, companyId);
                entity.CategoryId = map.rootId ?? entity.CategoryId;
                entity.SubCategoryId = map.subId ?? entity.SubCategoryId;
                var cats = await _db.Categories.Where(c => c.CompanyId == companyId && ids.Contains(c.Id)).ToListAsync();
                
                entity.Categories.Clear();
                foreach (var c in cats)
                {
                    entity.Categories.Add(c);
                }
            }

            entity.Images = await ProcessImagesAsync(companyId, dto.ImageFiles, dto.ImageOrder, entity.Images, dto.RemoveImages);

            await _db.SaveChangesAsync();

            // Extract features for clustering if images have changed
            if (entity.Images.Any())
            {
                await _clusteringService.ExtractAndStoreFeaturesAsync(entity.Id, entity.Images);
            }

            return entity;
        }

        public async Task<int> CountByCompanyAsync(int companyId)
        {
            return await _db.Products.CountAsync(p => p.CompanyId == companyId);
        }

        public async Task<List<Product>> GetByCompanyPageAsync(int companyId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 9;
            var skip = (page - 1) * pageSize;
            return await _db.Products
                .AsNoTracking()
                .Include(p => p.Variants)
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Product>> SearchByCompanyAsync(int companyId, string query, int limit = 20)
        {
            var q = (query ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(q)) return new List<Product>();
            
            // Remove ToLower() for SARGable query (assuming CI collation)
            return await _db.Products
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId && (
                    p.Name.Contains(q) ||
                    p.Sku.Contains(q) ||
                    p.Description.Contains(q)
                ))
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(long id)
        {
            int companyId = await GetCurrentCompanyIdAsync();

            var entity = await _db.Products.SingleOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
            if (entity == null) return false;

            var files = entity.Images ?? new List<string>();
            foreach (var rel in files)
            {
                try
                {
                    var path = System.IO.Path.Combine(_env.WebRootPath ?? string.Empty, rel.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                catch { }
            }

            _db.Products.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
