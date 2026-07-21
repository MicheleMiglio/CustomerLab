using System;

namespace CLab.Models
{
    public class RitenutaAcconto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }

        public string NumeroFattura { get; set; } = string.Empty;
        public DateTime DataFattura { get; set; }
        public DateTime? DataPagamentoFattura { get; set; }

        public decimal ImportoRitenuta { get; set; }
        public DateTime? ScadenzaVersamento { get; set; }
        public decimal? ImportoVersato { get; set; }

        public bool Versato => ImportoVersato.HasValue && ImportoVersato.Value > 0;
    }
}