using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartStudent.Models
{
    public class Budget
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Planned { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Actual { get; set; }
    }
}
