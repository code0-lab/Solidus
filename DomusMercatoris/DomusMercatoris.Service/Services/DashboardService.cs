using System.Data;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using DomusMercatoris.Service.Interfaces;

namespace DomusMercatoris.Service.Services
{
    public class DashboardService
    {
        private readonly DomusDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DashboardService(DomusDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<DashboardDataDto> GetDashboardDataAsync(int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                throw new UnauthorizedAccessException("Cannot access dashboard data for another company.");
            }

            var result = new DashboardDataDto();

            var connection = _context.Database.GetDbConnection();
            bool wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "sp_GetDashboardData";
                    command.CommandType = CommandType.StoredProcedure;
                    
                    var param = command.CreateParameter();
                    param.ParameterName = "@CompanyId";
                    param.Value = companyId;
                    command.Parameters.Add(param);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // 1. Result Set: Role Counts & Pending Orders Count
                        if (await reader.ReadAsync())
                        {
                            result.CustomerCount = reader.IsDBNull(reader.GetOrdinal("CustomerCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("CustomerCount"));
                            result.WorkerCount = reader.IsDBNull(reader.GetOrdinal("WorkerCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("WorkerCount"));
                            result.ManagerCount = reader.IsDBNull(reader.GetOrdinal("ManagerCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ManagerCount"));
                            result.PendingOrdersCount = reader.IsDBNull(reader.GetOrdinal("PendingOrdersCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("PendingOrdersCount"));
                        }

                        // 2. Result Set: Recent Orders
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var order = new OrderDto
                                {
                                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                                    Status = (DomusMercatoris.Core.Models.OrderStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                                    TotalPrice = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                                    User = new UserDto 
                                    { 
                                        FirstName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? "Unknown" : reader.GetString(reader.GetOrdinal("CustomerName")),
                                        LastName = "" 
                                    }
                                };

                                if (!reader.IsDBNull(reader.GetOrdinal("OrderItemsJson")))
                                {
                                    var json = reader.GetString(reader.GetOrdinal("OrderItemsJson"));
                                    if (!string.IsNullOrEmpty(json))
                                    {
                                        try 
                                        {
                                            order.OrderItems = JsonSerializer.Deserialize<List<OrderItemDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<OrderItemDto>();
                                        }
                                        catch 
                                        {
                                            // Handle JSON parse error gracefully
                                            order.OrderItems = new List<OrderItemDto>();
                                        }
                                    }
                                }

                                result.RecentOrders.Add(order);
                            }
                        }

                        // 3. Result Set: Low Stock Products
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.LowStockProducts.Add(new ProductDto
                                {
                                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                                    Name = reader.GetString(reader.GetOrdinal("Name")),
                                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                                    LowStockThreshold = reader.GetInt32(reader.GetOrdinal("LowStockThreshold")),
                                    ShelfNumber = reader.IsDBNull(reader.GetOrdinal("ShelfNumber")) ? null : reader.GetString(reader.GetOrdinal("ShelfNumber"))
                                });
                            }
                        }
                    }
                }
            }
            finally
            {
                if (!wasOpen && connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }

            return result;
        }
    }
}
