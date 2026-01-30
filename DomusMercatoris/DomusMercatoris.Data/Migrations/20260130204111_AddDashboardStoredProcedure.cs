using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomusMercatoris.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sp = @"
CREATE PROCEDURE [dbo].[sp_GetDashboardData]
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Result Set: Counts
    SELECT
        (SELECT COUNT(*) FROM Users WHERE CompanyId = @CompanyId AND Roles LIKE '%""Customer""%') AS CustomerCount,
        (SELECT COUNT(*) FROM Users WHERE CompanyId = @CompanyId AND Roles LIKE '%""User""%' AND Roles NOT LIKE '%""Manager""%') AS WorkerCount,
        (SELECT COUNT(*) FROM Users WHERE CompanyId = @CompanyId AND Roles LIKE '%""Manager""%') AS ManagerCount,
        (SELECT COUNT(*) FROM Orders WHERE CompanyId = @CompanyId AND Status IN (2, 4)) AS PendingOrdersCount; -- PaymentApproved or Preparing

    -- 2. Result Set: Recent Orders (Top 5)
    SELECT TOP 5
        o.Id,
        o.CreatedAt AS OrderDate,
        o.Status,
        o.TotalPrice AS TotalAmount,
        ISNULL(u.FirstName + ' ' + u.LastName, 'Unknown') AS CustomerName,
        (
             SELECT 
                 oi.Quantity,
                 p.Name AS ProductName
             FROM OrderItems oi
             INNER JOIN Products p ON oi.ProductId = p.Id
             WHERE oi.OrderId = o.Id
             FOR JSON PATH
         ) AS OrderItemsJson
    FROM Orders o
    LEFT JOIN Users u ON o.UserId = u.Id
    WHERE o.CompanyId = @CompanyId
    ORDER BY o.CreatedAt DESC;

    -- 3. Result Set: Low Stock Products
    SELECT TOP 20
        p.Id,
        p.Name,
        p.Quantity,
        p.LowStockThreshold,
        p.ShelfNumber
    FROM Products p
    WHERE p.CompanyId = @CompanyId AND p.Quantity <= p.LowStockThreshold
    ORDER BY p.Quantity ASC;

END";
            migrationBuilder.Sql(sp);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_GetDashboardData]");
        }
    }
}
