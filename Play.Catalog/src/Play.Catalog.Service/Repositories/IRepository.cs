using Play.Catalog.Service.Entities;

namespace Play.Catalog.Service.Repositories;

public interface IRepository<T> where T : IEntity
{
    Task CreateAsync(T entity);
    Task DelteAsync(Guid id);
    Task<IReadOnlyCollection<T>> GetAllAsync();
    Task<T> GetAsync(Guid id);
    Task UpdateAsync(T entity);
}
