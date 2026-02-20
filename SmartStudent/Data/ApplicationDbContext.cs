/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  ApplicationDbContext.cs				            Date: 1/5/2026    │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│           This class defines the Entity Framework Core DbContext            │
│																			  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/


using Microsoft.EntityFrameworkCore;
using SmartStudent.Models;

namespace SmartStudent.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<Category> Categories { get; set; }
    }
}