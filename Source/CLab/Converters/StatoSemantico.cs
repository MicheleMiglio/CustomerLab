using CLab.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CLab.Converters
{
    /// <summary>
    /// Mapper semantico centrale degli stati CLab 2.0 (FASE 2). Un solo punto
    /// dove ogni valore (enum o stringa) viene risolto in:
    /// testo da mostrare, colore pieno (testo/pallino) e colore tenue (sfondo pill).
    /// Semantica ambra (BrushStatoStandBy = ColorAccent): solo "In scadenza" e
    /// "Attenzione" (+ priorità Media, in linea con l'uso già esistente);
    /// gli stati informativi usano il blu, gli stati bloccanti il rosso.
    /// È l'evoluzione dei converter per-modulo (StatoAColore, StatoFatturaAColore,
    /// PrioritaToDo/Promemoria...): uno alla volta vengono ritirati a favore di questo.
    /// </summary>
    public static class StatoSemantico
    {
        public static (string Testo, string ChiaveFore, string ChiaveBack) Risolvi(object? value)
        {
            switch (value)
            {
                // --- Stato cliente (enum) ---
                case StatoCliente s:
                    return s switch
                    {
                        StatoCliente.Attivo => ("Attivo", "BrushStatoAttivo", "BrushStatoAttivoLight"),
                        StatoCliente.StandBy => ("Stand by", "BrushStatoStandBy", "BrushStatoStandByLight"),
                        StatoCliente.Cessato => ("Cessato", "BrushStatoCessato", "BrushStatoCessatoLight"),
                        _ => ("—", "BrushTestoSecondario", "BrushSfondoChiaro")
                    };

                // --- Priorità ToDo / Promemoria (enum, stessa terna) ---
                case PrioritaToDo p:
                    return RisolviPriorita((int)p);

                case PrioritaPromemoria p:
                    return RisolviPriorita((int)p);

                // --- Ritenute d'acconto (bool "versato") ---
                case bool b:
                    return b
                        ? ("Versato", "BrushStatoAttivo", "BrushStatoAttivoLight")
                        : ("Da versare", "BrushStatoStandBy", "BrushStatoStandByLight");

                // --- Stringhe: adempimenti, fatture, ritenute + stati semantici CLab 2.0 ---
                default:
                    return RisolviTesto(value?.ToString());
            }
        }

        private static (string Testo, string ChiaveFore, string ChiaveBack) RisolviTesto(string? testo) => testo switch
        {
            // Adempimenti (CalcoloStatoAdempimenti)
            "Compilato" => ("Compilato", "BrushStatoAttivo", "BrushStatoAttivoLight"),
            "InCorso" => ("In corso", "BrushPrimary", "BrushInfoLight"),
            "Ritardo" => ("In ritardo", "BrushStatoCessato", "BrushStatoCessatoLight"),
            "Futuro" => ("Futuro", "BrushTestoSecondario", "BrushSfondoChiaro"),

            // Fatture (Fattura.Stato calcolato)
            "Emessa" => ("Emessa", "BrushPrimary", "BrushInfoLight"),
            "Pagata" => ("Pagata", "BrushStatoAttivo", "BrushStatoAttivoLight"),
            "Scaduta" => ("Scaduta", "BrushStatoCessato", "BrushStatoCessatoLight"),
            "Annullata" => ("Annullata", "BrushTestoSecondario", "BrushSfondoChiaro"),

            // Ritenute (StatoVersamento)
            "Versato" => ("Versato", "BrushStatoAttivo", "BrushStatoAttivoLight"),
            "DaVersare" => ("Da versare", "BrushStatoStandBy", "BrushStatoStandByLight"),
            "Anomalia" => ("Anomalia", "BrushStatoCessato", "BrushStatoCessatoLight"),

            // Stati semantici CLab 2.0 (pronti per i nuovi moduli)
            "Regolare" => ("Regolare", "BrushStatoAttivo", "BrushStatoAttivoLight"),
            "InScadenza" => ("In scadenza", "BrushStatoStandBy", "BrushStatoStandByLight"),
            "Attenzione" => ("Attenzione", "BrushStatoStandBy", "BrushStatoStandByLight"),
            "Scaduto" => ("Scaduto", "BrushStatoCessato", "BrushStatoCessatoLight"),
            "Completato" => ("Completato", "BrushStatoAttivo", "BrushStatoAttivoLight"),
            "Pagato" => ("Pagato", "BrushStatoAttivo", "BrushStatoAttivoLight"),
            "Insoluto" => ("Insoluto", "BrushStatoCessato", "BrushStatoCessatoLight"),
            "Errore" => ("Errore", "BrushStatoCessato", "BrushStatoCessatoLight"),
            "Disabilitato" => ("Disabilitato", "BrushTestoSecondario", "BrushSfondoChiaro"),
            "Informativo" => ("Informativo", "BrushPrimary", "BrushInfoLight"),

            _ => (testo ?? "—", "BrushTestoSecondario", "BrushSfondoChiaro")
        };

        private static (string Testo, string ChiaveFore, string ChiaveBack) RisolviPriorita(int valore) => valore switch
        {
            2 => ("Alta", "BrushStatoCessato", "BrushStatoCessatoLight"),
            1 => ("Media", "BrushStatoStandBy", "BrushStatoStandByLight"),
            0 => ("Bassa", "BrushPrimaryLight", "BrushInfoLight"),
            _ => ("—", "BrushTestoSecondario", "BrushSfondoChiaro")
        };

        internal static System.Windows.Media.Brush Brush(string chiave)
            => System.Windows.Application.Current.Resources[chiave] as System.Windows.Media.Brush
               ?? System.Windows.Media.Brushes.Gray;
    }

    /// <summary>Stato → testo del badge (riusabile anche dove serve solo l'etichetta).</summary>
    public class StatoSemanticoTestoConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => StatoSemantico.Risolvi(value).Testo;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Stato → colore pieno (testo e pallino del badge).</summary>
    public class StatoSemanticoForeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => StatoSemantico.Brush(StatoSemantico.Risolvi(value).ChiaveFore);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Stato → colore tenue (sfondo della pillola).</summary>
    public class StatoSemanticoBackConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => StatoSemantico.Brush(StatoSemantico.Risolvi(value).ChiaveBack);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

