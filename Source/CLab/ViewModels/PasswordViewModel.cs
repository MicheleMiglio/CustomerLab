using CLab.Data;
using CLab.Models;
using CLab.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CLab.ViewModels
{
    /// <summary>
    /// Modulo Password: accesso (Fase 2), generatore/clipboard (Fase 3)
    /// e CRUD lista (Fase 4). La password della credenziale non transita
    /// da proprietà persistenti: vive nella PasswordBox della View.
    /// </summary>
    public class PasswordViewModel : ViewModelBase
    {
        private string _stato = "Bloccato";
        private string _messaggio = string.Empty;
        private string? _recoveryCodeMonouso;
        private bool _mostraRecoveryCode;
        private bool _isConfigurato;
        private bool _svuotando;

        public PasswordSessionService Sessione { get; } = PasswordSessionService.Istanza;

        public bool IsConfigurato
        {
            get => _isConfigurato;
            private set
            {
                _isConfigurato = value;
                OnPropertyChanged();
                NotificaStatiVista();
            }
        }

        public string Stato { get => _stato; private set { _stato = value; OnPropertyChanged(); } }
        public string Messaggio { get => _messaggio; private set { _messaggio = value; OnPropertyChanged(); } }

        /// <summary>Recovery Code mostrato UNA SOLA VOLTA dopo la configurazione.</summary>
        public string? RecoveryCodeMonouso
        {
            get => _recoveryCodeMonouso;
            private set
            {
                _recoveryCodeMonouso = value;
                MostraRecoveryCode = value != null;
                OnPropertyChanged();
            }
        }

        public bool MostraRecoveryCode { get => _mostraRecoveryCode; private set { _mostraRecoveryCode = value; OnPropertyChanged(); } }

        public bool MostraConfigurazione => !IsConfigurato;
        public bool MostraSblocco => IsConfigurato && !ModuloConfiguratoEAperto;
        public bool MostraElenco => ModuloConfiguratoEAperto;

        public ICommand ConfiguraCommand { get; }
        public ICommand SbloccaCommand { get; }
        public ICommand RecuperaCommand { get; }
        public ICommand LockCommand { get; }
        public ICommand ChiudiRecoveryCodeCommand { get; }
        public ICommand GeneraCommand { get; }
        public ICommand CopiaGenerataCommand { get; }
        public ICommand UsaGenerataCommand { get; }
        public ICommand AggiungiCommand { get; }
        public ICommand SalvaCommand { get; }
        public ICommand AnnullaCommand { get; }
        public ICommand TogglePasswordCommand { get; }
        public ICommand EliminaVoceCommand { get; }
        public ICommand ModificaVoceCommand { get; }
        public ICommand CopiaCampoCommand { get; }
        public ICommand CopiaRecoveryCodeCommand { get; }

        public PasswordViewModel()
        {
            _isConfigurato = PasswordAccessService.IsConfigurato();

            ConfiguraCommand = new RelayCommand(Configura);
            SbloccaCommand = new RelayCommand(Sblocca);
            RecuperaCommand = new RelayCommand(Recupera);
            LockCommand = new RelayCommand(SvuotaDatiSensibili);
            GeneraCommand = new RelayCommand(Genera);
            CopiaGenerataCommand = new RelayCommand(CopiaGenerata);
            UsaGenerataCommand = new RelayCommand(UsaGenerata);
            AggiungiCommand = new RelayCommand(Aggiungi);
            SalvaCommand = new RelayCommand(SalvaVoce, () => PuoSalvareVoce);
            AnnullaCommand = new RelayCommand(ChiudiPannello);
            TogglePasswordCommand = new RelayCommand(TogglePasswordForm);
            EliminaVoceCommand = new RelayCommand<PasswordVoceViewModel>(EliminaConConferma);
            ModificaVoceCommand = new RelayCommand<PasswordVoceViewModel>(ModificaVoce);
            CopiaCampoCommand = new RelayCommand<string>(CopiaCampo);
            CopiaRecoveryCodeCommand = new RelayCommand(CopiaRecoveryCode);
            ChiudiRecoveryCodeCommand = new RelayCommand(() => RecoveryCodeMonouso = null);

            AggiornaStato();

            // Sessione ancora aperta (es. rientro nel modulo entro i 15 minuti):
            // la lista si riapre subito, senza richiedere di nuovo la password.
            if (IsConfigurato && Sessione.SessioneValida())
                CaricaVoci();
        }

        /// <summary>Notifica lo stato corrente (chiamato dal timer UI).</summary>
        public void AggiornaStato()
        {
            if (_svuotando)
            {
                Stato = Sessione.IsUnlocked ? "Sbloccato" : "Bloccato";
                return;
            }

            if (!Sessione.VerificaTimeout())
            {
                Stato = "Bloccato";
                if (ModuloConfiguratoEAperto || PannelloAperto)
                    SvuotaDatiSensibili();
                return;
            }

            Stato = "Sbloccato";
        }

        public bool ProvaConfigurazione(string master, string conferma)
        {
            if (string.IsNullOrEmpty(master) || master.Length < 8)
            {
                Messaggio = "La password principale deve avere almeno 8 caratteri.";
                return false;
            }

            if (master != conferma)
            {
                Messaggio = "Le due password non coincidono.";
                return false;
            }

            if (!PasswordAccessService.ConfiguraIniziale(master, out var codice))
            {
                Messaggio = "Il modulo risulta già configurato.";
                return false;
            }

            RecoveryCodeMonouso = codice;
            IsConfigurato = true;
            SbloccaDopoOperazione(master);
            Messaggio = "Configurazione completata.";
            return true;
        }

        public bool ProvaSblocco(string master)
        {
            if (string.IsNullOrEmpty(master))
            {
                Messaggio = "Inserisci la password principale.";
                return false;
            }

            var config = PasswordAccessService.CaricaConfig();
            if (config == null || !PasswordAccessService.SbloccaConMaster(config, master, out var dek) || dek == null)
            {
                Messaggio = "Password principale non valida.";
                return false;
            }

            Sessione.Sblocca(dek);
            PasswordCryptoService.Azzera(dek);
            AggiornaStato();
            CaricaVoci();
            Messaggio = "Sessione sbloccata.";
            return true;
        }

        public bool ProvaRecupero(string recoveryCode, string nuovaMaster, string conferma)
        {
            if (string.IsNullOrWhiteSpace(recoveryCode))
            {
                Messaggio = "Inserisci il codice di recupero.";
                return false;
            }

            if (string.IsNullOrEmpty(nuovaMaster) || nuovaMaster.Length < 8)
            {
                Messaggio = "La nuova password principale deve avere almeno 8 caratteri.";
                return false;
            }

            if (nuovaMaster != conferma)
            {
                Messaggio = "Le due password non coincidono.";
                return false;
            }

            if (!PasswordAccessService.RecuperaConRecovery(recoveryCode, nuovaMaster))
            {
                Messaggio = "Codice di recupero non valido.";
                return false;
            }

            SbloccaDopoOperazione(nuovaMaster);
            Messaggio = "Password principale reimpostata. Sessione sbloccata.";
            return true;
        }

        private void SbloccaDopoOperazione(string master)
        {
            var config = PasswordAccessService.CaricaConfig();
            if (config != null && PasswordAccessService.SbloccaConMaster(config, master, out var dek) && dek != null)
            {
                Sessione.Sblocca(dek);
                PasswordCryptoService.Azzera(dek);
            }

            AggiornaStato();
            CaricaVoci();
        }

        private PasswordBox? _pwConfig;
        private PasswordBox? _pwConferma;
        private PasswordBox? _pwSblocco;
        private TextBox? _txtRecovery;
        private PasswordBox? _pwNuovaMaster;
        private PasswordBox? _pwNuovaConferma;
        private PasswordBox? _pwForm;
        private TextBox? _txtFormChiara;
        private PasswordBox? _pwGenerata;

        public void CollegaPasswordBox(
            PasswordBox? pwConfig,
            PasswordBox? pwConferma,
            PasswordBox? pwSblocco,
            TextBox? txtRecovery,
            PasswordBox? pwNuovaMaster,
            PasswordBox? pwNuovaConferma)
        {
            _pwConfig = pwConfig;
            _pwConferma = pwConferma;
            _pwSblocco = pwSblocco;
            _txtRecovery = txtRecovery;
            _pwNuovaMaster = pwNuovaMaster;
            _pwNuovaConferma = pwNuovaConferma;
        }

        public void CollegaControlliForm(PasswordBox? pwForm, TextBox? txtFormChiara, PasswordBox? pwGenerata)
        {
            _pwForm = pwForm;
            _txtFormChiara = txtFormChiara;
            _pwGenerata = pwGenerata;
        }

        private void Configura()
        {
            if (_pwConfig == null || _pwConferma == null)
                return;

            ProvaConfigurazione(_pwConfig.Password, _pwConferma.Password);
            _pwConfig.Clear();
            _pwConferma.Clear();
        }

        private void Sblocca()
        {
            if (_pwSblocco == null)
                return;

            ProvaSblocco(_pwSblocco.Password);
            _pwSblocco.Clear();
        }

        private void Recupera()
        {
            if (_txtRecovery == null || _pwNuovaMaster == null || _pwNuovaConferma == null)
                return;

            ProvaRecupero(_txtRecovery.Text, _pwNuovaMaster.Password, _pwNuovaConferma.Password);
            _txtRecovery.Clear();
            _pwNuovaMaster.Clear();
            _pwNuovaConferma.Clear();
        }

        private void Aggiungi()
        {
            if (!Sessione.SessioneValida()) { SvuotaDatiSensibili(); return; }

            _voceInModificaId = 0;
            TitoloPannello = "Nuova password";
            FormNome = string.Empty;
            FormSito = string.Empty;
            FormUsername = string.Empty;
            FormNote = string.Empty;
            VoceFeedback = string.Empty;
            FeedbackCopia = string.Empty;
            FeedbackGeneratore = string.Empty;
            FeedbackClipboard = string.Empty;
            PasswordFormVisibile = false;
            PulisciPasswordForm();
            HaModifiche = false;
            PannelloAperto = true;
        }

        public void ChiudiPannello()
        {
            PannelloAperto = false;
            PasswordFormVisibile = false;
            PulisciPasswordForm();
            VoceFeedback = string.Empty;
            FeedbackCopia = string.Empty;
            FeedbackGeneratore = string.Empty;
            FeedbackClipboard = string.Empty;

            // Ultimo: la pulizia dei campi scatena il PasswordChanged della
            // PasswordBox e non deve risultare una modifica non salvata.
            HaModifiche = false;
        }

        public bool ProvaSalvaVoce(string nome, string sito, string username, string password, string note)
        {
            var dek = PasswordSessionService.Istanza.TryGetDek();
            if (dek == null) { SvuotaDatiSensibili(); return false; }

            try
            {
                if (string.IsNullOrWhiteSpace(nome))
                {
                    VoceFeedback = "Il nome è obbligatorio.";
                    return false;
                }

                using var db = new ClabDbContext();

                Password riga;
                if (_voceInModificaId == 0)
                {
                    riga = new Password { CreatedAt = DateTime.Now };
                    db.Passwords.Add(riga);
                }
                else
                {
                    riga = db.Passwords.First(p => p.Id == _voceInModificaId);
                }

                var payload = new PasswordPayload
                {
                    Nome = nome.Trim(),
                    Sito = sito.Trim(),
                    Username = username.Trim(),
                    Password = password,
                    Note = note.Trim()
                };
                var (cifrato, nonce, tag) = PasswordCryptoService.CifraPayload(payload, dek);
                payload.Password = string.Empty;
                riga.PayloadCifrato = cifrato;
                riga.Nonce = nonce;
                riga.Tag = tag;
                riga.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                ChiudiPannello();
                CaricaVoci();
                return true;
            }
            catch (Exception)
            {
                VoceFeedback = "Salvataggio non riuscito. Riprova.";
                return false;
            }
            finally
            {
                PasswordCryptoService.Azzera(dek);
            }
        }

        public void ModificaVoce(PasswordVoceViewModel? voce)
        {
            if (voce == null) return;

            var dek = PasswordSessionService.Istanza.TryGetDek();
            if (dek == null) { SvuotaDatiSensibili(); return; }

            try
            {
                using var db = new ClabDbContext();
                var riga = db.Passwords.AsNoTracking().FirstOrDefault(p => p.Id == voce.Id);
                if (riga == null)
                {
                    VoceFeedback = "Voce non trovata.";
                    return;
                }

                PasswordPayload payload;
                try
                {
                    payload = PasswordCryptoService.DecifraPayload(riga.PayloadCifrato, dek, riga.Nonce, riga.Tag);
                }
                catch (Exception)
                {
                    VoceFeedback = "Impossibile aprire questa voce.";
                    return;
                }

                _voceInModificaId = voce.Id;
                TitoloPannello = "Modifica password";
                FormNome = payload.Nome;
                FormSito = payload.Sito;
                FormUsername = payload.Username;
                FormNote = payload.Note;
                VoceFeedback = string.Empty;
                FeedbackCopia = string.Empty;
                FeedbackGeneratore = string.Empty;
                FeedbackClipboard = string.Empty;
                PasswordFormVisibile = false;

                if (_pwForm != null)
                    _pwForm.Password = payload.Password;
                if (_txtFormChiara != null)
                    _txtFormChiara.Clear();

                payload.Password = string.Empty;
                HaModifiche = false;
                PannelloAperto = true;
            }
            finally
            {
                PasswordCryptoService.Azzera(dek);
            }
        }

        public bool EliminaVoce(PasswordVoceViewModel? voce)
        {
            var dek = PasswordSessionService.Istanza.TryGetDek();
            if (dek == null) { SvuotaDatiSensibili(); return false; }

            try
            {
                if (voce == null) return false;

                using var db = new ClabDbContext();
                var riga = db.Passwords.FirstOrDefault(p => p.Id == voce.Id);
                if (riga != null)
                {
                    db.Passwords.Remove(riga);
                    db.SaveChanges();
                }

                _vociComplete.Remove(voce);
                ApplicaFiltro();
                if (_voceInModificaId == voce.Id)
                    ChiudiPannello();
                return true;
            }
            finally
            {
                PasswordCryptoService.Azzera(dek);
            }
        }

        private void EliminaConConferma(PasswordVoceViewModel? voce)
        {
            if (voce == null) return;
            if (!Sessione.SessioneValida()) { SvuotaDatiSensibili(); return; }

            var esito = MessageBox.Show(
                $"Eliminare la password \"{voce.Nome}\"?",
                "Conferma eliminazione",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (esito != MessageBoxResult.Yes)
                return;

            EliminaVoce(voce);
        }

        private void TogglePasswordForm()
        {
            if (!Sessione.SessioneValida()) { SvuotaDatiSensibili(); return; }

            if (!PasswordFormVisibile)
            {
                if (_txtFormChiara != null && _pwForm != null)
                    _txtFormChiara.Text = _pwForm.Password;
                PasswordFormVisibile = true;
            }
            else
            {
                if (_pwForm != null && _txtFormChiara != null)
                    _pwForm.Password = _txtFormChiara.Text;
                _txtFormChiara?.Clear();
                PasswordFormVisibile = false;
            }
        }

        private void CopiaCampo(string? campo)
        {
            if (!Sessione.SessioneValida()) { SvuotaDatiSensibili(); return; }

            var testo = campo switch
            {
                "nome" => FormNome,
                "sito" => FormSito,
                "username" => FormUsername,
                "note" => FormNote,
                "password" => LeggiPasswordForm(),
                _ => null
            };

            if (string.IsNullOrEmpty(testo))
            {
                FeedbackCopia = "Niente da copiare.";
                return;
            }

            var copiata = PasswordClipboardService.IstanzaApplicativo.Copia(testo);
            FeedbackCopia = copiata
                ? "Copiato — se è una password verrà cancellata dagli appunti tra 30 secondi."
                : "Sessione bloccata o scaduta: sblocca il modulo per copiare.";
        }

        /// <summary>Copia il codice di recupero mostrato una sola volta: passa
        /// dallo stesso servizio appunti del modulo, quindi anche questo valore
        /// viene cancellato dagli appunti dopo 30 secondi.</summary>
        private void CopiaRecoveryCode()
        {
            if (string.IsNullOrEmpty(_recoveryCodeMonouso))
                return;

            PasswordClipboardService.IstanzaApplicativo.Copia(_recoveryCodeMonouso);
        }

        private string _generata = string.Empty;
        public string Generata
        {
            get => _generata;
            private set { _generata = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerataVisibile)); }
        }

        public bool GenerataVisibile => !string.IsNullOrEmpty(_generata);

        private string _feedbackGeneratore = string.Empty;
        public string FeedbackGeneratore
        {
            get => _feedbackGeneratore;
            private set { _feedbackGeneratore = value; OnPropertyChanged(); }
        }

        private string _feedbackClipboard = string.Empty;
        public string FeedbackClipboard
        {
            get => _feedbackClipboard;
            private set { _feedbackClipboard = value; OnPropertyChanged(); }
        }

        private string _feedbackCopia = string.Empty;
        public string FeedbackCopia
        {
            get => _feedbackCopia;
            private set { _feedbackCopia = value; OnPropertyChanged(); }
        }

        private int _lunghezza = PasswordGeneratorService.LunghezzaDefault;

        /// <summary>Lunghezza della password generata (8-64, default 20).</summary>
        public int Lunghezza
        {
            get => _lunghezza;
            set
            {
                _lunghezza = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LunghezzaTesto));
            }
        }

        /// <summary>Testo accanto allo slider della lunghezza.</summary>
        public string LunghezzaTesto =>
            $"{_lunghezza} caratteri (da {PasswordGeneratorService.LunghezzaMinima} a {PasswordGeneratorService.LunghezzaMassima})";

        private bool _usaMaiuscole = true;
        public bool UsaMaiuscole { get => _usaMaiuscole; set { _usaMaiuscole = value; OnPropertyChanged(); } }

        private bool _usaMinuscole = true;
        public bool UsaMinuscole { get => _usaMinuscole; set { _usaMinuscole = value; OnPropertyChanged(); } }

        private bool _usaNumeri = true;
        public bool UsaNumeri { get => _usaNumeri; set { _usaNumeri = value; OnPropertyChanged(); } }

        private bool _usaSimboli = true;
        public bool UsaSimboli { get => _usaSimboli; set { _usaSimboli = value; OnPropertyChanged(); } }

        public bool ProvaGenera(int lunghezza, bool maiuscole, bool minuscole, bool numeri, bool simboli, out string? generata)
        {
            generata = PasswordGeneratorService.Genera(lunghezza, maiuscole, minuscole, numeri, simboli, out var errore);

            if (generata == null)
            {
                FeedbackGeneratore = errore ?? "Generazione non riuscita.";
                return false;
            }

            Generata = generata;
            FeedbackGeneratore = $"Generata ({lunghezza} caratteri).";
            return true;
        }

        private void Genera()
        {
            if (!Sessione.SessioneValida()) { SvuotaDatiSensibili(); return; }
            if (_pwGenerata == null)
                return;

            if (ProvaGenera(Lunghezza, UsaMaiuscole, UsaMinuscole, UsaNumeri, UsaSimboli, out var generata) && generata != null)
                _pwGenerata.Password = generata;
            else
                _pwGenerata.Clear();
        }

        public void CopiaGenerata()
        {
            if (string.IsNullOrEmpty(_generata))
            {
                FeedbackClipboard = "Nessuna password da copiare.";
                return;
            }

            var copiata = PasswordClipboardService.IstanzaApplicativo.Copia(_generata);
            FeedbackClipboard = copiata
                ? "Copiata — verrà cancellata dagli appunti tra 30 secondi."
                : "Sessione bloccata o scaduta: sblocca il modulo per copiare.";
        }

        private void UsaGenerata()
        {
            if (!Sessione.SessioneValida()) { SvuotaDatiSensibili(); return; }
            if (string.IsNullOrEmpty(_generata))
            {
                FeedbackGeneratore = "Genera prima una password.";
                return;
            }

            if (PasswordFormVisibile)
            {
                if (_txtFormChiara != null)
                    _txtFormChiara.Text = _generata;
            }
            else if (_pwForm != null)
            {
                _pwForm.Password = _generata;
            }

            SegnaModificato();
        }

        public ObservableCollection<PasswordVoceViewModel> Voci { get; } = new();

        private string _filtroTesto = string.Empty;
        public string FiltroTesto
        {
            get => _filtroTesto;
            set { _filtroTesto = value; OnPropertyChanged(); ApplicaFiltro(); }
        }

        private readonly List<PasswordVoceViewModel> _vociComplete = new();

        private bool _moduloConfiguratoEAperto;
        public bool ModuloConfiguratoEAperto
        {
            get => _moduloConfiguratoEAperto;
            private set
            {
                _moduloConfiguratoEAperto = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBloccato));
                NotificaStatiVista();
            }
        }

        public bool IsBloccato => !ModuloConfiguratoEAperto;

        private bool _pannelloAperto;
        public bool PannelloAperto
        {
            get => _pannelloAperto;
            set { _pannelloAperto = value; OnPropertyChanged(); }
        }

        private bool _haModifiche;
        public bool HaModifiche { get => _haModifiche; set { _haModifiche = value; OnPropertyChanged(); } }
        public void SegnaModificato() => HaModifiche = true;

        private string _titoloPannello = "Nuova password";
        public string TitoloPannello { get => _titoloPannello; private set { _titoloPannello = value; OnPropertyChanged(); } }

        private string _voceFeedback = string.Empty;
        public string VoceFeedback { get => _voceFeedback; private set { _voceFeedback = value; OnPropertyChanged(); } }

        private int _voceInModificaId;
        private string _formNome = string.Empty;
        public string FormNome
        {
            get => _formNome;
            set { _formNome = value; OnPropertyChanged(); OnPropertyChanged(nameof(PuoSalvareVoce)); SegnaModificato(); }
        }

        private string _formSito = string.Empty;
        public string FormSito { get => _formSito; set { _formSito = value; OnPropertyChanged(); SegnaModificato(); } }

        private string _formUsername = string.Empty;
        public string FormUsername { get => _formUsername; set { _formUsername = value; OnPropertyChanged(); SegnaModificato(); } }

        private string _formNote = string.Empty;
        public string FormNote { get => _formNote; set { _formNote = value; OnPropertyChanged(); SegnaModificato(); } }

        public bool PuoSalvareVoce => !string.IsNullOrWhiteSpace(_formNome);

        private bool _passwordFormVisibile;
        public bool PasswordFormVisibile
        {
            get => _passwordFormVisibile;
            private set
            {
                _passwordFormVisibile = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PasswordFormMascherata));
                OnPropertyChanged(nameof(EtichettaTogglePassword));
            }
        }

        public bool PasswordFormMascherata => !_passwordFormVisibile;
        public string EtichettaTogglePassword => PasswordFormVisibile ? "Nascondi" : "Mostra";

        public bool ElencoVuoto => Voci.Count == 0;

        public string EmptyStateTesto =>
            string.IsNullOrWhiteSpace(FiltroTesto)
                ? "Nessuna password salvata."
                : $"Nessuna password corrisponde alla ricerca \"{FiltroTesto.Trim()}\".";

        public void CaricaVoci()
        {
            var dek = PasswordSessionService.Istanza.TryGetDek();
            if (dek == null)
            {
                SvuotaDatiSensibili();
                return;
            }

            try
            {
                _vociComplete.Clear();

                using var db = new ClabDbContext();
                var righe = db.Passwords.AsNoTracking().OrderBy(p => p.UpdatedAt).ToList();

                foreach (var riga in righe)
                {
                    try
                    {
                        var payload = PasswordCryptoService.DecifraPayload(riga.PayloadCifrato, dek, riga.Nonce, riga.Tag);
                        _vociComplete.Add(new PasswordVoceViewModel
                        {
                            Id = riga.Id,
                            Nome = payload.Nome,
                            Sito = payload.Sito,
                            Username = payload.Username,
                            Note = payload.Note,
                            CreatedAt = riga.CreatedAt,
                            UpdatedAt = riga.UpdatedAt
                        });
                        payload.Password = string.Empty;
                    }
                    catch (Exception)
                    {
                    }
                }

                _vociComplete.Sort((a, b) => string.Compare(a.Nome, b.Nome, StringComparison.OrdinalIgnoreCase));
                ApplicaFiltro();
                ModuloConfiguratoEAperto = true;
            }
            finally
            {
                PasswordCryptoService.Azzera(dek);
            }
        }

        private void ApplicaFiltro()
        {
            Voci.Clear();
            var filtro = _filtroTesto?.Trim() ?? string.Empty;

            foreach (var v in _vociComplete)
            {
                if (filtro.Length == 0 ||
                    v.Nome.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    v.Sito.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    v.Username.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    v.Note.Contains(filtro, StringComparison.OrdinalIgnoreCase))
                {
                    Voci.Add(v);
                }
            }

            OnPropertyChanged(nameof(ElencoVuoto));
            OnPropertyChanged(nameof(EmptyStateTesto));
        }

        public void SvuotaDatiSensibili()
        {
            if (_svuotando)
                return;

            _svuotando = true;
            try
            {
                PasswordSessionService.Istanza.Lock();

                PannelloAperto = false;
                PasswordFormVisibile = false;
                PulisciPasswordForm();
                _voceInModificaId = 0;
                FormNome = string.Empty;
                FormSito = string.Empty;
                FormUsername = string.Empty;
                FormNote = string.Empty;
                VoceFeedback = string.Empty;
                FeedbackCopia = string.Empty;
                FeedbackGeneratore = string.Empty;
                FeedbackClipboard = string.Empty;
                Generata = string.Empty;
                // Il codice di recupero è un segreto come gli altri: al lock
                // sparisce anche dalla schermata che lo mostra una sola volta.
                RecoveryCodeMonouso = null;
                _vociComplete.Clear();
                Voci.Clear();
                if (!string.IsNullOrEmpty(_filtroTesto))
                    FiltroTesto = string.Empty;
                else
                    ApplicaFiltro();

                ModuloConfiguratoEAperto = false;
                PulisciPasswordBoxAccesso();
                Stato = "Bloccato";

                // Ultimo: la pulizia dei campi segna "modificato" (segna che
                // l'utente ha toccato i valori), quindi al lock lo stato delle
                // modifiche non salvate deve restare pulito.
                HaModifiche = false;
            }
            finally
            {
                _svuotando = false;
            }
        }

        public void SegnaPasswordFormModificata()
        {
            HaModifiche = true;
        }

        private void SalvaVoce()
        {
            if (!Sessione.SessioneValida()) { SvuotaDatiSensibili(); return; }

            var password = LeggiPasswordForm();

            // La password può restare volutamente vuota, ma solo dopo una
            // conferma esplicita. "No" (default) annulla: il form resta
            // aperto con tutti i dati inseriti, nessun salvataggio.
            if (string.IsNullOrEmpty(password))
            {
                var esito = MessageBox.Show(
                    "Non hai impostato una password per questa credenziale. Vuoi salvare comunque?",
                    "Password non impostata",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (esito != MessageBoxResult.Yes)
                    return;
            }

            ProvaSalvaVoce(FormNome, FormSito, FormUsername, password, FormNote);
        }

        private string LeggiPasswordForm()
        {
            if (PasswordFormVisibile)
                return _txtFormChiara?.Text ?? string.Empty;
            return _pwForm?.Password ?? string.Empty;
        }

        private void PulisciPasswordForm()
        {
            _pwForm?.Clear();
            _txtFormChiara?.Clear();
            _pwGenerata?.Clear();
        }

        private void PulisciPasswordBoxAccesso()
        {
            _pwConfig?.Clear();
            _pwConferma?.Clear();
            _pwSblocco?.Clear();
            _txtRecovery?.Clear();
            _pwNuovaMaster?.Clear();
            _pwNuovaConferma?.Clear();
        }

        private void NotificaStatiVista()
        {
            OnPropertyChanged(nameof(MostraConfigurazione));
            OnPropertyChanged(nameof(MostraSblocco));
            OnPropertyChanged(nameof(MostraElenco));
        }
    }
}
