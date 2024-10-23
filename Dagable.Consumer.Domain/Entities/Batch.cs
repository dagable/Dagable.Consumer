using System.ComponentModel.DataAnnotations.Schema;

namespace Dagable.Consumer.Domain.Entities
{
    [Table("Batch", Schema = "Dagable")]
    public class Batch : DomainObject
    {
        [ForeignKey("Job")]
        public int JobId { get; set; }
        public int BatchNumber { get; set; }
        public byte[] CompressedData { get; set; }

        public Batch()
        {
        }

        public Batch(int batchNumber, byte[] compressedData, int jobId)
        {
            BatchNumber = batchNumber;
            CompressedData = compressedData;
            JobId = jobId;
        }
    }
}
