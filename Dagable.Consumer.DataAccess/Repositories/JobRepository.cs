using Dagable.Consumer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dagable.Consumer.DataAccess.Repositories
{
    public class JobRepository : IRepository<Job>
    {
        private readonly DagableDbContext _db;

        public JobRepository(DagableDbContext db)
        {
            _db = db;
        }

        public async Task<Job> Insert(Job entity)
        {
            var existingJob = await _db.Jobs.FirstOrDefaultAsync(x => x.RequestGuid == entity.RequestGuid);

            if (existingJob == null)
            {
                _db.Jobs.Add(entity);
                await _db.SaveChangesAsync();

                await _db.Entry(entity).ReloadAsync();

                return entity;
            }

            existingJob.CompletedGraphs = 0;
            existingJob.Batches = new List<Batch>();

            return await Update(existingJob);
        }

        public async Task<Job> Update(Job entity)
        {
            var result = await _db.Jobs.SingleOrDefaultAsync(x => x.RequestGuid == entity.RequestGuid);
            if (result != null)
            {
                result.CompletedGraphs = entity.CompletedGraphs;
                await _db.SaveChangesAsync();
            }
            return result;
        }
    }
}
