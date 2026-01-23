using AutoMapper;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace DomusMercatoris.Service.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IMapper _mapper;
        private readonly IGeminiCommentService _geminiCommentService;

        public CommentService(
            ICommentRepository commentRepository,
            IGenericRepository<Product> productRepository,
            IGenericRepository<User> userRepository,
            IMapper mapper,
            IGeminiCommentService geminiCommentService)
        {
            _commentRepository = commentRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _geminiCommentService = geminiCommentService;
        }

        public async Task<IEnumerable<CommentDto>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var comments = await _commentRepository.GetPagedWithDetailsAsync(page, pageSize);
            return _mapper.Map<IEnumerable<CommentDto>>(comments);
        }

        public async Task<CommentDto?> GetByIdAsync(int id)
        {
            var comment = await _commentRepository.GetByIdWithDetailsAsync(id);
            return comment == null ? null : _mapper.Map<CommentDto>(comment);
        }

        public async Task<IEnumerable<CommentDto>> GetByProductIdAsync(long productId, long? userId)
        {
            var comments = await _commentRepository.GetByProductIdWithDetailsAsync(productId, userId);
            return _mapper.Map<IEnumerable<CommentDto>>(comments);
        }

        public async Task<CommentDto> CreateAsync(CreateCommentDto createDto, long userId)
        {
            var product = await _productRepository.GetByIdAsync(createDto.ProductId);
            if (product == null)
            {
                throw new KeyNotFoundException("Product not found");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var comment = _mapper.Map<Comment>(createDto);
            comment.UserId = userId;
            comment.CreatedAt = DateTime.UtcNow;
            
            // Set navigation properties for mapper and EF
            comment.Product = product;
            comment.User = user;

            var isApproved = await _geminiCommentService.ModerateCommentAsync(createDto.Text, product.CompanyId);
            comment.IsApproved = isApproved;
            comment.ModerationStatus = isApproved ? 1 : 2;

            await _commentRepository.AddAsync(comment);
            await _commentRepository.SaveChangesAsync();

            // Map directly from the entity in memory, avoiding "Double-Dip" database call
            return _mapper.Map<CommentDto>(comment);
        }

        public async Task UpdateAsync(int id, UpdateCommentDto updateDto, long userId, bool isAdmin)
        {
            var comment = await _commentRepository.GetByIdAsync(id);
            if (comment == null)
            {
                throw new KeyNotFoundException("Comment not found");
            }

            if (comment.UserId != userId && !isAdmin)
            {
                throw new UnauthorizedAccessException("You are not authorized to update this comment");
            }

            // Check if text has changed
            if (comment.Text != updateDto.Text)
            {
                comment.Text = updateDto.Text;

                // If user is not admin, re-run moderation logic
                if (!isAdmin)
                {
                    var product = await _productRepository.GetByIdAsync(comment.ProductId);
                    if (product != null)
                    {
                        var isApproved = await _geminiCommentService.ModerateCommentAsync(comment.Text, product.CompanyId);
                        comment.IsApproved = isApproved;
                        comment.ModerationStatus = isApproved ? 1 : 2; // 1: Approved, 2: Rejected/Pending Review
                    }
                    else
                    {
                        // Fallback: Set to unapproved if product not found (security first)
                        comment.IsApproved = false;
                        comment.ModerationStatus = 0; // 0: Pending
                    }
                }
            }

            if (isAdmin)
            {
                comment.IsApproved = updateDto.IsApproved;
                comment.ModerationStatus = updateDto.IsApproved ? 1 : 2;
            }

            _commentRepository.Update(comment);
            await _commentRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id, long userId, bool isAdmin)
        {
            var comment = await _commentRepository.GetByIdAsync(id);
            if (comment == null)
            {
                throw new KeyNotFoundException("Comment not found");
            }

            if (comment.UserId != userId && !isAdmin)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this comment");
            }

            _commentRepository.Delete(comment);
            await _commentRepository.SaveChangesAsync();
        }
    }
}
