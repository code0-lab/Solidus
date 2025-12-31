using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Core.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly DomusDbContext _db;
        public SalesController(DomusDbContext db)
        {
            _db = db;
        }
        [HttpPost("checkout")]
        [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SaleDto>> Checkout([FromBody] SaleCreateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (dto.UserId == null && dto.FleetingUser == null) return BadRequest("Either UserId or FleetingUser must be provided.");
            long? fleetingUserId = null;
            if (!dto.UserId.HasValue && dto.FleetingUser != null)
            {
                var fu = new FleetingUser
                {
                    Email = dto.FleetingUser.Email,
                    FirstName = dto.FleetingUser.FirstName,
                    LastName = dto.FleetingUser.LastName,
                    Address = dto.FleetingUser.Address
                };
                _db.Add(fu);
                await _db.SaveChangesAsync();
                fleetingUserId = fu.Id;
            }
            var sale = new Sale
            {
                CompanyId = dto.CompanyId,
                UserId = dto.UserId ?? 0,
                FleetingUserId = fleetingUserId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();
            decimal total = 0;
            foreach (var it in dto.Items)
            {
                var product = await _db.Products.SingleOrDefaultAsync(p => p.Id == it.ProductId && p.CompanyId == dto.CompanyId);
                if (product == null) continue;
                VariantProduct vp = null;
                if (it.VariantProductId.HasValue)
                {
                    vp = await _db.VariantProducts.SingleOrDefaultAsync(v => v.Id == it.VariantProductId.Value && v.ProductId == product.Id);
                }
                var unitPrice = vp != null ? vp.Price : product.Price;
                var sp = new SaleProduct
                {
                    SaleId = sale.Id,
                    ProductId = product.Id,
                    VariantProductId = vp?.Id,
                    UnitPrice = unitPrice,
                    Quantity = it.Quantity
                };
                total += unitPrice * it.Quantity;
                _db.SaleProducts.Add(sp);
            }
            sale.TotalPrice = total;
            await _db.SaveChangesAsync();
            var res = new SaleDto
            {
                Id = sale.Id,
                IsPaid = sale.IsPaid,
                TotalPrice = sale.TotalPrice,
                CompanyId = sale.CompanyId,
                UserId = sale.UserId,
                FleetingUserId = sale.FleetingUserId,
                CargoTrackingId = sale.CargoTrackingId
            };
            return Ok(res);
        }
        [HttpPost("{id:long}/mark-paid")]
        [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SaleDto>> MarkPaid(long id)
        {
            var sale = await _db.Sales.Include(s => s.SaleProducts).SingleOrDefaultAsync(s => s.Id == id);
            if (sale == null) return NotFound();
            sale.IsPaid = true;
            sale.PaidAt = DateTime.UtcNow;
            var track = new CargoTracking
            {
                TrackingNumber = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                CarrierName = "Domus Cargo",
                Status = CargoStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UserId = sale.UserId > 0 ? sale.UserId : null,
                FleetingUserId = sale.FleetingUserId
            };
            _db.CargoTrackings.Add(track);
            await _db.SaveChangesAsync();
            sale.CargoTrackingId = track.Id;
            await _db.SaveChangesAsync();
            var res = new SaleDto
            {
                Id = sale.Id,
                IsPaid = sale.IsPaid,
                TotalPrice = sale.TotalPrice,
                CompanyId = sale.CompanyId,
                UserId = sale.UserId,
                FleetingUserId = sale.FleetingUserId,
                CargoTrackingId = sale.CargoTrackingId
            };
            return Ok(res);
        }
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SaleDto>> Get(long id)
        {
            var sale = await _db.Sales.SingleOrDefaultAsync(s => s.Id == id);
            if (sale == null) return NotFound();
            var res = new SaleDto
            {
                Id = sale.Id,
                IsPaid = sale.IsPaid,
                TotalPrice = sale.TotalPrice,
                CompanyId = sale.CompanyId,
                UserId = sale.UserId,
                FleetingUserId = sale.FleetingUserId,
                CargoTrackingId = sale.CargoTrackingId
            };
            return Ok(res);
        }
        [HttpGet("{id:long}/tracking")]
        [ProducesResponseType(typeof(CargoTracking), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CargoTracking>> GetTracking(long id)
        {
            var sale = await _db.Sales.SingleOrDefaultAsync(s => s.Id == id);
            if (sale == null || !sale.CargoTrackingId.HasValue) return NotFound();
            var tr = await _db.CargoTrackings.SingleOrDefaultAsync(t => t.Id == sale.CargoTrackingId.Value);
            if (tr == null) return NotFound();
            return Ok(tr);
        }
    }
}
