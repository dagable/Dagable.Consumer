using System.ComponentModel.DataAnnotations;

namespace Dagable.Consumer.Domain.Entities
{
    public class DomainObject
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
