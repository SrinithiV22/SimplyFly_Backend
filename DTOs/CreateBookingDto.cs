using System.ComponentModel.DataAnnotations;

namespace SimplyFly.Api.DTOs
{
    public class CreateBookingDto
    {
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
        [Range(1, 10)]
        public int Passengers { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string TicketType { get; set; } = "Economy"; // e.g. 'Economy', 'Business'

        [Required]
        public DateTime DepartureTime { get; set; } // snapshot of when this flight departs

        [Required]
        public DateTime ArrivalTime { get; set; } // snapshot of when it arrives
    }
}
