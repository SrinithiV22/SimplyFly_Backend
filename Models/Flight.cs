using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplyFly.Api.Models
{
    public class Flight
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Origin { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Destination { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
    }
}