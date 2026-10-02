using System;
using System.Security.Cryptography;
using System.Windows;

namespace CLab.Services
{
    /// <summary>
    /// Astrazione minima sugli appunti, usata da PasswordClipboardService:
    /// esiste SOLO per rendere testabile la logica di copia/cancellazione
    /// senza dipendere dal clipboard reale (e senza NuGet). In produzione è
    /// implementata con System.Windows.Clipboard.
    /// </summary>
    public interface IAppunti
    {
        void ImpostaTesto(string testo);
        string? LeggiTesto();
        void Svuota();
    }

    /// <summary>Implementazione reale: System.Windows.Clipboard (thread UI).
    /// Incapsula la gestione delle eccezioni COM quando il clipboard è
    /// occupato da altre applicazioni: mai crash, mai retry aggressivi.</summary>
    public class AppuntiWpf : IAppunti
    {
        // Il clipboard di Windows richiede un thread STA: il timer di
        // cancellazione gira su un thread del pool (MTA), quindi ogni accesso
        // viene marcato sul thread UI. Senza questo, la cancellazione a 30
        // secondi falliva in silenzio e la password restava negli appunti.
        private static T Esegui<T>(Func<T> azione, T predefinito)
        {
            try
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher == null || dispatcher.CheckAccess())
                    return azione();

                return dispatcher.Invoke(azione);
            }
            catch (Exception)
            {
                return predefinito; // clipboard occupato o app in chiusura: silenzioso
            }
        }

        public void ImpostaTesto(string testo)
        {
            Esegui(() => { Clipboard.SetText(testo); return true; }, false);
        }

        public string? LeggiTesto()
        {
            return Esegui<string?>(() => Clipboard.ContainsText() ? Clipboard.GetText() : null, null);
        }

        public void Svuota()
        {
            Esegui(() => { Clipboard.Clear(); return true; }, false);
        }
    }

    /// <summary>
    /// Copia degli appunti legata alla sessione Password:
    /// 1. copia consentita SOLO con sessione sbloccata (controllo logico qui,
    ///    non solo IsEnabled della UI);
    /// 2. dopo 30 secondi cancella il clipboard SOLO se il contenuto è ancora
    ///    ESATTAMENTE quello copiato da CLab; se l'utente ha copiato altro,
    ///    NON tocca nulla e NON ripristina il contenuto precedente;
    /// 3. nessuna persistenza del valore copiato: il riferimento resta vivo
    ///    solo per i 30 secondi del controllo.
    /// </summary>
    public class PasswordClipboardService
    {
        public const int SecondiAutoCancellazione = 30;

        private readonly IAppunti _appunti;
        private readonly Func<TimeSpan, System.Threading.Timer> _creaTimer;
        private readonly object _lock = new();

        // Valore copiato in attesa del controllo a 30 secondi (riferimento
        // unico, rilasciato subito dopo il confronto).
        private string? _valoreInAttesa;

        // Riferimento al timer in corso: un System.Threading.Timer senza
        // riferimenti viene raccolto dal GC e non scatta mai. Una nuova copia
        // sostituisce la precedente, così i 30 secondi contano dall'ultima copia.
        private System.Threading.Timer? _timerScadenza;

        public PasswordClipboardService() : this(new AppuntiWpf(), CreaTimerProduzione)
        { }

        /// <summary>Timer reale (produzione): dopo 30 secondi richiama VerificaScadenza.</summary>
        private static System.Threading.Timer CreaTimerProduzione(TimeSpan delay)
        {
            // Il callback usa il singleton applicativo; per i test viene iniettato
            // un timer fittizio e VerificaScadenza è invocata manualmente.
            return new System.Threading.Timer(
                _ => IstanzaApplicativo.VerificaScadenza(), null, delay, System.Threading.Timeout.InfiniteTimeSpan);
        }

        private static PasswordClipboardService? _istanzaApplicativo;
        private static readonly object _istanzaLock = new();

        /// <summary>Istanza applicativa (usata dal timer di produzione).</summary>
        public static PasswordClipboardService IstanzaApplicativo
        {
            get
            {
                if (_istanzaApplicativo == null)
                {
                    lock (_istanzaLock)
                    {
                        _istanzaApplicativo ??= new PasswordClipboardService();
                    }
                }

                return _istanzaApplicativo;
            }
        }

        /// <summary>Costruttore con astrazioni iniettabili: SOLO per i test.</summary>
        public PasswordClipboardService(IAppunti appunti, Func<TimeSpan, System.Threading.Timer> creaTimer)
        {
            _appunti = appunti;
            _creaTimer = creaTimer;
        }

        /// <summary>
        /// Copia il testo se la sessione è valida. Ritorna false (senza copiare)
        /// se la sessione è bloccata o scaduta: la UI può mostrare l'esito.
        /// </summary>
        public bool Copia(string testo)
        {
            // Controllo logico di sicurezza: la validità è decisa dalla sessione
            // (Stopwatch), a prescindere dallo stato della UI.
            if (!PasswordSessionService.Istanza.SessioneValida())
                return false;

            try
            {
                _appunti.ImpostaTesto(testo);
            }
            catch (Exception)
            {
                // Clipboard occupato/indisponibile: niente crash, nessuna copia.
                return false;
            }

            lock (_lock)
            {
                _valoreInAttesa = testo;

                // Attesa di 30 secondi dall'ultima copia, poi VerificaScadenza
                // (timer di thread: indipendente dalla sessione Password).
                _timerScadenza?.Dispose();
                _timerScadenza = _creaTimer(TimeSpan.FromSeconds(SecondiAutoCancellazione));
            }

            return true;
        }

        /// <summary>
        /// Verifica a 30 secondi: se il clipboard contiene ancora ESATTAMENTE
        /// il valore copiato da CLab lo cancella; altrimenti non lo tocca.
        /// Indipendente dallo stato della sessione Password.
        /// </summary>
        public void VerificaScadenza()
        {
            string? valore;
            lock (_lock)
            {
                valore = _valoreInAttesa;
                _valoreInAttesa = null; // rilascio immediato del riferimento
                _timerScadenza?.Dispose();
                _timerScadenza = null;
            }

            if (valore == null)
                return;

            var attuale = _appunti.LeggiTesto();

            // Confronto a tempo costante, non porta indizi temporali sul segreto.
            if (attuale != null && CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(attuale),
                    System.Text.Encoding.UTF8.GetBytes(valore)))
            {
                _appunti.Svuota();
            }
        }
    }
}