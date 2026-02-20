/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  Transaction.cs		                            Date: 1/7/2026    │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│               Represents a financial transaction for a user                 │
│																			  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace SmartStudent.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = null!;

        [StringLength(50)]
        public string? Category { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [StringLength(255)]
        public string? DocumentPath { get; set; }

        [NotMapped]
        public IFormFile? DocumentFile { get; set; }
    }
}
