using CLab.Data;
using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace CLab.ViewModels
{
    public class ClientiViewModel : ViewModelBase
    {
        public ObservableCollection<Cliente> Clienti { get; set; }

        private Cliente? _clienteSelezionato;
        public Cliente? ClienteSelezionato
        {
            get => _clienteSelezionato;
            set
            {
                _clienteSelezionato = value;
                OnPropertyChanged();
                CaricaNelForm(value);
            }
        }

        private bool _overlayAperto;
        public bool OverlayAperto
        {
            get => _overlayAperto;
            set { _overlayAperto = value; OnPropertyChanged(); }
        }

        private bool _modalitaModifica;
        public bool ModalitaModifica
        {
            get => _modalitaModifica;
            set { _modalitaModifica = value; OnPropertyChanged(); }
        }

        // --- TELEFONI ---
        public ObservableCollection<Contatti> TelefoniCliente { get; set; } = new ObservableCollection<Contatti>();

        private int _telefonoInModificaId = 0;
        private string _nuovoTelefonoValore = string.Empty;
        public string NuovoTelefonoValore
        {
            get => _nuovoTelefonoValore;
            set { _nuovoTelefonoValore = value; OnPropertyChanged(); }
        }

        private string? _nuovoTelefonoEtichetta;
        public string? NuovoTelefonoEtichetta
        {
            get => _nuovoTelefonoEtichetta;
            set { _nuovoTelefonoEtichetta = value; OnPropertyChanged(); }
        }

        // --- EMAIL ---
        public ObservableCollection<Contatti> EmailCliente { get; set; } = new ObservableCollection<Contatti>();

        private int _emailInModificaId = 0;
        private string _nuovaEmailValore = string.Empty;
        public string NuovaEmailValore
        {
            get => _nuovaEmailValore;
            set { _nuovaEmailValore = value; OnPropertyChanged(); }
        }

        private string? _nuovaEmailEtichetta;
        public string? NuovaEmailEtichetta
        {
            get => _nuovaEmailEtichetta;
            set { _nuovaEmailEtichetta = value; OnPropertyChanged(); }
        }

        // Campi del form
        private int _formId;
        private string _formRagioneSociale = string.Empty;
        public string FormRagioneSociale
        {
            get => _formRagioneSociale;
            set { _formRagioneSociale = value; OnPropertyChanged(); }
        }

        private string? _formPartitaIva;
        public string? FormPartitaIva
        {
            get => _formPartitaIva;
            set { _formPartitaIva = value; OnPropertyChanged(); }
        }

        private string? _formReferente;
        public string? FormReferente
        {
            get => _formReferente;
            set { _formReferente = value; OnPropertyChanged(); }
        }

        private StatoCliente _formStato = StatoCliente.Attivo;
        public StatoCliente FormStato
        {
            get => _formStato;
            set { _formStato = value; OnPropertyChanged(); }
        }

        // Comandi
        public RelayCommand NuovoCommand { get; }
        public RelayCommand SalvaCommand { get; }
        public RelayCommand SalvaTelefonoCommand { get; }
        public RelayCommand<Contatti> SelezionaTelefonoCommand { get; }
        public RelayCommand<Contatti> RimuoviTelefonoCommand { get; }
        public RelayCommand<Contatti> ImpostaTelefonoPrincipaleCommand { get; }
        public RelayCommand SalvaEmailCommand { get; }
        public RelayCommand<Contatti> SelezionaEmailCommand { get; }
        public RelayCommand<Contatti> RimuoviEmailCommand { get; }
        public RelayCommand<Contatti> ImpostaEmailPrincipaleCommand { get; }
        public RelayCommand<Cliente> ApriDettaglioCommand { get; }
        public RelayCommand<Cliente> EliminaClienteCommand { get; }
        public RelayCommand ChiudiOverlayCommand { get; }
        public RelayCommand AttivaModificaCommand { get; }

        public ClientiViewModel()
        {
            Clienti = new ObservableCollection<Cliente>();
            CaricaClienti();

            NuovoCommand = new RelayCommand(Nuovo);
            SalvaCommand = new RelayCommand(Salva);
            SalvaTelefonoCommand = new RelayCommand(SalvaTelefono, () => !string.IsNullOrWhiteSpace(NuovoTelefonoValore));
            SelezionaTelefonoCommand = new RelayCommand<Contatti>(SelezionaTelefono);
            RimuoviTelefonoCommand = new RelayCommand<Contatti>(RimuoviTelefono);
            ImpostaTelefonoPrincipaleCommand = new RelayCommand<Contatti>(ImpostaTelefonoPrincipale);

            SalvaEmailCommand = new RelayCommand(SalvaEmail, () => !string.IsNullOrWhiteSpace(NuovaEmailValore));
            SelezionaEmailCommand = new RelayCommand<Contatti>(SelezionaEmail);
            RimuoviEmailCommand = new RelayCommand<Contatti>(RimuoviEmail);
            ImpostaEmailPrincipaleCommand = new RelayCommand<Contatti>(ImpostaEmailPrincipale);
            ApriDettaglioCommand = new RelayCommand<Cliente>(ApriDettaglio);
            EliminaClienteCommand = new RelayCommand<Cliente>(EliminaCliente);
            ChiudiOverlayCommand = new RelayCommand(ChiudiOverlay);
            AttivaModificaCommand = new RelayCommand(() => ModalitaModifica = true);
        }

        private void CaricaClienti()
        {
            using (var db = new ClabDbContext())
            {
                var lista = db.Clienti.Include(c => c.Contatti).ToList();
                Clienti.Clear();
                foreach (var c in lista)
                {
                    Clienti.Add(c);
                }
            }
        }

        private void CaricaNelForm(Cliente? cliente)
        {
            TelefoniCliente.Clear();
            EmailCliente.Clear();
            AnnullaModificaTelefono();
            AnnullaModificaEmail();

            if (cliente == null)
            {
                _formId = 0;
                FormRagioneSociale = string.Empty;
                FormPartitaIva = null;
                FormReferente = null;
                FormStato = StatoCliente.Attivo;
            }
            else
            {
                _formId = cliente.Id;
                FormRagioneSociale = cliente.RagioneSociale;
                FormPartitaIva = cliente.PartitaIva;
                FormReferente = cliente.Referente;
                FormStato = cliente.Stato;

                using (var db = new ClabDbContext())
                {
                    var contatti = db.Contatti.Where(c => c.ClienteId == cliente.Id).ToList();
                    foreach (var contatto in contatti)
                    {
                        if (contatto.Tipo == TipoContatto.Telefono)
                            TelefoniCliente.Add(contatto);
                        else
                            EmailCliente.Add(contatto);
                    }
                }
            }
        }

        private void Nuovo()
        {
            ClienteSelezionato = null;
            CaricaNelForm(null);
            ModalitaModifica = true;
            OverlayAperto = true;
        }

        private void Salva()
        {
            if (string.IsNullOrWhiteSpace(FormRagioneSociale))
            {
                MessageBox.Show("La Ragione Sociale è obbligatoria.", "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidaPartitaIvaCodiceFiscale(FormPartitaIva, out string erroreIva))
            {
                MessageBox.Show(erroreIva, "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new ClabDbContext())
            {
                Cliente clienteSalvato;

                if (_formId == 0)
                {
                    var nuovo = new Cliente
                    {
                        RagioneSociale = FormRagioneSociale,
                        PartitaIva = FormPartitaIva,
                        Referente = FormReferente,
                        Stato = FormStato
                    };
                    db.Clienti.Add(nuovo);
                    db.SaveChanges();

                    clienteSalvato = nuovo;
                }
                else
                {
                    var esistente = db.Clienti.Find(_formId);
                    if (esistente == null) return;

                    esistente.RagioneSociale = FormRagioneSociale;
                    esistente.PartitaIva = FormPartitaIva;
                    esistente.Referente = FormReferente;
                    esistente.Stato = FormStato;

                    clienteSalvato = esistente;

                    var vecchiContatti = db.Contatti.Where(c => c.ClienteId == clienteSalvato.Id).ToList();
                    db.Contatti.RemoveRange(vecchiContatti);
                }

                foreach (var contatto in TelefoniCliente.Concat(EmailCliente))
                {
                    db.Contatti.Add(new Contatti
                    {
                        Tipo = contatto.Tipo,
                        Valore = contatto.Valore,
                        Etichetta = contatto.Etichetta,
                        Principale = contatto.Principale,
                        ClienteId = clienteSalvato.Id
                    });
                }

                db.SaveChanges();
            }

            CaricaClienti();
            ClienteSelezionato = null;
            CaricaNelForm(null);
            ChiudiOverlay();
        }

        private bool ValidaPartitaIvaCodiceFiscale(string? valore, out string errore)
        {
            errore = string.Empty;
            if (string.IsNullOrWhiteSpace(valore)) return true; // facoltativo

            string valorePulito = valore.Trim().ToUpper();
            bool soloNumeri = valorePulito.All(char.IsDigit);

            if (soloNumeri)
            {
                if (valorePulito.Length != 11)
                {
                    errore = "La Partita IVA deve essere composta da 11 cifre numeriche.";
                    return false;
                }
            }
            else
            {
                if (valorePulito.Length != 16)
                {
                    errore = "Il Codice Fiscale deve essere composto da 16 caratteri.";
                    return false;
                }

                string patternCF = @"^[A-Z]{6}[0-9]{2}[A-EHLMPR-T][0-9]{2}[A-Z][0-9]{3}[A-Z]$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(valorePulito, patternCF))
                {
                    errore = "Il Codice Fiscale inserito non rispetta il formato previsto (es. RSSMRA80A01F205X).";
                    return false;
                }
            }

            return true;
        }

        private bool ValidaEmail(string? valore, out string errore)
        {
            errore = string.Empty;
            if (string.IsNullOrWhiteSpace(valore)) return true;

            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(valore, pattern))
            {
                errore = $"L'indirizzo email '{valore}' non sembra valido.";
                return false;
            }

            return true;
        }

        private bool ValidaTelefono(string? valore, out string errore)
        {
            errore = string.Empty;
            if (string.IsNullOrWhiteSpace(valore)) return true;

            string soloCifreSpazi = valore.Replace(" ", "").Replace("+", "");
            if (!soloCifreSpazi.All(char.IsDigit))
            {
                errore = $"Il numero di telefono '{valore}' contiene caratteri non validi (sono ammessi solo numeri, spazi e '+').";
                return false;
            }

            return true;
        }

        private void SalvaTelefono()
        {
            if (!ValidaTelefono(NuovoTelefonoValore, out string errore))
            {
                MessageBox.Show(errore, "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_telefonoInModificaId == 0)
            {
                bool ePrimo = !TelefoniCliente.Any();
                TelefoniCliente.Add(new Contatti
                {
                    Tipo = TipoContatto.Telefono,
                    Valore = NuovoTelefonoValore,
                    Etichetta = NuovoTelefonoEtichetta,
                    Principale = ePrimo
                });
            }
            else
            {
                var esistente = TelefoniCliente.FirstOrDefault(t => t.Id == _telefonoInModificaId);
                if (esistente != null)
                {
                    esistente.Valore = NuovoTelefonoValore;
                    esistente.Etichetta = NuovoTelefonoEtichetta;
                }
            }

            AnnullaModificaTelefono();
        }

        private void SelezionaTelefono(Contatti? telefono)
        {
            if (telefono == null) return;
            _telefonoInModificaId = telefono.Id;
            NuovoTelefonoValore = telefono.Valore;
            NuovoTelefonoEtichetta = telefono.Etichetta;
        }

        private void AnnullaModificaTelefono()
        {
            _telefonoInModificaId = 0;
            NuovoTelefonoValore = string.Empty;
            NuovoTelefonoEtichetta = null;
        }

        private void RimuoviTelefono(Contatti? telefono)
        {
            if (telefono == null) return;
            bool eraPrincipale = telefono.Principale;
            TelefoniCliente.Remove(telefono);

            if (eraPrincipale && TelefoniCliente.Any())
            {
                TelefoniCliente.First().Principale = true;
            }

            if (_telefonoInModificaId == telefono.Id)
            {
                AnnullaModificaTelefono();
            }
        }

        private void ImpostaTelefonoPrincipale(Contatti? telefono)
        {
            if (telefono == null) return;
            foreach (var t in TelefoniCliente)
            {
                t.Principale = (t == telefono);
            }
        }

        private void SalvaEmail()
        {
            if (!ValidaEmail(NuovaEmailValore, out string errore))
            {
                MessageBox.Show(errore, "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_emailInModificaId == 0)
            {
                bool ePrima = !EmailCliente.Any();
                EmailCliente.Add(new Contatti
                {
                    Tipo = TipoContatto.Email,
                    Valore = NuovaEmailValore,
                    Etichetta = NuovaEmailEtichetta,
                    Principale = ePrima
                });
            }
            else
            {
                var esistente = EmailCliente.FirstOrDefault(e => e.Id == _emailInModificaId);
                if (esistente != null)
                {
                    esistente.Valore = NuovaEmailValore;
                    esistente.Etichetta = NuovaEmailEtichetta;
                }
            }

            AnnullaModificaEmail();
        }

        private void SelezionaEmail(Contatti? email)
        {
            if (email == null) return;
            _emailInModificaId = email.Id;
            NuovaEmailValore = email.Valore;
            NuovaEmailEtichetta = email.Etichetta;
        }

        private void AnnullaModificaEmail()
        {
            _emailInModificaId = 0;
            NuovaEmailValore = string.Empty;
            NuovaEmailEtichetta = null;
        }

        private void RimuoviEmail(Contatti? email)
        {
            if (email == null) return;
            bool eraPrincipale = email.Principale;
            EmailCliente.Remove(email);

            if (eraPrincipale && EmailCliente.Any())
            {
                EmailCliente.First().Principale = true;
            }

            if (_emailInModificaId == email.Id)
            {
                AnnullaModificaEmail();
            }
        }

        private void ImpostaEmailPrincipale(Contatti? email)
        {
            if (email == null) return;
            foreach (var e in EmailCliente)
            {
                e.Principale = (e == email);
            }
        }

        private void ApriDettaglio(Cliente? cliente)
        {
            if (cliente == null) return;
            ClienteSelezionato = cliente;
            CaricaNelForm(cliente);
            ModalitaModifica = false;
            OverlayAperto = true;
        }

        private void EliminaCliente(Cliente? cliente)
        {
            if (cliente == null) return;

            var risultato = MessageBox.Show(
                $"Eliminare il cliente '{cliente.RagioneSociale}'?",
                "Conferma eliminazione",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (risultato != MessageBoxResult.Yes) return;

            using (var db = new ClabDbContext())
            {
                var daEliminare = db.Clienti.Find(cliente.Id);
                if (daEliminare != null)
                {
                    db.Clienti.Remove(daEliminare);
                    db.SaveChanges();
                }
            }

            CaricaClienti();
        }

        private void ChiudiOverlay()
        {
            OverlayAperto = false;
            ModalitaModifica = false;
        }
    }
}