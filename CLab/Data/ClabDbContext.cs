using Microsoft.EntityFrameworkCore;
using CLab.Models;

namespace CLab.Data
{
    public class ClabDbContext : DbContext
    {
        public DbSet<Cliente> Clienti { get; set; }
        public DbSet<Contatti> Contatti { get; set; }

        public DbSet<Attivita> Attivita { get; set; }
        public DbSet<AttivitaOpzione> OpzioniAttivita { get; set; }
        public DbSet<ClienteAttivita> ClientiAttivita { get; set; }
        public DbSet<Compilazione> Compilazioni { get; set; }
        public DbSet<RitenutaAcconto> RitenuteAcconto { get; set; }
        public DbSet<Fattura> Fatture { get; set; }
        public DbSet<Referente> Referenti { get; set; }
        public DbSet<Programma> Programmi { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string percorsoDb = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "clab.db");

            optionsBuilder.UseSqlite($"Data Source={percorsoDb}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Riferimenti a Cliente "a senso unico": Attività conosce Cliente,
            // Cliente non sa nulla di Attività (nessuna proprietà di navigazione
            // in Cliente.cs, il modulo Clienti resta autonomo).
            modelBuilder.Entity<ClienteAttivita>()
                .HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(ca => ca.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Compilazione>()
                .HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RitenutaAcconto>()
                .HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(r => r.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Fattura>()
                .HasOne<Referente>()
                .WithMany()
                .HasForeignKey(f => f.ReferenteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cliente>()
                .HasOne<Referente>()
                .WithMany()
                .HasForeignKey(c => c.ReferenteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cliente>()
                .HasOne<Programma>()
                .WithMany()
                .HasForeignKey(c => c.ProgrammaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Eliminare un'Attivita dal catalogo elimina a cascata le sue
            // opzioni, le assegnazioni ai clienti e tutte le compilazioni:
            // è la cancellazione "pesante" di cui avvisiamo l'utente prima.
            modelBuilder.Entity<AttivitaOpzione>()
                .HasOne<Attivita>()
                .WithMany(a => a.Opzioni)
                .HasForeignKey(o => o.AttivitaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClienteAttivita>()
                .HasOne(ca => ca.Attivita)
                .WithMany(a => a.ClientiAssegnati)
                .HasForeignKey(ca => ca.AttivitaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Compilazione>()
                .HasOne(c => c.Attivita)
                .WithMany()
                .HasForeignKey(c => c.AttivitaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Una sola compilazione per cliente + attività + anno + periodo
            modelBuilder.Entity<Compilazione>()
                .HasIndex(c => new { c.ClienteId, c.AttivitaId, c.Anno, c.Periodo })
                .IsUnique();

            // Un cliente non può avere la stessa attività assegnata due volte
            modelBuilder.Entity<ClienteAttivita>()
                .HasIndex(ca => new { ca.ClienteId, ca.AttivitaId })
                .IsUnique();
        }
    }
}