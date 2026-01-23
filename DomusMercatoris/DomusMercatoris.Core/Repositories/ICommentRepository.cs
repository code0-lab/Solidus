using System.Collections.Generic;
using System.Threading.Tasks;
using DomusMercatoris.Core.Entities;

namespace DomusMercatoris.Core.Repositories
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        Task<IEnumerable<Comment>> GetAllWithDetailsAsync();
        Task<IEnumerable<Comment>> GetPagedWithDetailsAsync(int page, int pageSize);
        Task<Comment?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<Comment>> GetByProductIdWithDetailsAsync(long productId, long? userId);
    }
}
