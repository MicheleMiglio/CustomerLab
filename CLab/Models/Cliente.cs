using System.Collections.Generic;

namespace CLab.Models
{
    public enum StatoCliente
    {
        Attivo,
        Cessato,
        StandBy
    }

    public class Cliente
    {
        public int Id { get; set; }
        public string RagioneSociale { get; set; } = string.Empty;
        public string? PartitaIva { get; set; }
        public string? CodiceFiscale { get; set; }
        public string? Referente { get; set; }
        public string? Note { get; set; }
        public StatoCliente Stato { get; set; } = StatoCliente.Attivo;

        public List<Contatti> Contatti { get; set; } = new List<Contatti>();
    }
}