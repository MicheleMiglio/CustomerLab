using System;

namespace CLab.ViewModels
{
    /// <summary>
    /// Voce della lista credenziali. Esiste solo a sessione sbloccata.
    /// Non contiene la password: il payload viene decifrato per i metadati
    /// (nome/sito/username/note) e la password è scartata subito.
    /// </summary>
    public class PasswordVoceViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string Nome { get; set; } = string.Empty;
        public string Sito { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;

        /// <summary>Usata solo per la ricerca in memoria, non mostrata in griglia.</summary>
        public string Note { get; set; } = string.Empty;

        public string AggiornataDisplay => UpdatedAt.ToString("dd/MM/yyyy HH:mm");

        // --- Password mostrata inline ---
        // Il valore in chiaro esiste qui SOLO mentre la password è mostrata:
        // lo imposta e lo azzera il PasswordViewModel (nascondi manuale, dopo
        // 10 secondi, al lock o al ricaricamento della lista).

        private string _passwordMostrata = string.Empty;
        private bool _mostrata;

        public bool PasswordMostrata => _mostrata;

        /// <summary>Testo della cella: maschera di lunghezza fissa (non rivela la
        /// lunghezza reale) oppure la password, se mostrata.</summary>
        public string PasswordDisplay =>
            _mostrata ? (_passwordMostrata.Length == 0 ? "\u2014" : _passwordMostrata) : "\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022";

        public void ImpostaPasswordMostrata(string password)
        {
            _passwordMostrata = password ?? string.Empty;
            _mostrata = true;
            OnPropertyChanged(nameof(PasswordMostrata));
            OnPropertyChanged(nameof(PasswordDisplay));
        }

        public void NascondiPassword()
        {
            _passwordMostrata = string.Empty;
            _mostrata = false;
            OnPropertyChanged(nameof(PasswordMostrata));
            OnPropertyChanged(nameof(PasswordDisplay));
        }
    }
}
