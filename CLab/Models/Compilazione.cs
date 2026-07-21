namespace CLab.Models
{
    public class Compilazione
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }

        public int AttivitaId { get; set; }
        public Attivita? Attivita { get; set; }

        public int Anno { get; set; }

        // Mensile: 1-12 · Trimestrale: 1-4 · Annuale: sempre 1
        public int Periodo { get; set; }

        public bool? ValoreBooleano { get; set; }
        public string? ValoreTesto { get; set; }
        public decimal? ValoreNumero { get; set; }

        public string? Commento { get; set; }
    }
}