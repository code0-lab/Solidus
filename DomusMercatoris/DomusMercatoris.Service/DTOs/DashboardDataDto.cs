using System;
using System.Collections.Generic;
using DomusMercatoris.Service.DTOs;

namespace DomusMercatoris.Service.DTOs
{
    public class DashboardDataDto
    {
        public int CustomerCount { get; set; }
        public int WorkerCount { get; set; }
        public int ManagerCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public List<OrderDto> RecentOrders { get; set; } = new List<OrderDto>();
        public List<ProductDto> LowStockProducts { get; set; } = new List<ProductDto>();
    }
}
