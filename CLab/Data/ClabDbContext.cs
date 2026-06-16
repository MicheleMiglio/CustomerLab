using Microsoft.EntityFrameworkCore;
using CLab.Models;

namespace CLab.Data
{
    public class ClabDbContext : DbContext
    {
        public DbSet<Cliente> Clienti { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=clab.db");
        }
    }
}