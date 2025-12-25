using AutoMapper;
using DomusMercatorisDotnetMVC.Dto.CommentsDto;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using Microsoft.EntityFrameworkCore;

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
                .Where(c => c.ProductId == productId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<CommentsDto>>(comments);
        }
    }
}
