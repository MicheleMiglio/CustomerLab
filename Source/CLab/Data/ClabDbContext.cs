using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

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
        public DbSet<Promemoria> Promemoria { get; set; }
        public DbSet<ToDo> ToDo { get; set; }
        public DbSet<ToDoSottoAttivita> ToDoSottoAttivita { get; set; }

        public static string PercorsoDatabase
        {
            get
            {
                string cartellaDb = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CLab");

                Directory.CreateDirectory(cartellaDb);

                return System.IO.Path.Combine(cartellaDb, "clab.db");
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={PercorsoDatabase}");
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

            // ToDo: collegamenti opzionali a Cliente/Referente, gestiti "a mano"
            // dal ViewModel (conferma popup + orfanamento dei completati) —
            // Restrict evita che EF cancelli o orfani i ToDo di nascosto.
            modelBuilder.Entity<ToDo>()
                .HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(t => t.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ToDo>()
                .HasOne<Referente>()
                .WithMany()
                .HasForeignKey(t => t.ReferenteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ToDoSottoAttivita>()
                .HasOne<ToDo>()
                .WithMany(t => t.SottoAttivita)
                .HasForeignKey(s => s.ToDoId)
                .OnDelete(DeleteBehavior.Cascade);

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