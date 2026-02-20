/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  Category.cs		                                Date: 1/2/2026    │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│                    Represents a user created category                       │
│																			  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/

using System.ComponentModel.DataAnnotations;

namespace SmartStudent.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
