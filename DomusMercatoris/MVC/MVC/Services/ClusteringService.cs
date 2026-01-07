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
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DomusMercatorisDotnetMVC.Services
{
    public interface IClusteringService
    {
        Task ExtractAndStoreFeaturesAsync(long productId, List<string> imagePaths);
        Task RunClusteringAsync(int numberOfClusters);
        Task ProcessAllProductsFeaturesAsync();
        Task<List<float>?> ExtractFeaturesFromFilesAsync(List<Microsoft.AspNetCore.Http.IFormFile> files);
        Task<ProductCluster?> FindNearestClusterAsync(List<float> featureVector);
    }

    public class ClusteringService : IClusteringService
    {
        private readonly DomusDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _httpClient;
        private readonly string _pythonApiUrl;

        public ClusteringService(DomusDbContext context, IWebHostEnvironment env, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _env = env;
            _httpClient = httpClient;
            _pythonApiUrl = configuration.GetValue<string>("PythonApiUrl") ?? "http://localhost:5001";
        }

        public async Task ProcessAllProductsFeaturesAsync()
        {
            // Get all products
            var allProducts = await _context.Products.ToListAsync();

            // Get all product IDs that have features
            var existingFeatureProductIds = await _context.ProductFeatures
                .Select(f => f.ProductId)
                .ToListAsync();
            
            var existingSet = new HashSet<long>(existingFeatureProductIds);
            
            var productsToProcess = allProducts
                .Where(p => !existingSet.Contains(p.Id))
                .ToList();

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

            try
            {
                using var content = new MultipartFormDataContent();
                
                foreach (var p in imagePaths)
                {
                    var cleanPath = p.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var fullPath = Path.Combine(_env.WebRootPath, cleanPath);
                    
                    if (File.Exists(fullPath))
                    {
                        var fileBytes = await File.ReadAllBytesAsync(fullPath);
                        var imageContent = new ByteArrayContent(fileBytes);
                        content.Add(imageContent, "files", Path.GetFileName(fullPath));
                    }
                }

                if (!content.Any()) return;

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
                    // Log error
                    Console.WriteLine($"Python API Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling Python API: {ex.Message}");
            }
        }

        public async Task<List<float>?> ExtractFeaturesFromFilesAsync(List<Microsoft.AspNetCore.Http.IFormFile> files)
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

        public async Task<ProductCluster?> FindNearestClusterAsync(List<float> featureVector)
        {
            var maxVersion = await _context.ProductClusters.MaxAsync(c => (int?)c.Version);
            if (!maxVersion.HasValue) return null;

            var clusters = await _context.ProductClusters
                .Include(c => c.AutoCategories)
                .Where(c => c.Version == maxVersion.Value)
                .ToListAsync();
            
            if (!clusters.Any()) return null;

            ProductCluster? bestCluster = null;
            double minDistance = double.MaxValue;

            foreach (var cluster in clusters)
            {
                if (string.IsNullOrEmpty(cluster.CentroidJson)) continue;

                List<float>? centroid = null;
                try { centroid = JsonSerializer.Deserialize<List<float>>(cluster.CentroidJson); } catch { }

                if (centroid == null || centroid.Count != featureVector.Count) continue;

                double dist = 0;
                for (int i = 0; i < featureVector.Count; i++)
                {
                    double diff = featureVector[i] - centroid[i];
                    dist += diff * diff;
                }

                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestCluster = cluster;
                }
            }
            return bestCluster;
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
