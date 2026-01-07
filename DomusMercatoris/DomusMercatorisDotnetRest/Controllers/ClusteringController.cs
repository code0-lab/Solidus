using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

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
        }

        [HttpPost("classify")]
        public async Task<ActionResult<ClassificationResultDto>> Classify([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File is required.");
            var isImageMime = file.ContentType != null && file.ContentType.StartsWith("image/");
            var ext = System.IO.Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var isHeicExt = ext == ".heic" || ext == ".heif";
            if (!isImageMime && !isHeicExt) return BadRequest("Only image files (including HEIC/HEIF) are allowed.");
            var maxBytes = 17 * 1024 * 1024;
            if (file.Length > maxBytes) return BadRequest("File size exceeds 17MB.");

            var pythonApiUrl = _configuration.GetValue<string>("PythonApiUrl") ?? "http://localhost:5001";
            using var client = new HttpClient();
            using var content = new MultipartFormDataContent();
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var imageContent = new ByteArrayContent(ms.ToArray());
            content.Add(imageContent, "files", file.FileName);

            var response = await client.PostAsync($"{pythonApiUrl}/extract", content);
            if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode, "Python API error.");

            var payload = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("vector", out var vecElement)) return BadRequest("Invalid response.");
            var vector = vecElement.EnumerateArray().Select(v => v.GetSingle()).ToList();

            var maxVersion = await _db.ProductClusters.MaxAsync(c => (int?)c.Version);
            if (!maxVersion.HasValue) return NotFound("No clusters available.");

            var clusters = await _db.ProductClusters.Where(c => c.Version == maxVersion.Value).ToListAsync();
            if (clusters.Count == 0) return NotFound("No clusters available.");

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

            if (bestCluster == null) return NotFound("No matching cluster.");

            return Ok(new ClassificationResultDto
            {
                ClusterId = bestCluster.Id,
                ClusterName = bestCluster.Name,
                Version = bestCluster.Version
            });
        }
    }
}
