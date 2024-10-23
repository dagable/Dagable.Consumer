using Dagable.Consumer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dagable.Consumer.DataAccess.Repositories
{
    public class BatchRepository : IRepository<Batch>
    {
        private readonly DagableDbContext _db;
        public BatchRepository(DagableDbContext db)
        {
            _db = db;
        }

        public async Task<Batch> Insert(Batch entity)
        {
            var batch = await _db.Batches.FirstOrDefaultAsync(x => x.JobId == entity.JobId && entity.BatchNumber == x.BatchNumber);

            if (batch == null)
            {
                _db.Batches.Add(entity);

                await _db.SaveChangesAsync();

                await _db.Entry(entity).ReloadAsync();

                return entity;
            }

            return await Update(entity);
        }

        public async Task<Batch> Update(Batch entity)
        {
            var result = await _db.Batches.SingleOrDefaultAsync(x => x.JobId == entity.JobId && entity.BatchNumber == x.BatchNumber);
            if (result != null)
            {
                result.CompressedData = entity.CompressedData;
                await _db.SaveChangesAsync();
            }

            return result;
        }
    }
}
