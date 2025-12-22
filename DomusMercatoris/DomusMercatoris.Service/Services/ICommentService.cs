using DomusMercatoris.Service.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DomusMercatoris.Service.Services
{
    public interface ICommentService
    {
        Task<IEnumerable<CommentDto>> GetAllAsync();
        Task<CommentDto?> GetByIdAsync(int id);
        Task<IEnumerable<CommentDto>> GetByProductIdAsync(long productId);
        Task<CommentDto> CreateAsync(CreateCommentDto createDto, long userId);
        Task UpdateAsync(int id, UpdateCommentDto updateDto, long userId, bool isAdmin);
        Task DeleteAsync(int id, long userId, bool isAdmin);
    }
}
