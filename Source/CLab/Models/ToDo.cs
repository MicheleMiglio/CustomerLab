using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CLab.Models
{
    public enum PrioritaToDo
    {
        Bassa = 0,
        Media = 1,
        Alta = 2
    }

    /// <summary>
    /// Un'attività operativa con stato fatto/non fatto. A differenza dei
    /// Promemoria (post-it effimeri, si buttano) i ToDo restano nello storico
    /// anche da completati; a differenza delle Attività non sono una
    /// configurazione ricorrente per cliente ma un'istanza singola di lavoro.
    /// </summary>
    public class ToDo : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Titolo { get; set; } = string.Empty;
        public string? Descrizione { get; set; }

        public bool Completato { get; set; }
        public DateTime? DataCompletamento { get; set; }

        public DateTime? DataScadenza { get; set; }
        public PrioritaToDo Priorita { get; set; } = PrioritaToDo.Media;

        public DateTime DataCreazione { get; set; } = DateTime.Now;

        /// <summary>Riservato per un futuro riordino manuale; non usato nell'ordinamento di default.</summary>
        public int Ordine { get; set; }

        public int? ClienteId { get; set; }
        public int? ReferenteId { get; set; }

        /// <summary>
        /// Snapshot della ragione sociale, valorizzato SOLO quando un ToDo
        /// completato viene "orfanato" perché il cliente collegato è stato
        /// eliminato: mantiene il contesto storico anche senza il record.
        /// </summary>
        public string? ClienteNomeStorico { get; set; }

        public List<ToDoSottoAttivita> SottoAttivita { get; set; } = new();

        // Non mappate: valorizzate a runtime dal ViewModel dopo il caricamento
        // (join con Clienti/Referenti), servono solo per lista e ricerca testuale.
        [NotMapped]
        public string ClienteNome { get; set; } = string.Empty;
        [NotMapped]
        public string ReferenteNome { get; set; } = string.Empty;

        [NotMapped]
        public int SottoAttivitaTotali => SottoAttivita.Count;
        [NotMapped]
        public int SottoAttivitaCompletate => SottoAttivita.Count(s => s.Completato);
        [NotMapped]
        public bool HaSottoAttivita => SottoAttivita.Count > 0;

        /// <summary>In riga il chip Referente si vede solo se manca il Cliente (altrimenti l'informazione è ridondante).</summary>
        [NotMapped]
        public bool MostraChipReferente => string.IsNullOrEmpty(ClienteNome) && !string.IsNullOrEmpty(ReferenteNome);

        [NotMapped]
        public string CollegamentoDisplay =>
            !string.IsNullOrEmpty(ClienteNome)
                ? ClienteNome
                : (!string.IsNullOrEmpty(ReferenteNome) ? ReferenteNome : "—");

        [NotMapped]
        public string ScadenzaDisplay => DataScadenza.HasValue
            ? DataScadenza.Value.ToString("dd/MM/yyyy")
            : "—";

        [NotMapped]
        public string CompletamentoDisplay => Completato && DataCompletamento.HasValue
            ? DataCompletamento.Value.ToString("dd/MM/yyyy")
            : "—";

        [NotMapped]
        public string PassiDisplay => HaSottoAttivita
            ? $"{SottoAttivitaCompletate}/{SottoAttivitaTotali}"
            : "—";

        [NotMapped]
        public bool IsScaduto => DataScadenza.HasValue && DataScadenza.Value.Date < DateTime.Today && !Completato;

        // Stato di espansione della riga (mostra/nasconde le sotto-attività
        // inline). È solo UI, non persistito: implementa INotifyPropertyChanged
        // così può cambiare a schermo senza dover ricaricare tutta la lista dal db.
        private bool _isEspanso;
        [NotMapped]
        public bool IsEspanso
        {
            get => _isEspanso;
            set
            {
                if (_isEspanso == value) return;
                _isEspanso = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MostraSottoAttivitaEspanse));
            }
        }

        [NotMapped]
        public bool MostraSottoAttivitaEspanse => HaSottoAttivita && IsEspanso;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? nome = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }

    public class ToDoSottoAttivita
    {
        public int Id { get; set; }
        public int ToDoId { get; set; }
        public string Testo { get; set; } = string.Empty;
        public bool Completato { get; set; }
        public int Ordine { get; set; }
    }
}
