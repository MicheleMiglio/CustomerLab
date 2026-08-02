using System;
using System.Collections.Generic;

namespace CLab.Models
{
    public class Fattura
    {
        public int Id { get; set; }
        public int? ReferenteId { get; set; }
        public string Numero { get; set; } = string.Empty;
        public DateTime DataEmissione { get; set; }
        public decimal Importo { get; set; }

        public DateTime? DataScadenza { get; set; }
        public DateTime? DataPagamento { get; set; }

        public bool Annullata { get; set; }
        public string? Nota { get; set; }

        public bool Pagata => DataPagamento.HasValue;

        /// <summary>"Emessa" / "Pagata" / "Scaduta" / "Annullata" — calcolato, mai scelto a mano tranne Annullata.</summary>
        public string Stato
        {
            get
            {
                if (Annullata) return "Annullata";
                if (Pagata) return "Pagata";
                if (DataScadenza.HasValue && DataScadenza.Value.Date < DateTime.Now.Date) return "Scaduta";
                return "Emessa";
            }
        }

        public bool AnomaliaPagamentoPrimaEmissione => DataPagamento.HasValue && DataPagamento.Value.Date < DataEmissione.Date;
        public bool AnomaliaScadenzaPrimaEmissione => DataScadenza.HasValue && DataScadenza.Value.Date < DataEmissione.Date;
        public bool HaAnomalie => AnomaliaPagamentoPrimaEmissione || AnomaliaScadenzaPrimaEmissione;

        public string DettaglioAnomalie
        {
            get
            {
                var elenco = new List<string>();
                if (AnomaliaPagamentoPrimaEmissione) elenco.Add("Data pagamento antecedente alla data di emissione");
                if (AnomaliaScadenzaPrimaEmissione) elenco.Add("Data scadenza antecedente alla data di emissione");
                return string.Join("\n", elenco);
            }
        }
    }
}