using System;
using System.Collections.Generic;

namespace CLab.Models
{
    public class RitenutaAcconto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }

        public string Intestazione { get; set; } = string.Empty;
        public string NumeroFattura { get; set; } = string.Empty;
        public DateTime DataFattura { get; set; }
        public DateTime? DataPagamentoFattura { get; set; }

        public decimal ImportoRitenuta { get; set; }
        public DateTime? ScadenzaVersamento { get; set; }
        public decimal? ImportoVersato { get; set; }

        /// <summary>Spuntato dalla griglia quando il versamento è avvenuto con ravvedimento operoso.</summary>
        public bool Ravvedimento { get; set; }

        public bool Versato => ImportoVersato.HasValue && ImportoVersato.Value > 0;

        /// <summary>"DaVersare" / "Versato" / "Anomalia" (importo versato diverso da quello trattenuto).</summary>
        public string StatoVersamento
        {
            get
            {
                if (!ImportoVersato.HasValue || ImportoVersato.Value == 0) return "DaVersare";
                return ImportoVersato.Value == ImportoRitenuta ? "Versato" : "Anomalia";
            }
        }

        public bool AnomaliaImporto => ImportoVersato.HasValue && ImportoVersato.Value != 0 && ImportoVersato.Value != ImportoRitenuta;

        public bool AnomaliaDataPagamentoFattura => DataPagamentoFattura.HasValue && DataPagamentoFattura.Value.Date < DataFattura.Date;

        public bool AnomaliaDataVersamento => ScadenzaVersamento.HasValue && DataPagamentoFattura.HasValue
            && ScadenzaVersamento.Value.Date < DataPagamentoFattura.Value.Date;

        public bool AnomaliaImportoSenzaData
        {
            get
            {
                bool importoPresente = ImportoVersato.HasValue && ImportoVersato.Value > 0;
                bool dataPresente = ScadenzaVersamento.HasValue;
                return importoPresente != dataPresente;
            }
        }

        public bool HaAnomalie => AnomaliaImporto || AnomaliaDataPagamentoFattura || AnomaliaDataVersamento || AnomaliaImportoSenzaData;

        /// <summary>Solo anomalie di data/coerenza: quella d'importo la segnala già il pallino di stato.</summary>
        public bool HaAnomalieData => AnomaliaDataPagamentoFattura || AnomaliaDataVersamento || AnomaliaImportoSenzaData;

        public string DettaglioAnomalieData
        {
            get
            {
                var elenco = new List<string>();
                if (AnomaliaDataPagamentoFattura) elenco.Add("Data pagamento fattura antecedente alla data di emissione");
                if (AnomaliaDataVersamento) elenco.Add("Data pagamento ritenuta antecedente al pagamento della fattura");
                if (AnomaliaImportoSenzaData) elenco.Add("Importo versato e data pagamento ritenuta non coerenti");
                return string.Join("\n", elenco);
            }
        }
    }
}