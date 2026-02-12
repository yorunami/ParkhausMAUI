using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using ParkhausMAUI.Models;

namespace ParkhausMAUI.ViewModels
{
    /// <summary>
    /// VIEWMODEL-KLASSE (MVVM Pattern)
    /// Verbindet die Benutzeroberfläche (View) mit den Daten (Model).
    /// Enthält die gesamte Geschäftslogik, Validierung und Befehle (Commands).
    /// </summary>
    public class ParkingViewModel : INotifyPropertyChanged
    {
        // --- KONSTANTEN (Clean Code) ---
        // Zentral verwaltete Werte für einfache Wartbarkeit
        private const double PricePerMinute = 0.50; // Preis pro Minute in CHF
        private const string RegexPattern = @"^[A-Z]{2}\s[0-9]{1,6}$"; // Format: "ZH 12345"

        // Kompilierter Regex für bessere Performance bei häufiger Nutzung
        private static readonly Regex SwissLicensePattern = new Regex(RegexPattern, RegexOptions.Compiled);

        // --- DATENHALTUNG ---

        // Liste ALLER Parkplätze (Datenbank-Ersatz im Speicher)
        private readonly List<ParkingSlot> _allSlots = new();

        /// <summary>
        /// Liste für die UI-Anzeige. 
        /// ObservableCollection aktualisiert die Oberfläche automatisch bei Änderungen.
        /// Enthält nur die Plätze der aktuell ausgewählten Etage.
        /// </summary>
        public ObservableCollection<ParkingSlot> VisibleSlots { get; set; } = new();

        // Verfügbare Etagen für den Picker
        public ObservableCollection<string> Floors { get; } = new() { "EG", "1. OG", "2. OG" };

        // --- PROPERTIES (mit INotifyPropertyChanged) ---

        private string _selectedFloor = "EG";
        /// <summary>
        /// Die aktuell ausgewählte Etage.
        /// Bei Änderung wird die Liste der sichtbaren Parkplätze neu gefiltert.
        /// </summary>
        public string SelectedFloor
        {
            get => _selectedFloor;
            set { SetProperty(ref _selectedFloor, value); FilterSlots(); }
        }

        private string _inputText = string.Empty;
        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        private string _statusMessage = "Willkommen! Bitte Kennzeichen eingeben.";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // --- COMMANDS ---
        // Befehle, die von den Buttons in der UI ausgelöst werden
        public ICommand ParkInCommand { get; }
        public ICommand ParkOutCommand { get; }

        /// <summary>
        /// Konstruktor: Initialisiert das Parkhaus und die Befehle.
        /// </summary>
        public ParkingViewModel()
        {
            InitializeParkingGarage();

            // Verknüpft die Methoden mit den Commands
            ParkInCommand = new Command<ParkingSlot>(OnParkIn);
            ParkOutCommand = new Command<ParkingSlot>(OnParkOut);

            // Zeigt initial das Erdgeschoss an
            FilterSlots();
        }

        // Erstellt die Parkplätze für alle Etagen (Initialisierung)
        private void InitializeParkingGarage()
        {
            foreach (var floor in Floors)
            {
                for (int i = 1; i <= 6; i++)
                {
                    _allSlots.Add(new ParkingSlot { SlotNumber = i, FloorName = floor });
                }
            }
        }

        /// <summary>
        /// Logik für das Einparken.
        /// Führt Validierungen durch und aktualisiert den Slot-Status.
        /// </summary>
        /// <param name="slot">Der ausgewählte Parkplatz aus der UI</param>
        private void OnParkIn(ParkingSlot? slot)
        {
            if (slot == null) return;

            // Input Sanitization: Leerzeichen entfernen und Grossschreibung erzwingen
            string rawInput = InputText?.Trim().ToUpper() ?? "";

            // --- VALIDIERUNG (Robustheit) ---

            // 1. Prüfen ob Eingabe leer ist
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                StatusMessage = "⚠️ Bitte Kennzeichen eingeben!";
                return;
            }

            // 2. Format-Prüfung mit Regex (z.B. muss "ZH 123" sein)
            if (!SwissLicensePattern.IsMatch(rawInput))
            {
                StatusMessage = "⚠️ Format falsch! (z.B. 'ZH 12345')";
                return;
            }

            // 3. Duplikat-Prüfung: Ist das Auto schon im Parkhaus?
            if (_allSlots.Any(s => s.IsOccupied && s.LicensePlate == rawInput))
            {
                StatusMessage = "❌ Fehler: Auto befindet sich bereits im Parkhaus!";
                return;
            }

            // --- DURCHFÜHRUNG ---

            var realSlot = GetRealSlot(slot);
            if (realSlot != null && !realSlot.IsOccupied)
            {
                realSlot.IsOccupied = true;
                realSlot.LicensePlate = rawInput;
                realSlot.EntryTime = DateTime.Now; // Zeitstempel setzen

                StatusMessage = $"✅ {rawInput} geparkt auf {realSlot.FloorName}, Platz {realSlot.SlotNumber}";
                InputText = ""; // Eingabefeld leeren
                FilterSlots();  // UI aktualisieren
            }
        }

        /// <summary>
        /// Logik für das Ausparken.
        /// Berechnet den Preis basierend auf der Parkdauer.
        /// </summary>
        private void OnParkOut(ParkingSlot? slot)
        {
            if (slot == null) return;

            var realSlot = GetRealSlot(slot);
            if (realSlot != null && realSlot.IsOccupied)
            {
                // Zeitdifferenz berechnen
                var duration = DateTime.Now - (realSlot.EntryTime ?? DateTime.Now);

                // Preisberechnung: Aufrunden der Minuten * Preis pro Minute
                double price = Math.Ceiling(duration.TotalMinutes) * PricePerMinute;

                StatusMessage = $"🚗 Gute Fahrt! Kosten: {price:F2} CHF";

                // Slot zurücksetzen (Freigeben)
                realSlot.IsOccupied = false;
                realSlot.LicensePlate = null;
                realSlot.EntryTime = null;

                FilterSlots(); // UI aktualisieren
            }
        }

        // Hilfsmethode: Findet das echte Slot-Objekt in der Hauptliste
        private ParkingSlot? GetRealSlot(ParkingSlot uiSlot)
        {
            return _allSlots.FirstOrDefault(s => s.FloorName == uiSlot.FloorName && s.SlotNumber == uiSlot.SlotNumber);
        }

        // Filtert die Liste der Parkplätze basierend auf der gewählten Etage
        private void FilterSlots()
        {
            VisibleSlots.Clear();
            foreach (var slot in _allSlots.Where(s => s.FloorName == SelectedFloor))
            {
                VisibleSlots.Add(slot);
            }
        }

        // --- INotifyPropertyChanged IMPLEMENTIERUNG ---
        public event PropertyChangedEventHandler? PropertyChanged;

        // Helper-Methode für saubere Property-Updates (Clean Code)
        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return;

            storage = value;
            // Benachrichtigt die UI, dass sich ein Wert geändert hat
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}