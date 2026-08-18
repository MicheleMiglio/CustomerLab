using System;

namespace CLab.Models
{
    public class ClienteAttivita
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }

        public int AttivitaId { get; set; }
        public Attivita? Attivita { get; set; }

        public DateTime DataAssegnazione { get; set; } = DateTime.Now;
    }
}