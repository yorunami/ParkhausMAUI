using Microsoft.Maui.Graphics;

namespace ParkhausMAUI.Models
{
    /// <summary>
    /// MODEL-KLASSE
    /// Repräsentiert einen einzelnen Parkplatz im System.
    /// Diese Klasse hält nur Daten und keine Geschäftslogik (Separation of Concerns).
    /// </summary>
    public class ParkingSlot
    {
        // Die Nummer des Parkplatzes (1-6)
        public int SlotNumber { get; set; }

        // Die Etage, zu der dieser Platz gehört (z.B. "EG", "1. OG")
        public string FloorName { get; set; } = "EG";

        // Status: Ist der Parkplatz belegt?
        public bool IsOccupied { get; set; }

        // Kennzeichen des parkenden Autos (kann null sein, wenn leer)
        public string? LicensePlate { get; set; }

        // Zeitpunkt der Einfahrt (für Preisberechnung)
        public DateTime? EntryTime { get; set; }

        /// <summary>
        /// Gibt die Farbe für die UI zurück, basierend auf dem Belegt-Status.
        /// Rot (#E11D48) = Belegt, Grün (#10B981) = Frei.
        /// Dies ermöglicht visuelles Feedback in der App.
        /// </summary>
        public Color StatusColor => IsOccupied
            ? Color.FromArgb("#E11D48")
            : Color.FromArgb("#10B981");

        // Hilfstext für die Anzeige in der App
        public string InfoText => IsOccupied ? $"{LicensePlate}" : "FREI";

        // Hilfseigenschaft für die Sichtbarkeit der Buttons (DataBinding)
        public bool IsFree => !IsOccupied;
    }
}