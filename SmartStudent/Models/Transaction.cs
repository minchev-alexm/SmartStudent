using System;
using System.ComponentModel.DataAnnotations;

namespace SmartStudent.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [StringLength(255)]
        public string? DocumentPath { get; set; }
    }
}