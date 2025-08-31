using System.ComponentModel.DataAnnotations;

namespace SimplyFly.Api.DTOs
{
    public class CreateFlightDetailDto
    {
        [Required]
        public int FlightId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FlightName { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? BaggageInfo { get; set; }
        
        [Required]
        [Range(1, 1000)]
        public int NumberOfSeats { get; set; }
        
        [Required]
        public DateTime DepartureTime { get; set; }
        
        [Required]
        public DateTime ArrivalTime { get; set; }
        
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Fare { get; set; }
    }

    public class UpdateFlightDetailDto
    {
        [Required]
        public int FlightId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FlightName { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? BaggageInfo { get; set; }
        
        [Required]
        [Range(1, 1000)]
        public int NumberOfSeats { get; set; }
        
        [Required]
        public DateTime DepartureTime { get; set; }
        
        [Required]
        public DateTime ArrivalTime { get; set; }
        
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Fare { get; set; }
    }

    public class FlightDetailDto
    {
        public int FlightDetailId { get; set; }
        public int FlightId { get; set; }
        public int FlightOwnerId { get; set; }
        public string FlightName { get; set; } = string.Empty;
        public string? BaggageInfo { get; set; }
        public int NumberOfSeats { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Fare { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Flight information
        public string? FlightOrigin { get; set; }
        public string? FlightDestination { get; set; }
        public string? FlightRoute { get; set; }
    }

    public class CreateFlightOwnerDto
    {
        [Required]
        [StringLength(100)]
        public string AirlineName { get; set; } = string.Empty;
    }

    public class FlightOwnerDto
    {
        public int FlightOwnerId { get; set; }
        public int UserId { get; set; }
        public string AirlineName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }
}
