using System;

namespace CLab.Models
{
    public enum PrioritaPromemoria
    {
        Bassa = 0,
        Media = 1,
        Alta = 2
    }

    /// <summary>
    /// Un "post-it" digitale: niente scadenze, niente stato completato.
    /// Si scrive e, quando non serve più, si butta via.
    /// </summary>
    public class Promemoria
    {
        public int Id { get; set; }
        public string Titolo { get; set; } = string.Empty;
        public string? Descrizione { get; set; }
        public PrioritaPromemoria Priorita { get; set; } = PrioritaPromemoria.Media;
        public DateTime DataCreazione { get; set; } = DateTime.Now;
    }
}