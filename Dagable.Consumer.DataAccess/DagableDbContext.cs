using Dagable.Consumer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dagable.Consumer.DataAccess
{
    public class DagableDbContext : DbContext
    {
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Batch> Batches { get; set; }

        public DagableDbContext() : base()
        {
        }

        public DagableDbContext(DbContextOptions<DagableDbContext> options)
        : base(options)
        {
        }
    }
}
