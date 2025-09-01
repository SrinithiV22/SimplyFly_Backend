using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyFly.Api.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int FlightId { get; set; }

        [MaxLength(100)]
        public string? Flight { get; set; } // e.g. 'Air India'

        [Required]
        [MaxLength(255)]
        public string Route { get; set; } = string.Empty; // e.g. 'Madras to Kochi'

        [MaxLength(255)]
        public string? SelectedSeats { get; set; } // e.g. '7A, 8B'

        [Required]
        public int Passengers { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string TicketType { get; set; } = "Economy"; // e.g. 'Economy', 'Business'

        [Required]
        [Column(TypeName = "date")]
        public DateTime TicketBookingDate { get; set; } = DateTime.Today;

        [Required]
        [Column(TypeName = "time")]
        public TimeSpan TicketBookingTime { get; set; } = DateTime.Now.TimeOfDay;

        public DateTime DepartureTime { get; set; } // snapshot of when this flight departs

        [Required]
        public DateTime ArrivalTime { get; set; } // snapshot of when it arrives

        [MaxLength(50)]
        public string Status { get; set; } = "Confirmed"; // Status of the booking

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column(TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("FlightId")]
        public virtual Models.Flight? FlightDetails { get; set; }
    }
}