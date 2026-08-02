using System.Collections.Generic;

namespace CLab.Models
{
    public enum Periodicita
    {
        Mensile,
        Trimestrale,
        Annuale
    }

    public enum TipoCampoAttivita
    {
        SiNo,
        TestoLibero,
        Numero,
        Tendina
    }

    public class Attivita
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public override string ToString() => Nome;
        public Periodicita Periodicita { get; set; }
        public TipoCampoAttivita TipoCampo { get; set; }

        // Rilevante solo se TipoCampo = TestoLibero
        public int? TestoLunghezzaMassima { get; set; }

        // Rilevante solo se TipoCampo = Numero
        public bool NumeroEImporto { get; set; }

        // Rilevante solo se TipoCampo = Tendina
        public bool TendinaRichiedeImporto { get; set; }
        public List<AttivitaOpzione> Opzioni { get; set; } = new();

        public List<ClienteAttivita> ClientiAssegnati { get; set; } = new();
    }

    public class AttivitaOpzione
    {
        public int Id { get; set; }
        public int AttivitaId { get; set; }
        public string Testo { get; set; } = string.Empty;
        public int Ordine { get; set; }
    }
}