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
        private readonly IMapper _mapper;

        public CommentService(
            ICommentRepository commentRepository,
            IGenericRepository<Product> productRepository,
            IMapper mapper)
        {
            _commentRepository = commentRepository;
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CommentDto>> GetAllAsync()
        {
            var comments = await _commentRepository.GetAllWithDetailsAsync();
            return _mapper.Map<IEnumerable<CommentDto>>(comments);
        }

        public async Task<CommentDto?> GetByIdAsync(int id)
        {
            var comment = await _commentRepository.GetByIdWithDetailsAsync(id);
            return comment == null ? null : _mapper.Map<CommentDto>(comment);
        }

        public async Task<IEnumerable<CommentDto>> GetByProductIdAsync(long productId)
        {
            var comments = await _commentRepository.GetByProductIdWithDetailsAsync(productId);
            return _mapper.Map<IEnumerable<CommentDto>>(comments);
        }

        public async Task<CommentDto> CreateAsync(CreateCommentDto createDto, long userId)
        {
            var productExists = await _productRepository.ExistsAsync(createDto.ProductId);
            if (!productExists)
            {
                throw new KeyNotFoundException("Product not found");
            }

            var comment = _mapper.Map<Comment>(createDto);
            comment.UserId = userId;
            comment.CreatedAt = DateTime.UtcNow;
            comment.IsApproved = false;

            await _commentRepository.AddAsync(comment);
            await _commentRepository.SaveChangesAsync();

            // Fetch again to include details for DTO
            var createdComment = await _commentRepository.GetByIdWithDetailsAsync(comment.Id);
            return _mapper.Map<CommentDto>(createdComment);
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

            comment.Text = updateDto.Text;
            if (isAdmin)
            {
                comment.IsApproved = updateDto.IsApproved;
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
