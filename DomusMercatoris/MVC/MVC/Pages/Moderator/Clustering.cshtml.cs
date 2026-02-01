using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.Interfaces;
using DomusMercatorisDotnetMVC.Services;

namespace DomusMercatorisDotnetMVC.Pages.Moderator
{
    [Authorize(Roles = "Moderator,Rex")]
    public class ClusteringModel : PageModel
    {
        private readonly DomusDbContext _context;
        private readonly IClusteringService _clusteringService;

        public ClusteringModel(DomusDbContext context, IClusteringService clusteringService)
        {
            _context = context;
            _clusteringService = clusteringService;
        }

        public List<ProductCluster> Clusters { get; set; } = new();
        public List<int> Versions { get; set; } = new();
        
        [BindProperty(SupportsGet = true)]
        public int? CurrentVersion { get; set; }

        [BindProperty]
        public int NumberOfClusters { get; set; } = 5;

        public int TotalPages { get; set; }
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public async Task OnGetAsync()
        {
            Versions = await _context.ProductClusters
                .Select(c => c.Version)
                .Distinct()
                .OrderByDescending(v => v)
                .ToListAsync();

            if (!CurrentVersion.HasValue && Versions.Any())
            {
                CurrentVersion = Versions.First();
            }

            if (CurrentVersion.HasValue)
            {
                var query = _context.ProductClusters
                    .Where(c => c.Version == CurrentVersion.Value);

                int count = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(count / (double)PageSize);
                if (PageIndex < 1) PageIndex = 1;
                if (PageIndex > TotalPages && TotalPages > 0) PageIndex = TotalPages;

                Clusters = await query
                    .Include(c => c.Members)
                    .OrderBy(c => c.Name)
                    .Skip((PageIndex - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnGetClusterMembersAsync(int clusterId)
        {
            var cluster = await _context.ProductClusters
                .Include(c => c.Members)
                .ThenInclude(m => m.Product)
                .FirstOrDefaultAsync(c => c.Id == clusterId);

            if (cluster == null) throw new NotFoundException($"Cluster {clusterId} not found.");

            return Partial("_ClusterMembers", cluster.Members);
        }

        public async Task<IActionResult> OnPostTrainAsync()
        {
            if (NumberOfClusters < 2) NumberOfClusters = 2;
            
            await _clusteringService.RunClusteringAsync(NumberOfClusters);
            
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostExtractFeaturesAsync()
        {
            await _clusteringService.ProcessAllProductsFeaturesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRenameAsync(int clusterId, string newName, int pageIndex)
        {
            var cluster = await _context.ProductClusters.FindAsync(clusterId);
            if (cluster != null)
            {
                cluster.Name = newName;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { CurrentVersion = cluster?.Version, PageIndex = pageIndex });
        }

        public async Task<IActionResult> OnPostDeleteVersionAsync(int version)
        {
            var clusters = await _context.ProductClusters
                .Where(c => c.Version == version)
                .ToListAsync();

            if (clusters.Any())
            {
                _context.ProductClusters.RemoveRange(clusters);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMoveMemberAsync(long productId, int? fromClusterId, int? toClusterId, bool createNewCluster, int? version)
        {
            if (!createNewCluster && !toClusterId.HasValue)
            {
                return BadRequest(new { success = false, message = "Target cluster is required." });
            }

            var memberQuery = _context.ProductClusterMembers.AsQueryable().Where(m => m.ProductId == productId);

            if (fromClusterId.HasValue)
            {
                memberQuery = memberQuery.Where(m => m.ProductClusterId == fromClusterId.Value);
            }

            var member = await memberQuery.FirstOrDefaultAsync();
            if (member == null)
            {
                throw new NotFoundException("Member not found.");
            }

            ProductCluster? targetCluster = null;

            if (createNewCluster)
            {
                int targetVersion = version ?? await _context.ProductClusters
                    .Where(c => c.Id == member.ProductClusterId)
                    .Select(c => c.Version)
                    .FirstOrDefaultAsync();

                if (targetVersion <= 0)
                {
                    targetVersion = 1;
                }

                targetCluster = new ProductCluster
                {
                    Version = targetVersion,
                    Name = $"Manual Cluster (v{targetVersion})",
                    CentroidJson = null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProductClusters.Add(targetCluster);
                await _context.SaveChangesAsync();
            }
            else if (toClusterId.HasValue)
            {
                targetCluster = await _context.ProductClusters.FindAsync(toClusterId.Value);
                if (targetCluster == null)
                {
                    throw new NotFoundException("Target cluster not found.");
                }
            }

            if (targetCluster == null)
            {
                throw new BadRequestException("Invalid target cluster.");
            }

            member.ProductClusterId = targetCluster.Id;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, targetClusterId = targetCluster.Id });
        }

        public async Task<IActionResult> OnPostMergeAsync(int sourceClusterId, int targetClusterId, int pageIndex)
        {
            if (sourceClusterId == targetClusterId)
            {
                return RedirectToPage(new { CurrentVersion, PageIndex = pageIndex });
            }

            var source = await _context.ProductClusters
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == sourceClusterId);

            var target = await _context.ProductClusters
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == targetClusterId);

            if (source == null || target == null)
            {
                return NotFound();
            }

            var existingProductIds = await _context.ProductClusterMembers
                .Where(m => m.ProductClusterId == target.Id)
                .Select(m => m.ProductId)
                .ToListAsync();

            foreach (var member in source.Members.ToList())
            {
                if (!existingProductIds.Contains(member.ProductId))
                {
                    member.ProductClusterId = target.Id;
                }
            }

            _context.ProductClusters.Remove(source);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { CurrentVersion = target.Version, PageIndex = pageIndex });
        }

        public async Task<IActionResult> OnPostSplitAsync(int clusterId, int pageIndex)
        {
            await _clusteringService.SplitClusterAsync(clusterId, 2);

            var version = await _context.ProductClusters
                .Where(c => c.Id == clusterId)
                .Select(c => c.Version)
                .FirstOrDefaultAsync();

            if (version <= 0 && CurrentVersion.HasValue)
            {
                version = CurrentVersion.Value;
            }

            return RedirectToPage(new { CurrentVersion = version, PageIndex = pageIndex });
        }

        public async Task<IActionResult> OnPostDeleteClusterAsync(int clusterId, int pageIndex)
        {
            var cluster = await _context.ProductClusters
                .FirstOrDefaultAsync(c => c.Id == clusterId);

            if (cluster == null)
            {
                return RedirectToPage(new { CurrentVersion, PageIndex = pageIndex });
            }

            var version = cluster.Version;

            _context.ProductClusters.Remove(cluster);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { CurrentVersion = version, PageIndex = pageIndex });
        }
    }
}
