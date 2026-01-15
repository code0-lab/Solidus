using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace DomusMercatoris.Data.Repositories
{
    public class CommentRepository : GenericRepository<Comment>, ICommentRepository
    {
        public CommentRepository(DomusDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Comment>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(c => c.User)
                .Include(c => c.Product)
                .Where(c => c.IsApproved)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Comment?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(c => c.User)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Comment>> GetByProductIdWithDetailsAsync(long productId, long? userId)
        {
            var query = _dbSet
                .Include(c => c.User)
                .Where(c => c.ProductId == productId);

            if (userId.HasValue)
            {
                query = query.Where(c => c.IsApproved || c.UserId == userId.Value);
            }
            else
            {
                query = query.Where(c => c.IsApproved);
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
