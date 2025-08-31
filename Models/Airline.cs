using System.ComponentModel.DataAnnotations;

namespace SimplyFly.Api.Models
{
    public class Airline
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        // Navigation Properties
        public virtual ICollection<Flight> Flights { get; set; } = new List<Flight>();
    }
}