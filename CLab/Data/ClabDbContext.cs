using Microsoft.EntityFrameworkCore;
using CLab.Models;

namespace CLab.Data
{
    public class ClabDbContext : DbContext
    {
        public DbSet<Cliente> Clienti { get; set; }
        public DbSet<Contatti> Contatti { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string percorsoDb = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "clab.db");

            optionsBuilder.UseSqlite($"Data Source={percorsoDb}");
        }
    }
}