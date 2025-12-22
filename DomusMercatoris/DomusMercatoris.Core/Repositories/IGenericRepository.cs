using System.Collections.Generic;
using System.Threading.Tasks;

namespace DomusMercatoris.Core.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(object id);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<bool> ExistsAsync(object id);
        Task SaveChangesAsync();
    }
}
