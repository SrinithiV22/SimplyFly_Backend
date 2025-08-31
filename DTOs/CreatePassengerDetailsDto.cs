using System.ComponentModel.DataAnnotations;

namespace SimplyFly.Api.DTOs
{
    public class CreatePassengerDetailsDto
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public List<PassengerDetailDto> Passengers { get; set; } = new List<PassengerDetailDto>();
    }

    public class PassengerDetailDto
    {
        [MaxLength(10)]
        public string? SeatNo { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Range(1, 120)]
        public int Age { get; set; }

        [Required]
        [MaxLength(10)]
        public string Gender { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PassportNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nationality { get; set; } = string.Empty;
    }
}
