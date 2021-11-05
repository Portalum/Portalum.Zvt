using System.Collections.Generic;

namespace Portalum.Payment.Zvt.Repositories
{
    /// <summary>
    /// German IntermediateStatusRepository
    /// </summary>
    public class GermanIntermediateStatusRepository : IIntermediateStatusRepository
    {
        private readonly Dictionary<byte, string> _statusCodes;

        /// <summary>
        /// German IntermediateStatusRepository
        /// </summary>
        public GermanIntermediateStatusRepository()
        {
            this._statusCodes = new Dictionary<byte, string>
            {
                { 0x00, "BZT wartet auf Betragbestätigung" },
                { 0x01, "Bitte Anzeigen auf dem PIN-Pad beachten" },
                { 0x02, "Bitte Anzeigen auf dem PIN-Pad beachten" },
                { 0x03, "Vorgang nicht möglich" },
                { 0x04, "BZT wartet auf Antwort vom FEP" },
                { 0x05, "BZT sendet Autostorno" },
                { 0x06, "BZT sendet Nachbuchungen" },
                { 0x07, "Karte nicht zugelassen" },
                { 0x08, "Karte unbekannt / undefiniert" },
                { 0x09, "Karte verfallen " },
                { 0x0A, "Karte einstecken" },
                { 0x0B, "Bitte Karte entnehmen!" },
                { 0x0C, "Karte nicht lesbar" },
                { 0x0D, "Vorgang abgebrochen" },
                { 0x0E, "Vorgang wird bearbeitet bitte warten..." },
                { 0x0F, "BZT leitet einen automatischen Kassenabschluss ein" },
                { 0x10, "Karte ungültig" },
                { 0x11, "Guthabenanzeige" },
                { 0x12, "Systemfehler" },
                { 0x13, "Zahlung nicht möglich" },
                { 0x14, "Guthaben nicht ausreichend" },
                { 0x15, "Geheimzahl falsch" },
                { 0x16, "Limit nicht ausreichend" },
                { 0x17, "Bitte warten..." },
                { 0x18, "Geheimzahl zu oft falsch" },
                { 0x19, "Kartendaten falsch" },
                { 0x1A, "Servicemodus" },
                { 0x1B, "Autorisierung erfolgt. Bitte tanken" },
                { 0x1C, "Zahlung erfolgt. Bitte Ware entnehmen" },
                { 0x1D, "Autorisierung nicht möglich" },
                { 0x26, "BZT wartet auf Eingabe der Mobilfunknummer" },
                { 0x27, "BZT wartet auf Wiederholung der Mobilfunknummer" },
                { 0x28, "Währungsauswahl, bitte warten..." },
                { 0x29, "Sprachauswahl, bitte warten..." },
                { 0x2A, "Zum Laden Karte einstecken" },
                { 0x2B, "Offline-Notbetrieb, bitte warten" },
                { 0x2C, "Auswahl Debit/Kredit, bitte warten " },
                { 0x41, "Bitte Anzeigen auf dem PIN-Pad beachten\nBitte Karte entnehmen!" },
                { 0x42, "Bitte Anzeigen auf dem PIN-Pad beachten\nBitte Karte entnehmen!" },
                { 0x43, "Vorgang nicht möglich\nBitte Karte entnehmen!" },
                { 0x44, "BZT wartet auf Antwort vom FEP\nBitte Karte entnehmen!" },
                { 0x45, "BZT sendet Autostorno\nBitte Karte entnehmen!" },
                { 0x46, "BZT sendet Nachbuchungen\nBitte Karte entnehmen!" },
                { 0x47, "Karte nicht zugelassen\nBitte Karte entnehmen!" },
                { 0x48, "Karte unbekannt / undefiniert\nBitte Karte entnehmen!" },
                { 0x49, "Karte verfallen\nBitte Karte entnehmen!" },
                { 0x4A, "" }, //Textfeld in der Dokumentation leer
                { 0x4B, "Bitte Karte entnehmen!" },
                { 0x4C, "Karte nicht lesbar\nBitte Karte entnehmen!" },
                { 0x4D, "Vorgang abgebrochen\nBitte Karte entnehmen!" },
                { 0x4E, "Vorgang wird bearbeitet bitte warten...\nBitte Karte entnehmen!" },
                { 0x4F, "BZT leitet einen automatischen Kassenabschluss ein\nBitte Karte entnehmen!" },
                { 0x50, "Karte ungültig\nBitte Karte entnehmen!" },
                { 0x51, "Guthabenanzeige\nBitte Karte entnehmen!" },
                { 0x52, "Systemfehler\nBitte Karte entnehmen!" },
                { 0x53, "Zahlung nicht möglich\nBitte Karte entnehmen!" },
                { 0x54, "Guthaben nicht ausreichend\nBitte Karte entnehmen!" },
                { 0x55, "Geheimzahl falsch\nBitte Karte entnehmen!" },
                { 0x56, "Limit nicht ausreichend\nBitte Karte entnehmen!" },
                { 0x57, "Bitte warten...\nBitte Karte entnehmen!" },
                { 0x58, "Geheimzahl zu oft falsch\nBitte Karte entnehmen!" },
                { 0x59, "Kartendaten falsch\nBitte Karte entnehmen!" },
                { 0x5A, "Servicemodus\nBitte Karte entnehmen!" },
                { 0x5B, "Autorisierung erfolgt. Bitte tanken\nBitte Karte entnehmen!" },
                { 0x5C, "Zahlung erfolgt. Bitte Ware entnehmen\nBitte Karte entnehmen!" },
                { 0x5D, "Autorisierung nicht möglich\nBitte Karte entnehmen!" },
                { 0x66, "BZT wartet auf Eingabe der Mobilfunknummer\nBitte Karte entnehmen!" },
                { 0x67, "BZT wartet auf Wiederholung der Mobilfunknummer\nBitte Karte entnehmen!" },
                { 0x68, "BZT hat Einstecken der Kundenkarte erkannt" },
                { 0x69, "Bitte DCC auswählen" },
                { 0xC7, "BZT wartet auf Eingabe des Kilometerstands" },
                { 0xC8, "BZT wartet auf Kassierer" },
                { 0xC9, "BZT leitet eine automatische Diagnose ein" },
                { 0xCA, "BZT leitet eine automatische Initialisierung ein" },
                { 0xCB, "Händlerjournal voll" },
                { 0xCC, "Lastschrift nicht möglich, PIN notwendig" },
                { 0xD2, "DFÜ-Verbindung wird hergestellt" },
                { 0xD3, "DFÜ-Verbindung besteht" },
                { 0xE0, "BZT wartet auf Anwendungsauswahl" },
                { 0xE1, "BZT wartet auf Sprachauswahl" },
                { 0xE2, "BZT fordert auf, die Reinungskarte zu benutzen" },
                { 0xF1, "Offline" },
                { 0xF2, "Online" },
                { 0xF3, "Offline-Transaktion" },
                { 0xFF, "kein geeigneter ZVT-Statuscode zu demdem Status. Siehe TLV-Tags 24 und 07" }
            };
        }

        /// <inheritdoc />
        public string GetMessage(byte errorCode)
        {
            if (this._statusCodes.TryGetValue(errorCode, out var errorMessage))
            {
                return errorMessage;
            }

            return null;
        }
    }
}
