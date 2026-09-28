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
    }
}
