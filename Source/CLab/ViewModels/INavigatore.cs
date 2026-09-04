namespace CLab.ViewModels
{
    /// <summary>
    /// Navigazione contestuale CLab 2.0 (FASE 4). Astrazione minima implementata
    /// da MainViewModel: le sorgenti di navigazione (es. la nuova Home) aprono
    /// i moduli già posizionati/filtrati senza dipendere direttamente da
    /// MainViewModel, ma senza framework né DI. I parametri nulli lasciano
    /// il modulo nel comportamento predefinito.
    /// </summary>
    public interface INavigatore
    {
        void ApriClienti();

        /// <summary>Apri il modulo ToDo; se indicato, con filtri preapplicati.</summary>
        void ApriToDo(int? clienteId = null, bool soloScaduti = false, bool prioritaAlta = false);

        void ApriPromemoria();

        /// <summary>
        /// Apri lo Scadenzario; se indicato, già posizionato sul cliente con
        /// scheda preimpostata ("adempimenti"/"ritenute") e, per gli adempimenti,
        /// filtro operativo "solo ritardi".
        /// </summary>
        void ApriScadenzario(int? clienteId = null, string? scheda = null, bool soloRitardi = false);

        /// <summary>Apri il modulo Fatture; se indicato, limitato all'anno.</summary>
        void ApriFatture(int? anno = null);
    }
}
