/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  User.cs				                            Date: 1/4/2026    │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│                  Represents a registered application user                   │
│																			  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/

using System.ComponentModel.DataAnnotations;

namespace SmartStudent.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string PasswordHash { get; set; }
    }
}
