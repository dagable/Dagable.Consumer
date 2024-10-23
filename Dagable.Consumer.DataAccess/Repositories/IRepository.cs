using Dagable.Consumer.Domain.Entities;

namespace Dagable.Consumer.DataAccess.Repositories
{
    public interface IRepository<Entity> where Entity : DomainObject
    {
        Task<Entity> Insert(Entity entity);

        Task<Entity> Update(Entity entity);
    }
}
