using System.Collections.Generic;
using System.Threading.Tasks;
using DomusMercatoris.Core.Entities;

namespace DomusMercatoris.Core.Repositories
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        Task<IEnumerable<Comment>> GetAllWithDetailsAsync(int? companyId = null);
        Task<IEnumerable<Comment>> GetPagedWithDetailsAsync(int page, int pageSize, int? companyId = null);
        Task<Comment?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<Comment>> GetByProductIdWithDetailsAsync(long productId, long? userId);
    }
}
