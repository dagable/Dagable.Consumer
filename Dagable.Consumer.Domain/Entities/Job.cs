using System.ComponentModel.DataAnnotations.Schema;

namespace Dagable.Consumer.Domain.Entities
{
    [Table("Job", Schema = "Dagable")]
    public class Job : DomainObject
    {
        public Guid RequestGuid { get; set; }
        public Guid UserGuid { get; set; }
        public int TotalGraphs { get; set; }
        public int CompletedGraphs { get; set; }

        public ICollection<Batch> Batches { get; set; } = new List<Batch>();

        public Job() { }

        public Job(Guid requestGuid, Guid UserId, int totalGraphs)
        {
            UserGuid = UserGuid;
            TotalGraphs = totalGraphs;
            RequestGuid = requestGuid;
        }
    }
}
