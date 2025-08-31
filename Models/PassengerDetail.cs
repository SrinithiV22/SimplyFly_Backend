using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyFly.Api.Models
{
    public class PassengerDetail
    {
        [Key]
        public int PassengerId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int BookingId { get; set; } = 1; // Default value for now

        [MaxLength(10)]
        public string? SeatNo { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public int Age { get; set; }

        [Required]
        [MaxLength(10)]
        public string Gender { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PassportNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nationality { get; set; } = string.Empty;

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        
        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }
    }
}
