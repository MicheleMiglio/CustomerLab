using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace CLab.Services
{
    /// <summary>
    /// Stato di sessione del modulo Password, esclusivamente in memoria:
    /// contiene la DEK solo mentre la sessione è sbloccata e la azzera con
    /// CryptographicOperations.ZeroMemory al lock (manuale o per timeout).
    ///
    /// La validità è basata su Stopwatch (clock monotono), NON su DateTime:
    /// il DispatcherTimer eventuale nella UI serve solo a notificare
    /// l'interfaccia, la validità reale è sempre verificata qui.
    ///
    /// Nessuna persistenza: né la DEK, né Master Password, né Recovery Code
    /// transitano da qui verso il database o i log.
    ///
    /// Il costruttore con durata personalizzata esiste SOLO per l'harness di
    /// test (simulazione del tempo); l'applicazione usa esclusivamente
    /// l'istanza singleton con la durata fissa di 15 minuti, non configurabile.
    /// </summary>
    public class PasswordSessionService
    {
        /// <summary>Durata fissa della sessione in produzione: 15 minuti esatti.</summary>
        public static readonly TimeSpan DurataSessione = TimeSpan.FromMinutes(15);

        private static readonly Lazy<PasswordSessionService> _istanza =
            new(() => new PasswordSessionService());

        /// <summary>Istanza condivisa a livello applicativo (durata fissa 15 minuti).</summary>
        public static PasswordSessionService Istanza => _istanza.Value;

        private readonly object _lock = new();
        private readonly Stopwatch _cronometro = Stopwatch.StartNew();
        private readonly TimeSpan _durata;

        private byte[]? _dek;
        // Istante dell'ultima attività sul cronometro. TimeSpan, non tick: i tick
        // di Stopwatch dipendono da Stopwatch.Frequency e NON coincidono con i
        // tick di TimeSpan (100 ns) su tutte le macchine, quindi il timeout
        // poteva allungarsi o accorciarsi a seconda dell'hardware.
        private TimeSpan _ultimoAccesso;

        // Sorveglianza indipendente dalla UI: azzera la DEK alla scadenza anche
        // se l'utente ha lasciato il modulo Password. Riferimento tenuto nel
        // campo per evitare la raccolta da parte del GC.
        private System.Threading.Timer? _sorveglianza;
        private static readonly TimeSpan IntervalloSorveglianza = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Sollevato a ogni transizione da sessione aperta a sessione chiusa:
        /// lock manuale, scadenza rilevata da VerificaTimeout oppure da una
        /// operazione sensibile (TryGetDek). IsUnlocked e SessioneValida sono
        /// solo letture e non lo sollevano.
        /// </summary>
        public event Action? SessioneTerminata;

        /// <summary>Costruttore di produzione (durata fissa 15 minuti).</summary>
        public PasswordSessionService() : this(DurataSessione) { }

        /// <summary>Costruttore con durata personalizzata: SOLO per harness di test.</summary>
        public PasswordSessionService(TimeSpan durataSessione)
        {
            if (durataSessione <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(durataSessione));

            _durata = durataSessione;
        }

        /// <summary>
        /// True se la sessione è sbloccata e non scaduta. La verifica del
        /// timeout avviene SEMPRE qui dentro, a prescindere dalla UI.
        /// </summary>
        public bool IsUnlocked
        {
            get
            {
                lock (_lock)
                {
                    return SessioneValidaInterna();
                }
            }
        }

        /// <summary>Tempo residuo prima del lock automatico (Zero se bloccata).</summary>
        public TimeSpan TempoRimanente
        {
            get
            {
                lock (_lock)
                {
                    if (_dek == null)
                        return TimeSpan.Zero;

                    var trascorso = _cronometro.Elapsed - _ultimoAccesso;
                    return trascorso >= _durata ? TimeSpan.Zero : _durata - trascorso;
                }
            }
        }

        /// <summary>
        /// Rende la DEK disponibile alla sessione appena sbloccata (copia interna).
        /// Il chiamante resta responsabile dell'azzeramento della sua copia.
        /// </summary>
        public void Sblocca(byte[] dek)
        {
            if (dek == null || dek.Length != 32)
                throw new ArgumentException("DEK non valida.", nameof(dek));

            lock (_lock)
            {
                AzzeraDekInterna();
                _dek = (byte[])dek.Clone();
                _ultimoAccesso = _cronometro.Elapsed;

                _sorveglianza?.Dispose();
                _sorveglianza = new System.Threading.Timer(
                    _ => VerificaTimeout(), null, IntervalloSorveglianza, IntervalloSorveglianza);
            }
        }

        /// <summary>
        /// Restituisce una COPIA della DEK se la sessione è valida, altrimenti
        /// null (in quel caso esegue anche il lock se il timeout è scaduto).
        /// Ogni operazione sensibile deve passare da qui: è il controllo
        /// logico di sicurezza, non un semplice IsEnabled della UI.
        /// </summary>
        public byte[]? TryGetDek()
        {
            bool terminata;
            byte[]? copia;

            lock (_lock)
            {
                if (!SessioneValidaInterna())
                {
                    terminata = _dek != null;
                    EseguiLockInterno();
                    copia = null;
                }
                else
                {
                    terminata = false;
                    _ultimoAccesso = _cronometro.Elapsed; // Touch implicito
                    copia = (byte[])_dek!.Clone();
                }
            }

            // L'evento esce dal lock: chi lo riceve può richiamare il servizio
            // senza rischi (la seconda volta la sessione risulta già bloccata).
            if (terminata)
                SessioneTerminata?.Invoke();

            return copia;
        }

        /// <summary>Verifica la validità della sessione senza esporre la DEK.</summary>
        public bool SessioneValida()
        {
            lock (_lock)
            {
                return SessioneValidaInterna();
            }
        }

        /// <summary>Rinnova il riferimento temporale dell'ultima attività.</summary>
        public void Touch()
        {
            lock (_lock)
            {
                if (_dek != null)
                    _ultimoAccesso = _cronometro.Elapsed;
            }
        }

        /// <summary>Verifica il timeout: se scaduto esegue il lock. True se ancora valida.</summary>
        public bool VerificaTimeout()
        {
            bool terminata;

            lock (_lock)
            {
                if (SessioneValidaInterna())
                    return true;

                terminata = _dek != null;
                EseguiLockInterno();
            }

            if (terminata)
                SessioneTerminata?.Invoke();

            return false;
        }

        /// <summary>
        /// Blocca la sessione: azzera la DEK in memoria (ZeroMemory), ne
        /// elimina il riferimento e marca la sessione come locked.
        /// </summary>
        public void Lock()
        {
            bool terminata;

            lock (_lock)
            {
                terminata = _dek != null;
                EseguiLockInterno();
            }

            if (terminata)
                SessioneTerminata?.Invoke();
        }

        // --- Interni (chiamati sempre sotto lock) ---

        private bool SessioneValidaInterna()
        {
            if (_dek == null)
                return false;

            var trascorso = _cronometro.Elapsed - _ultimoAccesso;
            return trascorso < _durata;
        }

        private void EseguiLockInterno()
        {
            _sorveglianza?.Dispose();
            _sorveglianza = null;
            AzzeraDekInterna();
            _ultimoAccesso = TimeSpan.Zero;
        }

        private void AzzeraDekInterna()
        {
            if (_dek != null)
            {
                CryptographicOperations.ZeroMemory(_dek);
                _dek = null;
            }
        }
    }
}