using CLab.Models;
using System.Collections.ObjectModel;

namespace CLab.ViewModels.Dettaglio
{
    public class ClienteDettaglioViewModel : ViewModelBase
    {
        // FASE 3: vero quando l'utente altera qualcosa nel form dopo l'ultimo
        // caricamento/salvataggio. Lo legge SidePanelControl per la conferma
        // di chiusura con modifiche non salvate. Viene azzerato dal
        // ClientiViewModel a ogni CaricaNelForm e dopo il salvataggio.
        private bool _haModifiche;
        public bool HaModifiche
        {
            get => _haModifiche;
            set { _haModifiche = value; OnPropertyChanged(); }
        }

        private void SegnaModificato() => HaModifiche = true;

        private string _formRagioneSociale = string.Empty;
        public string FormRagioneSociale
        {
            get => _formRagioneSociale;
            set
            {
                _formRagioneSociale = value;
                OnPropertyChanged();
                SegnaModificato();
            }
        }

        private string? _formPartitaIva;
        public string? FormPartitaIva
        {
            get => _formPartitaIva;
            set
            {
                _formPartitaIva = value;
                OnPropertyChanged();
                SegnaModificato();
            }
        }

        private Referente? _formReferente;
        public Referente? FormReferente
        {
            get => _formReferente;
            set { _formReferente = value; OnPropertyChanged(); SegnaModificato(); }
        }

        private Programma? _formProgramma;
        public Programma? FormProgramma
        {
            get => _formProgramma;
            set { _formProgramma = value; OnPropertyChanged(); SegnaModificato(); }
        }

        private string? _formIntermediario;
        public string? FormIntermediario
        {
            get => _formIntermediario;
            set
            {
                _formIntermediario = value;
                OnPropertyChanged();
                SegnaModificato();
            }
        }

        private string? _formTipoContabilita;
        public string? FormTipoContabilita
        {
            get => _formTipoContabilita;
            set
            {
                _formTipoContabilita = value;
                OnPropertyChanged();
                SegnaModificato();
            }
        }

        private StatoCliente _formStato = StatoCliente.Attivo;
        public StatoCliente FormStato
        {
            get => _formStato;
            set
            {
                _formStato = value;
                OnPropertyChanged();
                SegnaModificato();
            }
        }

        // FASE 7: note del cliente, usate dalla scheda Situazione e preserve al salvataggio
        private string? _formNote;
        public string? FormNote
        {
            get => _formNote;
            set
            {
                _formNote = value;
                OnPropertyChanged();
                SegnaModificato();
            }
        }

        public ObservableCollection<Contatti> TelefoniCliente { get; } = new();

        public ObservableCollection<Contatti> EmailCliente { get; } = new();

        private string _nuovoTelefonoValore = string.Empty;
        public string NuovoTelefonoValore
        {
            get => _nuovoTelefonoValore;
            set
            {
                _nuovoTelefonoValore = value;
                OnPropertyChanged();
            }
        }

        private string? _nuovoTelefonoEtichetta;
        public string? NuovoTelefonoEtichetta
        {
            get => _nuovoTelefonoEtichetta;
            set
            {
                _nuovoTelefonoEtichetta = value;
                OnPropertyChanged();
            }
        }

        private string _nuovaEmailValore = string.Empty;
        public string NuovaEmailValore
        {
            get => _nuovaEmailValore;
            set
            {
                _nuovaEmailValore = value;
                OnPropertyChanged();
            }
        }

        private string? _nuovaEmailEtichetta;
        public string? NuovaEmailEtichetta
        {
            get => _nuovaEmailEtichetta;
            set
            {
                _nuovaEmailEtichetta = value;
                OnPropertyChanged();
            }
        }
    }
}