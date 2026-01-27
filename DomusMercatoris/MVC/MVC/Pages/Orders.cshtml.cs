using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomusMercatorisDotnetMVC.Services;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Policy = "OrdersAccess")]
    public class OrdersModel : PageModel
    {
        private readonly DomusMercatoris.Service.Services.OrderService _orderService;
        private readonly UserService _userService;

        public OrdersModel(DomusMercatoris.Service.Services.OrderService orderService, UserService userService)
        {
            _orderService = orderService;
            _userService = userService;
        }

        public List<Order> Orders { get; set; } = new List<Order>();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 9;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 1;

        public async Task OnGetAsync()
        {
            int companyId = await _userService.GetCompanyIdFromUserAsync(User);

            if (companyId > 0)
            {
                if (PageNumber < 1) PageNumber = 1;

                var result = await _orderService.GetPagedByCompanyIdAsync(companyId, PageNumber, PageSize);
                
                Orders = result.Items;
                TotalCount = result.TotalCount;
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
                
                if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;
            }
        }
    }
}
