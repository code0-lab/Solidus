using AutoMapper;
using DomusMercatorisDotnetMVC.Dto.CommentsDto;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Service.Services;

namespace DomusMercatorisDotnetMVC.Services
{
    public class CommentService
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;
        private readonly GeminiCommentService _geminiCommentService;

        public CommentService(DomusDbContext db, IMapper mapper, GeminiCommentService geminiCommentService)
        {
            _db = db;
            _mapper = mapper;
            _geminiCommentService = geminiCommentService;
        }

        public async Task<CommentsDto> AddCommentAsync(CreateCommentDto createDto, long userId)
        {
            var comment = _mapper.Map<Comment>(createDto);
            comment.UserId = userId;
            comment.CreatedAt = DateTime.UtcNow;

            // Get user's company ID to check AI moderation settings
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                // Validate role
                if (user.Roles == null || !user.Roles.Contains("Customer"))
                {
                    throw new UnauthorizedAccessException("Only users with Customer role can post comments.");
                }

                // Perform AI moderation
                bool isApproved = await _geminiCommentService.ModerateCommentAsync(comment.Text, user.CompanyId);
                comment.IsApproved = isApproved;
                comment.ModerationStatus = isApproved ? 1 : 2;
            }
            else
            {
                // If user not found, we cannot validate role or proceed safely.
                throw new KeyNotFoundException("User not found.");
            }

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            // Load user for mapping
            await _db.Entry(comment).Reference(c => c.User).LoadAsync();

            return _mapper.Map<CommentsDto>(comment);
        }

        public async Task<List<CommentsDto>> GetCommentsByProductIdAsync(long productId)
        {
            var comments = await _db.Comments
                .Include(c => c.User)
                .Include(c => c.Product)
                .Where(c => c.ProductId == productId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<CommentsDto>>(comments);
        }

        public async Task<bool> SetApprovalAsync(int commentId, bool isApproved, int companyId)
        {
            var comment = await _db.Comments
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.Product != null && c.Product.CompanyId == companyId);

            if (comment == null)
            {
                return false;
            }

            comment.IsApproved = isApproved;
            comment.ModerationStatus = isApproved ? 1 : 2;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<CommentsDto>> GetLatestCommentsForCompanyAsync(int companyId, int take = 10)
        {
            if (take <= 0) take = 10;

            var comments = await _db.Comments
                .Include(c => c.User)
                .Include(c => c.Product)
                .Where(c => c.Product != null && c.Product.CompanyId == companyId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(take)
                .ToListAsync();

            var result = comments
                .Select(c => new CommentsDto
                {
                    Id = c.Id,
                    ProductId = c.ProductId,
                    UserId = c.UserId,
                    UserName = c.User != null ? c.User.FirstName + " " + c.User.LastName : string.Empty,
                    ProductName = c.Product != null ? c.Product.Name : string.Empty,
                    Comment = c.Text,
                    IsApproved = c.IsApproved,
                    CreatedAt = c.CreatedAt
                })
                .ToList();

            return result;
        }

        public async Task<(List<ProductCommentsSummaryDto> Items, int TotalCount)> GetProductsWithCommentsForCompanyAsync(int companyId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var baseQuery = _db.Comments
                .Include(c => c.Product)
                .Where(c => c.Product != null && c.Product.CompanyId == companyId)
                .GroupBy(c => new { c.ProductId, c.Product.Name });

            var totalCount = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderByDescending(g => g.Count())
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new ProductCommentsSummaryDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    CommentCount = g.Count()
                })
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
