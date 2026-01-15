using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using DomusMercatorisDotnetMVC.Services;
using System.Linq;
using DomusMercatorisDotnetMVC.Dto.CommentsDto;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,user")]
    public class DashboardModel : PageModel
    {
        private readonly ProductService _productService;
        private readonly UserService _userService;
        private readonly CommentService _commentService;

        public DashboardModel(ProductService productService, UserService userService, CommentService commentService)
        {
            _productService = productService;
            _userService = userService;
            _commentService = commentService;
        }

        public int CompanyId { get; set; }
        public int ProductCount { get; set; }
        public int WorkerCount { get; set; }
        public int ManagerCount { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public List<CommentsDto> RecentComments { get; set; } = new();

        public async Task OnGetAsync()
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var companyId))
            {
                CompanyId = companyId;
            }
            else
            {
                var idClaim = User.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
                {
                    var me = _userService.GetById(userId);
                    if (me != null)
                    {
                        CompanyId = me.CompanyId;
                        ManagerName = me.FirstName + " " + me.LastName;
                    }
                }
            }
            if (CompanyId >= 0)
            {
                ProductCount = _productService.CountByCompany(CompanyId);
                var users = _userService.GetByCompany(CompanyId);
                ManagerCount = users.Count(u => u.Roles?.Contains("Manager") ?? false);
                WorkerCount = users.Count(u => !(u.Roles?.Contains("Manager") ?? false));
                CompanyName = _userService.GetCompanyName(CompanyId) ?? string.Empty;
                var comments = await _commentService.GetLatestCommentsForCompanyAsync(CompanyId, 5);
                RecentComments = comments.Where(c => c.IsApproved).ToList();
            }
        }
    }
}
