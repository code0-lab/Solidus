using System;
using DomusMercatorisDotnetMVC.Dto.ProductDto;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.Services;
using DomusMercatoris.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Data;

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

        private bool ProductCategoriesExists()
        {
            try
            {
                var conn = _db.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) conn.Open();
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
        
        private int? GetRootCategoryId(int id, int companyId)
        {
            var cur = _db.Categories.SingleOrDefault(c => c.CompanyId == companyId && c.Id == id);
            if (cur == null) return null;
            while (cur.ParentId.HasValue)
            {
                var pid = cur.ParentId.Value;
                var parent = _db.Categories.SingleOrDefault(c => c.CompanyId == companyId && c.Id == pid);
                if (parent == null) break;
                cur = parent;
            }
            return cur.Id;
        }

        private int GetDepth(int id, int companyId)
        {
            var d = 0;
            var cur = _db.Categories.SingleOrDefault(c => c.CompanyId == companyId && c.Id == id);
            if (cur == null) return 0;
            while (cur.ParentId.HasValue)
            {
                d++;
                var pid = cur.ParentId.Value;
                cur = _db.Categories.SingleOrDefault(c => c.CompanyId == companyId && c.Id == pid);
                if (cur == null) break;
            }
            return d;
        }

        private (int? rootId, int? subId) MapCategoryFallback(List<int> ids, int companyId)
        {
            var valid = ids.Where(i => _db.Categories.Any(c => c.CompanyId == companyId && c.Id == i)).ToList();
            if (valid.Count == 0) return (null, null);
            var roots = valid.Select(i => GetRootCategoryId(i, companyId)).Where(r => r.HasValue).Select(r => r!.Value).ToList();
            if (roots.Count == 0) return (null, null);
            var rootId = roots.GroupBy(x => x).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
            var sameRoot = valid.Where(i => i != rootId && GetRootCategoryId(i, companyId) == rootId).ToList();
            int? subId = null;
            if (sameRoot.Count > 0)
            {
                subId = sameRoot.OrderByDescending(i => GetDepth(i, companyId)).First();
            }
            return (rootId, subId);
        }

        public async Task<Product> Create(ProductCreateDto dto)
        {
            int companyId = 0;
            var ctx = _httpContextAccessor.HttpContext;
            var compClaim = ctx?.User?.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(compClaim) && int.TryParse(compClaim, out var cid))
            {
                companyId = cid;
            }
            else
            {
                var idClaim = ctx?.User?.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
                {
                    var user = _db.Users.SingleOrDefault(u => u.Id == userId);
                    if (user != null) companyId = user.CompanyId;
                }
            }
            var savedPaths = new List<string>();
            var order = dto.ImageOrder ?? new List<string>();
            var baseDir = System.IO.Path.Combine(_env.WebRootPath, "uploads", "products", companyId.ToString());
            if (!System.IO.Directory.Exists(baseDir))
            {
                System.IO.Directory.CreateDirectory(baseDir);
            }
            if (order.Count > 0)
            {
                foreach (var token in order)
                {
                    if (string.IsNullOrWhiteSpace(token)) continue;
                    if (token.StartsWith("new:", StringComparison.OrdinalIgnoreCase))
                    {
                        var idxStr = token.Split(':')[1];
                        if (int.TryParse(idxStr, out var idx))
                        {
                            var f = (idx >= 0 && idx < (dto.ImageFiles?.Count ?? 0)) ? dto.ImageFiles?[idx] : null;
                            if (f != null && f.Length > 0)
                            {
                                var ext = System.IO.Path.GetExtension(f.FileName);
                                var fileName = Guid.NewGuid().ToString("N") + ext;
                                var fullPath = System.IO.Path.Combine(baseDir, fileName);
                                using (var fs = new System.IO.FileStream(fullPath, System.IO.FileMode.Create))
                                {
                                    await f.CopyToAsync(fs);
                                }
                                var relPath = $"/uploads/products/{companyId}/{fileName}";
                                savedPaths.Add(relPath);
                            }
                        }
                    }
                    if (savedPaths.Count >= 4) break;
                }
            }
            else
            {
                var files = (dto.ImageFiles ?? new List<IFormFile?>()).Where(f => f != null && f.Length > 0).ToList();
                foreach (var f in files)
                {
                    if (savedPaths.Count >= 4) break;
                    var ext = System.IO.Path.GetExtension(f!.FileName);
                    var fileName = Guid.NewGuid().ToString("N") + ext;
                    var fullPath = System.IO.Path.Combine(baseDir, fileName);
                    using (var fs = new System.IO.FileStream(fullPath, System.IO.FileMode.Create))
                    {
                        await f.CopyToAsync(fs);
                    }
                    var relPath = $"/uploads/products/{companyId}/{fileName}";
                    savedPaths.Add(relPath);
                }
            }

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
                var map = MapCategoryFallback(catIdsCreate, companyId);
                entity.CategoryId = map.rootId ?? entity.CategoryId;
                entity.SubCategoryId = map.subId ?? entity.SubCategoryId;
                if (ProductCategoriesExists())
                {
                    var cats = _db.Categories.Where(c => c.CompanyId == companyId && catIdsCreate.Contains(c.Id)).ToList();
                    entity.Categories = cats;
                }
            }
            _db.Products.Add(entity);
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

        public List<Product> GetByCompany(int companyId)
        {
            return _db.Products.Where(p => p.CompanyId == companyId).OrderByDescending(p => p.CreatedAt).ToList();
        }

        public Product? GetByIdInCompany(long id, int companyId)
        {
            return _db.Products
                .Include(p => p.AutoCategory)
                .SingleOrDefault(p => p.Id == id && p.CompanyId == companyId);
        }

        public Product? Update(long id, ProductUpdateDto dto)
        {
            int companyId = 0;
            var ctx = _httpContextAccessor.HttpContext;
            var compClaim = ctx?.User?.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(compClaim) && int.TryParse(compClaim, out var cid))
            {
                companyId = cid;
            }
            else
            {
                var idClaim = ctx?.User?.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
                {
                    var user = _db.Users.SingleOrDefault(u => u.Id == userId);
                    if (user != null) companyId = user.CompanyId;
                }
            }

            var entity = _db.Products.SingleOrDefault(p => p.Id == id && p.CompanyId == companyId);
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
                var map = MapCategoryFallback(ids, companyId);
                entity.CategoryId = map.rootId ?? entity.CategoryId;
                entity.SubCategoryId = map.subId ?? entity.SubCategoryId;
                if (ProductCategoriesExists())
                {
                    var cats = _db.Categories.Where(c => c.CompanyId == companyId && ids.Contains(c.Id)).ToList();
                    entity.Categories = cats;
                }
            }

            var removed = new HashSet<string>((dto.RemoveImages ?? new List<string>()).Where(p => !string.IsNullOrWhiteSpace(p)));
            var baseDir = System.IO.Path.Combine(_env.WebRootPath, "uploads", "products", companyId.ToString());
            if (!System.IO.Directory.Exists(baseDir))
            {
                System.IO.Directory.CreateDirectory(baseDir);
            }

            var ordered = new List<string>();
            var order = dto.ImageOrder ?? new List<string>();
            var existingSet = new HashSet<string>(entity.Images ?? new List<string>());

            if (order.Count > 0)
            {
                foreach (var token in order)
                {
                    if (string.IsNullOrWhiteSpace(token)) continue;
                    if (token.StartsWith("new:", StringComparison.OrdinalIgnoreCase))
                    {
                        var idxStr = token.Split(':')[1];
                        if (int.TryParse(idxStr, out var idx))
                        {
                            var f = (idx >= 0 && idx < (dto.ImageFiles?.Count ?? 0)) ? dto.ImageFiles?[idx] : null;
                            if (f != null && f.Length > 0)
                            {
                                var ext = System.IO.Path.GetExtension(f.FileName);
                                var fileName = Guid.NewGuid().ToString("N") + ext;
                                var fullPath = System.IO.Path.Combine(baseDir, fileName);
                                using (var fs = new System.IO.FileStream(fullPath, System.IO.FileMode.Create))
                                {
                                    f.CopyTo(fs);
                                }
                                var relPath = $"/uploads/products/{companyId}/{fileName}";
                                ordered.Add(relPath);
                            }
                        }
                    }
                    else
                    {
                        var path = token;
                        if (existingSet.Contains(path) && !removed.Contains(path))
                        {
                            ordered.Add(path);
                        }
                    }
                    if (ordered.Count >= 4) break;
                }
            }
            else
            {
                var existing = (entity.Images ?? new List<string>()).Where(p => !removed.Contains(p)).ToList();
                ordered.AddRange(existing);
                var files = (dto.ImageFiles ?? new List<IFormFile?>()).Where(f => f != null && f.Length > 0).ToList();
                foreach (var f in files)
                {
                    if (ordered.Count >= 4) break;
                    var ext = System.IO.Path.GetExtension(f!.FileName);
                    var fileName = Guid.NewGuid().ToString("N") + ext;
                    var fullPath = System.IO.Path.Combine(baseDir, fileName);
                    using (var fs = new System.IO.FileStream(fullPath, System.IO.FileMode.Create))
                    {
                        f.CopyTo(fs);
                    }
                    var relPath = $"/uploads/products/{companyId}/{fileName}";
                    ordered.Add(relPath);
                }
            }

            var newImages = ordered.Take(4).ToList();
            if (newImages.Count > 4)
            {
                throw new InvalidOperationException("Maximum 4 images allowed.");
            }

            entity.Images = newImages;

            _db.SaveChanges();
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
            q = q.ToLowerInvariant();
            return await _db.Products
                .Where(p => p.CompanyId == companyId && (
                    (p.Name ?? string.Empty).ToLower().Contains(q) ||
                    (p.Sku ?? string.Empty).ToLower().Contains(q) ||
                    (p.Description ?? string.Empty).ToLower().Contains(q)
                ))
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public bool Delete(long id)
        {
            int companyId = 0;
            var ctx = _httpContextAccessor.HttpContext;
            var compClaim = ctx?.User?.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(compClaim) && int.TryParse(compClaim, out var cid))
            {
                companyId = cid;
            }
            else
            {
                var idClaim = ctx?.User?.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
                {
                    var user = _db.Users.SingleOrDefault(u => u.Id == userId);
                    if (user != null) companyId = user.CompanyId;
                }
            }

            var entity = _db.Products.SingleOrDefault(p => p.Id == id && p.CompanyId == companyId);
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
            _db.SaveChanges();
            return true;
        }
    }
}
