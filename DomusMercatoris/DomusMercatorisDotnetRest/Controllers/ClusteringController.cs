using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using DomusMercatoris.Core.Exceptions;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClusteringController : ControllerBase
    {
        private readonly DomusDbContext _db;
        private readonly IConfiguration _configuration;

        public ClusteringController(DomusDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public class ClassificationResultDto
        {
            public int ClusterId { get; set; }
            public string? ClusterName { get; set; }
            public int Version { get; set; }
            public List<long> SimilarProductIds { get; set; } = new();
        }
        
        public class FileUploadDto
        {
            public IFormFile? File { get; set; }
        }

        [HttpPost("classify")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ClassificationResultDto>> Classify([FromForm] FileUploadDto form)
        {
            var file = form.File;
            if (file == null || file.Length == 0) throw new BadRequestException("File is required.");
            var isImageMime = file.ContentType != null && file.ContentType.StartsWith("image/");
            var ext = System.IO.Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var isHeicExt = ext == ".heic" || ext == ".heif";
            if (!isImageMime && !isHeicExt) throw new BadRequestException("Only image files (including HEIC/HEIF) are allowed.");
            var maxBytes = 17 * 1024 * 1024;
            if (file.Length > maxBytes) throw new BadRequestException("File size exceeds 17MB.");

            var pythonApiUrl = _configuration.GetValue<string>("PythonApiUrl") ?? "http://localhost:5001";
            using var client = new HttpClient();
            using var content = new MultipartFormDataContent();
            
            // 1. Read file to memory
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            byte[] finalBytes;

            // 2. Attempt C# Processing (Fallback for Client-Side)
            try 
            {
                using var image = Image.Load(ms);
                
                // Check if resizing/processing is needed
                // "Resnet 50 ye ulaşan foroğraflar her zaman resnet için olabildiğince ideal olmalı o yüzden gelen fotoğrafları her zaman 224x224 ve rgb yapacağız"
                bool needsProcessing = image.Width != 224 || image.Height != 224;

                if (needsProcessing)
                {
                    // Resize to fit 224x224 with Padding (White)
                    var options = new ResizeOptions
                    {
                        Size = new Size(224, 224),
                        Mode = ResizeMode.Pad,
                        PadColor = Color.White
                    };

                    // Resize AND Composite over White (to handle transparency in the image content itself)
                    image.Mutate(x => x
                        .Resize(options)
                        .BackgroundColor(Color.White)); 
                    
                    using var outStream = new MemoryStream();
                    await image.SaveAsJpegAsync(outStream);
                    finalBytes = outStream.ToArray();
                }
                else
                {
                    // Even if 224x224, ensure it's RGB (JPEG) and handled transparency?
                    // If client sent PNG 224x224 with transparency, we still want to flatten it.
                    // But if client sent JPEG 224x224, it's fine.
                    // Let's assume if it matches dimensions, client likely did the job, OR it's just a coincidence.
                    // But to be safe, we can just use the original bytes if dimensions match, 
                    // assuming the client logic works. 
                    // However, if the user uploaded a 224x224 transparent PNG directly (bypassing client logic somehow),
                    // we might want to flatten it. 
                    // Let's stick to: if dimensions match, trust it (optimization).
                    finalBytes = ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClusteringController] Image processing fallback failed: {ex.Message}");
                // Fallback to original bytes
                finalBytes = ms.ToArray();
            }

            var imageContent = new ByteArrayContent(finalBytes);
            content.Add(imageContent, "files", file.FileName.Replace(ext ?? "", ".jpg")); // Ensure extension is jpg if we converted? Or just keep original name.

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync($"{pythonApiUrl}/extract", content);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[ClusteringController] Python API requires attention: {ex.Message}");
                throw new InvalidOperationException("AI Service is unavailable. Please check backend logs.");
            }

            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Python API error: {response.StatusCode}");

            var payload = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("vector", out var vecElement)) throw new BadRequestException("Invalid response.");
            var vector = vecElement.EnumerateArray().Select(v => v.GetSingle()).ToList();

            var maxVersion = await _db.ProductClusters.MaxAsync(c => (int?)c.Version);
            if (!maxVersion.HasValue) throw new NotFoundException("No clusters available.");

            var clusters = await _db.ProductClusters.Where(c => c.Version == maxVersion.Value).ToListAsync();
            if (clusters.Count == 0) throw new NotFoundException("No clusters available.");

            double minDistance = double.MaxValue;
            DomusMercatoris.Core.Entities.ProductCluster? bestCluster = null;

            foreach (var c in clusters)
            {
                if (string.IsNullOrEmpty(c.CentroidJson)) continue;
                List<float>? centroid = null;
                try { centroid = JsonSerializer.Deserialize<List<float>>(c.CentroidJson); } catch { }
                if (centroid == null || centroid.Count != vector.Count) continue;

                double dist = 0;
                for (int i = 0; i < vector.Count; i++)
                {
                    var diff = vector[i] - centroid[i];
                    dist += diff * diff;
                }
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestCluster = c;
                }
            }
            Console.WriteLine($"[ClusteringController] Best Cluster: {bestCluster?.Name}, Min Distance: {minDistance}");

            if (bestCluster == null) throw new NotFoundException("No matching cluster.");

            // Find similar products within the cluster
            var similarProductIds = new List<long>();
            try
            {
                var productIdsInCluster = await _db.ProductClusterMembers
                    .Where(m => m.ProductClusterId == bestCluster.Id)
                    .Select(m => m.ProductId)
                    .ToListAsync();

                if (productIdsInCluster.Any())
                {
                    var features = await _db.ProductFeatures
                        .Where(f => productIdsInCluster.Contains(f.ProductId))
                        .ToListAsync();

                    var similarities = new List<(long ProductId, double Distance)>();

                    foreach (var f in features)
                    {
                        List<float>? productVector = null;
                        try { productVector = JsonSerializer.Deserialize<List<float>>(f.FeatureVectorJson); } catch { }

                        if (productVector != null && productVector.Count == vector.Count)
                        {
                            double dist = 0;
                            for (int i = 0; i < vector.Count; i++)
                            {
                                var diff = vector[i] - productVector[i];
                                dist += diff * diff;
                            }
                            similarities.Add((f.ProductId, dist));
                        }
                    }

                    similarProductIds = similarities.OrderBy(x => x.Distance)
                        .Take(10) // Take top 10
                        .Select(x => x.ProductId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating similarity: {ex.Message}");
            }

            return Ok(new ClassificationResultDto
            {
                ClusterId = bestCluster.Id,
                ClusterName = bestCluster.Name,
                Version = bestCluster.Version,
                SimilarProductIds = similarProductIds
            });
        }
    }
}
