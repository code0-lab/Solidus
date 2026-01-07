using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
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

            if (cluster == null) return NotFound();

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
    }
}
