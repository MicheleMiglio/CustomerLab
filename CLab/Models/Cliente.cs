using System.Collections.Generic;

namespace CLab.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        public string RagioneSociale { get; set; } = string.Empty;
        public string? PartitaIva { get; set; }
        public string? CodiceFiscale { get; set; }
        public string? Note { get; set; }
        public bool Attivo { get; set; } = true;

        public List<Contatti> Contatti { get; set; } = new List<Contatti>();
    }
}