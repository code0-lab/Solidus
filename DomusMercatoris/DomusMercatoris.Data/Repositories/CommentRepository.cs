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

        public async Task<IEnumerable<Comment>> GetByProductIdWithDetailsAsync(long productId)
        {
            return await _dbSet
                .Include(c => c.User)
                .Where(c => c.ProductId == productId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
