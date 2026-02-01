using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using DomusMercatoris.Core.Exceptions;
using DomusMercatoris.Service.Interfaces;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClusteringController : ControllerBase
    {
        private readonly IClusteringService _clusteringService;
        private readonly ICurrentUserService _currentUserService;

        public ClusteringController(IClusteringService clusteringService, ICurrentUserService currentUserService)
        {
            _clusteringService = clusteringService;
            _currentUserService = currentUserService;
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
            
            var ext = System.IO.Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var isImageMime = file.ContentType != null && file.ContentType.StartsWith("image/");
            var isHeicExt = ext == ".heic" || ext == ".heif";
            if (!isImageMime && !isHeicExt) throw new BadRequestException("Only image files (including HEIC/HEIF) are allowed.");
            
            var maxBytes = 17 * 1024 * 1024;
            if (file.Length > maxBytes) throw new BadRequestException("File size exceeds 17MB.");

            // Use the Service to extract features (handling API call internally)
            var vector = await _clusteringService.ExtractFeaturesFromFilesAsync(new List<IFormFile> { file });

            if (vector == null || vector.Count == 0)
            {
                throw new InvalidOperationException("Failed to extract features from image.");
            }

            // Find Nearest Cluster (Now uses Cosine Similarity logic & Threshold from Service)
            var bestCluster = await _clusteringService.FindNearestClusterAsync(vector);

            if (bestCluster == null)
            {
                throw new NotFoundException("No matching category found (Similarity too low).");
            }

            // Find Similar Products within the cluster
            var similarProductIds = await _clusteringService.GetSimilarProductsAsync(
                bestCluster.Id, 
                vector, 
                _currentUserService.CompanyId
            );

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
