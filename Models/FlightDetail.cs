using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyFly.Api.Models
{
    public class FlightDetail
    {
        [Key]
        public int FlightDetailId { get; set; }

        [Required]
        public int FlightId { get; set; }

        [Required]
        public int FlightOwnerId { get; set; }

        [Required]
        [StringLength(100)]
        public string FlightName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? BaggageInfo { get; set; }

        [Required]
        public int NumberOfSeats { get; set; }

        [Required]
        public DateTime DepartureTime { get; set; }

        [Required]
        public DateTime ArrivalTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Fare { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("FlightId")]
        public Flight Flight { get; set; } = null!;

        [ForeignKey("FlightOwnerId")]
        public FlightOwner FlightOwner { get; set; } = null!;
    }
}
