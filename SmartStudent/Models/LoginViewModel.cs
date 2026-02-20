/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  LoginViewModel.cs				                    Date: 1/5/2026    │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│                    Model for user login credentials                         │
│																			  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/

using System.ComponentModel.DataAnnotations;

namespace SmartStudent.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
