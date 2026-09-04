using CLab.Models;
using System;

namespace CLab.Services
{
    /// <summary>
    /// Calcolo dello stato di un periodo di adempimento. FASE 4: estrazione
    /// della logica un tempo duplicata in ScadenzarioViewModel e HomeViewModel,
    /// per evitare una terza implementazione con la nuova Home.
    /// Semantica invariata al 100%: "Futuro" resta intenzionalmente escluso
    /// dai conteggi a monte (dashboard, torte, percentuali).
    /// </summary>
    public static class CalcoloStatoAdempimenti
    {
        public const string Compilato = "Compilato";
        public const string InCorso = "InCorso";
        public const string Ritardo = "Ritardo";
        public const string Futuro = "Futuro";

        public static string Calcola(Periodicita periodicita, int anno, int periodo, bool compilato)
        {
            if (compilato) return Compilato;

            var oggi = DateTime.Now;

            if (periodicita == Periodicita.Annuale)
                return anno < oggi.Year ? Ritardo : InCorso;

            if (anno < oggi.Year) return Ritardo;
            if (anno > oggi.Year) return Futuro;

            int correnteIndice = periodicita == Periodicita.Mensile ? oggi.Month : ((oggi.Month - 1) / 3) + 1;
            if (periodo < correnteIndice) return Ritardo;
            if (periodo == correnteIndice) return InCorso;
            return Futuro;
        }
    }
}
