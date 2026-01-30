using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Models;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DomusMercatoris.Service.Services
{
    public class CargoService
    {
        private readonly DomusDbContext _context;
        private readonly IMapper _mapper;

        public CargoService(DomusDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<CargoTrackingDto> CreateTrackingAsync(CreateCargoTrackingDto dto)
        {
            var entity = _mapper.Map<CargoTracking>(dto);
            entity.Status = CargoStatus.Pending;
            entity.CreatedAt = DateTime.UtcNow;

            _context.CargoTrackings.Add(entity);
            await _context.SaveChangesAsync();

            // Reload to get related data if needed
            if (entity.UserId.HasValue)
            {
                await _context.Entry(entity).Reference(e => e.User).LoadAsync();
            }

            return _mapper.Map<CargoTrackingDto>(entity);
        }

        public async Task<CargoTrackingDto> GetByTrackingNumberAsync(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                throw new BadRequestException("Tracking number cannot be empty.");
            }

            var entity = await _context.CargoTrackings
                .AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.TrackingNumber == trackingNumber);

            if (entity == null) throw new NotFoundException($"Cargo {trackingNumber} not found.");
            return _mapper.Map<CargoTrackingDto>(entity);
        }

        public async Task<List<CargoTrackingDto>> GetUserCargosAsync(long userId)
        {
            var list = await _context.CargoTrackings
                .AsNoTracking()
                .Include(c => c.User)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<CargoTrackingDto>>(list);
        }

        public async Task UpdateStatusAsync(UpdateCargoStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TrackingNumber))
            {
                throw new BadRequestException("Tracking number is required.");
            }

            var entity = await _context.CargoTrackings
                .FirstOrDefaultAsync(c => c.TrackingNumber == dto.TrackingNumber);

            if (entity == null) throw new NotFoundException($"Cargo {dto.TrackingNumber} not found.");

            entity.Status = dto.NewStatus;
            
            if (!string.IsNullOrEmpty(dto.Description))
            {
                entity.Description = dto.Description;
            }

            if (dto.NewStatus == CargoStatus.Shipped && !entity.ShippedDate.HasValue)
            {
                entity.ShippedDate = DateTime.UtcNow;
            }
            else if (dto.NewStatus == CargoStatus.Delivered && !entity.DeliveredDate.HasValue)
            {
                entity.DeliveredDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<PaginatedResult<CargoTrackingDto>> GetAllCargosAsync(int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.CargoTrackings
                .AsNoTracking()
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<CargoTrackingDto>
            {
                Items = _mapper.Map<List<CargoTrackingDto>>(items),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}