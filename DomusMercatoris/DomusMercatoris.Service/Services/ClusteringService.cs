using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http; // For IFormFile
using DomusMercatoris.Service.Interfaces; // Ensure Interface is here or updated

namespace DomusMercatoris.Service.Services
{
    public class ClusteringService : IClusteringService
    {
        private readonly DomusDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _pythonApiUrl;
        private readonly string _webRootPath;

        // Using IConfiguration to get WebRootPath if needed, or passing it explicitly
        public ClusteringService(DomusDbContext context, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
            _pythonApiUrl = configuration.GetValue<string>("PythonApiUrl") ?? "http://localhost:5001";
            
            var configWebRoot = configuration.GetValue<string>("WebRootPath");
            if (!string.IsNullOrEmpty(configWebRoot))
            {
                // Ensure we have an absolute path
                _webRootPath = Path.IsPathRooted(configWebRoot) 
                    ? configWebRoot 
                    : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configWebRoot));
            }
            else
            {
                _webRootPath = Directory.GetCurrentDirectory();
            }
        }

        public async Task ProcessAllProductsFeaturesAsync()
        {
            var productsToProcess = await _context.Products
                .Where(p => !_context.ProductFeatures.Any(f => f.ProductId == p.Id))
                .ToListAsync();

            foreach (var p in productsToProcess)
            {
                if (p.Images != null && p.Images.Any())
                {
                    await ExtractAndStoreFeaturesAsync(p.Id, p.Images);
                }
            }
        }

        public async Task ExtractAndStoreFeaturesAsync(long productId, List<string> imagePaths)
        {
            if (imagePaths == null || !imagePaths.Any()) return;

            using var content = new MultipartFormDataContent();
            
            foreach (var p in imagePaths)
            {
                var cleanPath = p.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                // Attempt to resolve path. If _webRootPath is not set correctly, this might fail.
                // Best practice: Store absolute paths or configured base path.
                var fullPath = Path.Combine(_webRootPath, "wwwroot", cleanPath); 
                // Adjusting: typically WebRootPath points to wwwroot.
                // If _webRootPath is "/App", then "/App/uploads/..."
                
                // Correction: If the service runs in MVC, Directory.GetCurrentDirectory() might be the project root.
                // We should rely on configuration for the static files path if possible.
                
                // Fallback check
                if (!File.Exists(fullPath)) 
                {
                     // Try without wwwroot if the path already includes it or mapped differently
                     fullPath = Path.Combine(_webRootPath, cleanPath);
                }

                if (File.Exists(fullPath))
                {
                    var fileBytes = await File.ReadAllBytesAsync(fullPath);
                    var imageContent = new ByteArrayContent(fileBytes);
                    content.Add(imageContent, "files", Path.GetFileName(fullPath));
                }
            }

            if (!content.Any()) return;

            try 
            {
                var response = await _httpClient.PostAsync($"{_pythonApiUrl}/extract", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<FeatureResponse>();
                    if (result != null && result.vector != null)
                    {
                        await SaveFeaturesToDb(productId, result.vector);
                        // Try to assign to current clusters if any
                        await AssignToCurrentClustersAsync(productId, result.vector);
                    }
                }
                else
                {
                    Console.WriteLine($"Python API Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"Error calling Python API: {ex.Message}");
            }
        }

        public async Task<List<float>?> ExtractFeaturesFromFilesAsync(List<IFormFile> files)
        {
            if (files == null || !files.Any()) return null;

            try
            {
                using var content = new MultipartFormDataContent();
                foreach (var f in files)
                {
                    if (f.Length > 0)
                    {
                        var stream = f.OpenReadStream();
                        var imageContent = new StreamContent(stream);
                        content.Add(imageContent, "files", f.FileName);
                    }
                }

                if (!content.Any()) return null;

                var response = await _httpClient.PostAsync($"{_pythonApiUrl}/extract", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<FeatureResponse>();
                    return result?.vector;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling Python API: {ex.Message}");
            }
            return null;
        }

        public async Task<ProductCluster?> FindNearestClusterAsync(List<float> featureVector, double minSimilarity = 0.60)
        {
            var maxVersion = await _context.ProductClusters.MaxAsync(c => (int?)c.Version);
            if (!maxVersion.HasValue) return null;

            var clusters = await _context.ProductClusters
                .Include(c => c.AutoCategories)
                .Where(c => c.Version == maxVersion.Value)
                .ToListAsync();
            
            if (!clusters.Any()) return null;

            ProductCluster? bestCluster = null;
            double maxSimilarity = -1.0;

            // Calculate magnitude of input vector once
            double vecMag = 0;
            for (int i = 0; i < featureVector.Count; i++) vecMag += featureVector[i] * featureVector[i];
            vecMag = Math.Sqrt(vecMag);

            if (vecMag == 0) return null; // Zero vector cannot have similarity

            foreach (var cluster in clusters)
            {
                if (string.IsNullOrEmpty(cluster.CentroidJson)) continue;

                List<float>? centroid = null;
                try { centroid = JsonSerializer.Deserialize<List<float>>(cluster.CentroidJson); } catch { }

                if (centroid == null || centroid.Count != featureVector.Count) continue;

                double dotProduct = 0;
                double cenMag = 0;

                for (int i = 0; i < featureVector.Count; i++)
                {
                    dotProduct += featureVector[i] * centroid[i];
                    cenMag += centroid[i] * centroid[i];
                }
                cenMag = Math.Sqrt(cenMag);

                if (cenMag == 0) continue;

                double similarity = dotProduct / (vecMag * cenMag);

                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                    bestCluster = cluster;
                }
            }

            // CRITICAL: Threshold check for similarity
            // Using parameter minSimilarity (default 0.60)
            if (maxSimilarity < minSimilarity)
            {
                return null;
            }

            return bestCluster;
        }
        
        // NEW: Get Similar Products within a cluster using Cosine Similarity
        public async Task<List<long>> GetSimilarProductsAsync(long clusterId, List<float> targetVector, int? companyId, int take = 10)
        {
             var query = from m in _context.ProductClusterMembers
                        join p in _context.Products on m.ProductId equals p.Id
                        where m.ProductClusterId == clusterId
                        select new { m.ProductId, p.CompanyId };

            if (companyId.HasValue)
            {
                query = query.Where(x => x.CompanyId == companyId.Value);
            }

            var productIdsInCluster = await query.Select(x => x.ProductId).ToListAsync();

            if (!productIdsInCluster.Any()) return new List<long>();

            var features = await _context.ProductFeatures
                .Where(f => productIdsInCluster.Contains(f.ProductId))
                .ToListAsync();

            var similarities = new List<(long ProductId, double Score)>();
            
            // Calculate magnitude of target vector
            double vecMag = 0;
            for (int i = 0; i < targetVector.Count; i++) vecMag += targetVector[i] * targetVector[i];
            vecMag = Math.Sqrt(vecMag);
            if (vecMag == 0) return new List<long>();

            foreach (var f in features)
            {
                List<float>? productVector = null;
                try { productVector = JsonSerializer.Deserialize<List<float>>(f.FeatureVectorJson); } catch { }

                if (productVector != null && productVector.Count == targetVector.Count)
                {
                    double dotProduct = 0;
                    double prodMag = 0;
                    for (int i = 0; i < targetVector.Count; i++)
                    {
                        dotProduct += targetVector[i] * productVector[i];
                        prodMag += productVector[i] * productVector[i];
                    }
                    prodMag = Math.Sqrt(prodMag);
                    
                    if (prodMag > 0)
                    {
                        double similarity = dotProduct / (vecMag * prodMag);
                        similarities.Add((f.ProductId, similarity));
                    }
                }
            }

            return similarities.OrderByDescending(x => x.Score) // Higher score is better
                .Take(take)
                .Select(x => x.ProductId)
                .ToList();
        }

        private async Task AssignToCurrentClustersAsync(long productId, List<float> featureVector)
        {
            try
            {
                var bestCluster = await FindNearestClusterAsync(featureVector);

                if (bestCluster != null)
                {
                    // Check if already member
                    var exists = await _context.ProductClusterMembers
                        .AnyAsync(m => m.ProductId == productId && m.ProductClusterId == bestCluster.Id);
                    
                    if (!exists)
                    {
                        _context.ProductClusterMembers.Add(new ProductClusterMember
                        {
                            ProductId = productId,
                            ProductClusterId = bestCluster.Id
                        });
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // No similar cluster found (or no clusters exist). Create a new one.
                    var maxVersion = await _context.ProductClusters.MaxAsync(c => (int?)c.Version) ?? 0;
                    // If no clusters exist, start version 1. If exist, keep version.
                    if (maxVersion == 0) maxVersion = 1;

                    // We need a name. Let's name it "New Cluster {Date}" or similar.
                    var count = await _context.ProductClusters.CountAsync(c => c.Version == maxVersion);
                    
                    var newCluster = new ProductCluster
                    {
                        Version = maxVersion,
                        Name = $"Cluster {count + 1} (Auto-Created)",
                        CentroidJson = JsonSerializer.Serialize(featureVector),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ProductClusters.Add(newCluster);
                    await _context.SaveChangesAsync(); // Save to get Id

                    _context.ProductClusterMembers.Add(new ProductClusterMember
                    {
                        ProductId = productId,
                        ProductClusterId = newCluster.Id
                    });
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error assigning to cluster: {ex.Message}");
            }
        }

        private async Task SaveFeaturesToDb(long productId, List<float> features)
        {
            var featureJson = JsonSerializer.Serialize(features);
            var existing = await _context.ProductFeatures.FirstOrDefaultAsync(f => f.ProductId == productId);
            
            if (existing != null)
            {
                existing.FeatureVectorJson = featureJson;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ProductFeatures.Add(new ProductFeature
                {
                    ProductId = productId,
                    FeatureVectorJson = featureJson
                });
            }
            await _context.SaveChangesAsync();
        }

        public async Task RunClusteringAsync(int numberOfClusters)
        {
            // 1. Fetch all features
            var featuresEntities = await _context.ProductFeatures.ToListAsync();
            if (!featuresEntities.Any()) return;

            var productDataList = new List<ProductData>();
            foreach (var f in featuresEntities)
            {
                try 
                {
                    var vec = JsonSerializer.Deserialize<List<float>>(f.FeatureVectorJson);
                    if (vec != null && vec.Count > 0)
                    {
                        productDataList.Add(new ProductData { ProductId = f.ProductId, Features = vec });
                    }
                }
                catch { /* Ignore invalid JSON */ }
            }

            if (!productDataList.Any()) return;

            // 2. Call Python API
            var request = new
            {
                features = productDataList.Select(p => p.Features).ToList(),
                k = numberOfClusters
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_pythonApiUrl}/cluster", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ClusterResponse>();
                    if (result != null && result.labels != null)
                    {
                        await SaveClustersToDb(productDataList, result.labels, result.centroids ?? new List<List<float>>(), numberOfClusters);
                    }
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"Error calling Python API (Cluster): {ex.Message}");
            }
        }

        public async Task SplitClusterAsync(int clusterId, int numberOfSubClusters = 2)
        {
            var cluster = await _context.ProductClusters
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == clusterId);

            if (cluster == null) return;
            if (cluster.Members == null || !cluster.Members.Any()) return;

            var productIds = cluster.Members.Select(m => m.ProductId).ToList();

            var featureEntities = await _context.ProductFeatures
                .Where(f => productIds.Contains(f.ProductId))
                .ToListAsync();

            var productDataList = new List<ProductData>();
            foreach (var f in featureEntities)
            {
                try
                {
                    var vec = JsonSerializer.Deserialize<List<float>>(f.FeatureVectorJson);
                    if (vec != null && vec.Count > 0)
                    {
                        productDataList.Add(new ProductData { ProductId = f.ProductId, Features = vec });
                    }
                }
                catch
                {
                }
            }

            if (!productDataList.Any()) return;
            if (numberOfSubClusters < 2) numberOfSubClusters = 2;

            var request = new
            {
                features = productDataList.Select(p => p.Features).ToList(),
                k = numberOfSubClusters
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_pythonApiUrl}/cluster", request);

                if (!response.IsSuccessStatusCode) return;

                var result = await response.Content.ReadFromJsonAsync<ClusterResponse>();
                if (result == null || result.labels == null) return;

                var labels = result.labels;
                if (labels.Count != productDataList.Count) return;

                var centroids = result.centroids ?? new List<List<float>>();

                var distinctLabels = labels.Distinct().OrderBy(l => l).ToList();
                if (distinctLabels.Count < 2) return;

                var minLabel = distinctLabels.Min();

                var clusterMap = new Dictionary<int, ProductCluster>();

                foreach (var label in distinctLabels)
                {
                    if (label == minLabel)
                    {
                        List<float>? centroid = null;
                        if (label >= 0 && label < centroids.Count)
                        {
                            centroid = centroids[label];
                        }

                        cluster.CentroidJson = JsonSerializer.Serialize(centroid ?? new List<float>());
                        clusterMap[label] = cluster;
                    }
                    else
                    {
                        List<float>? centroid = null;
                        if (label >= 0 && label < centroids.Count)
                        {
                            centroid = centroids[label];
                        }

                        var newCluster = new ProductCluster
                        {
                            Version = cluster.Version,
                            Name = $"{cluster.Name} (Split {label})",
                            CentroidJson = JsonSerializer.Serialize(centroid ?? new List<float>()),
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.ProductClusters.Add(newCluster);
                        clusterMap[label] = newCluster;
                    }
                }

                await _context.SaveChangesAsync();

                var members = await _context.ProductClusterMembers
                    .Where(m => m.ProductClusterId == cluster.Id && productIds.Contains(m.ProductId))
                    .ToListAsync();

                var memberByProductId = members.ToDictionary(m => m.ProductId, m => m);

                for (int i = 0; i < productDataList.Count; i++)
                {
                    var pid = productDataList[i].ProductId;
                    if (!memberByProductId.TryGetValue(pid, out var member)) continue;

                    var label = labels[i];
                    if (clusterMap.TryGetValue(label, out var targetCluster))
                    {
                        member.ProductClusterId = targetCluster.Id;
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling Python API (SplitCluster): {ex.Message}");
            }
        }

        private async Task SaveClustersToDb(List<ProductData> products, List<int> labels, List<List<float>> centroids, int k)
        {
            var maxVersion = await _context.ProductClusters.MaxAsync(c => (int?)c.Version) ?? 0;
            var newVersion = maxVersion + 1;

            var clusterMap = new Dictionary<int, ProductCluster>();

            // Create clusters
            // Note: labels are 0..k-1
            var uniqueLabels = labels.Distinct().OrderBy(l => l).ToList();
            
            foreach (var label in uniqueLabels)
            {
                var centroidVec = (centroids != null && label < centroids.Count) ? centroids[label] : new List<float>();

                var cluster = new ProductCluster
                {
                    Version = newVersion,
                    Name = $"Cluster {label + 1} (v{newVersion})",
                    CentroidJson = JsonSerializer.Serialize(centroidVec),
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProductClusters.Add(cluster);
                clusterMap[label] = cluster;
            }
            
            // We need IDs, but let's trust EF graph insertion
            
            for (int i = 0; i < products.Count; i++)
            {
                var pid = products[i].ProductId;
                var label = labels[i];
                
                if (clusterMap.TryGetValue(label, out var cluster))
                {
                    cluster.Members.Add(new ProductClusterMember
                    {
                        ProductId = pid
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<ProductClusterMember?> GetClusterMemberByProductIdAsync(long productId)
        {
            return await _context.ProductClusterMembers
                .Include(m => m.ProductCluster)
                .ThenInclude(c => c.AutoCategories)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ProductId == productId);
        }
    }

    public class FeatureResponse
    {
        public List<float>? vector { get; set; }
    }

    public class ClusterResponse
    {
        public List<int>? labels { get; set; }
        public List<List<float>>? centroids { get; set; }
    }

    public class ProductData
    {
        public long ProductId { get; set; }
        public List<float>? Features { get; set; }
    }
}
