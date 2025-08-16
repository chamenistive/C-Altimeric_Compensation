// ================================================================
// MODÈLES POUR L'INTERFACE WEB
// DTOs et modèles spécifiques à l'interface utilisateur
// ================================================================

using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Web.Models
{
    /// <summary>
    /// Modèle pour l'upload de fichiers.
    /// </summary>
    public class FileUploadModel
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadTime { get; set; } = DateTime.Now;
        public bool IsValid { get; set; } = false;
        public List<string> ValidationErrors { get; set; } = new();
    }

    /// <summary>
    /// Modèle pour la configuration des paramètres.
    /// </summary>
    public class CompensationParametersModel
    {
        public string ReferencePoint { get; set; } = string.Empty;
        public double ReferenceAltitude { get; set; } = 100.0;
        public double TargetPrecisionMm { get; set; } = 2.0;
        public double InstrumentalErrorMm { get; set; } = 1.0;
        public double KilometricErrorMm { get; set; } = 1.5;
        public double ConfidenceLevel { get; set; } = 0.95;
        public bool ApplyAtmosphericCorrections { get; set; } = false;
        public string NetworkType { get; set; } = "fermé";
        
        // Conditions atmosphériques si activées
        public double TemperatureCelsius { get; set; } = 20.0;
        public double PressureHPa { get; set; } = 1013.25;
        public double HumidityPercent { get; set; } = 60.0;
    }

    /// <summary>
    /// Modèle pour l'affichage des données importées.
    /// </summary>
    public class LevelingDataDisplayModel
    {
        public string Matricule { get; set; } = string.Empty;
        public double AR1 { get; set; }
        public double AV1 { get; set; }
        public double? DIST1 { get; set; }
        public double? AR2 { get; set; }
        public double? AV2 { get; set; }
        public double? DIST2 { get; set; }
        public double DeltaH1 => AV1 - AR1;
        public double? DeltaH2 => (AR2.HasValue && AV2.HasValue) ? AV2.Value - AR2.Value : null;
        public double? Controle => DeltaH2.HasValue ? Math.Abs(DeltaH1 - DeltaH2.Value) : null;
        public bool IsValid => Controle == null || Controle < 0.01; // 1cm de tolérance
        public string ValidationStatus => IsValid ? "✅" : "⚠️";

        public static LevelingDataDisplayModel FromLevelingData(LevelingData data)
        {
            return new LevelingDataDisplayModel
            {
                Matricule = data.Matricule,
                AR1 = data.AR1 ?? 0,
                AV1 = data.AV1 ?? 0,
                DIST1 = data.DIST1,
                AR2 = data.AR2,
                AV2 = data.AV2,
                DIST2 = data.DIST2
            };
        }
    }

    /// <summary>
    /// Modèle pour l'affichage des résultats.
    /// </summary>
    public class CompensationResultDisplayModel
    {
        public string Matricule { get; set; } = string.Empty;
        public double ObservedAltitude { get; set; }
        public double AdjustedAltitude { get; set; }
        public double Correction { get; set; }
        public double StandardDeviation { get; set; }
        public bool IsReference { get; set; }
        public string Status => IsReference ? "REF" : "ADJ";
        
        public string FormattedObservedAltitude => ObservedAltitude.ToString("F4");
        public string FormattedAdjustedAltitude => AdjustedAltitude.ToString("F4");
        public string FormattedCorrection => (Correction * 1000).ToString("F1") + "mm";
        public string FormattedStandardDeviation => (StandardDeviation * 1000).ToString("F1") + "mm";
    }

    /// <summary>
    /// Modèle pour les statistiques de compensation.
    /// </summary>
    public class CompensationStatisticsModel
    {
        public int TotalPoints { get; set; }
        public int ProcessedPoints { get; set; }
        public double ClosureError { get; set; }
        public double RmsResiduals { get; set; }
        public double MaxCorrection { get; set; }
        public double TotalDistance { get; set; }
        public TimeSpan ComputationTime { get; set; }
        public bool IsValid { get; set; }
        public bool PrecisionAchieved { get; set; }
        
        public string FormattedClosureError => (ClosureError * 1000).ToString("F1") + "mm";
        public string FormattedRmsResiduals => (RmsResiduals * 1000).ToString("F1") + "mm";
        public string FormattedMaxCorrection => (MaxCorrection * 1000).ToString("F1") + "mm";
        public string FormattedTotalDistance => TotalDistance.ToString("F2") + "km";
        public string FormattedComputationTime => ComputationTime.TotalSeconds.ToString("F1") + "s";
        public string ValidationStatus => IsValid ? "✅ VALIDÉ" : "❌ ÉCHEC";
        public string PrecisionStatus => PrecisionAchieved ? "✅ ATTEINTE" : "⚠️ NON ATTEINTE";
    }

    /// <summary>
    /// Modèle pour les options d'export.
    /// </summary>
    public class ExportOptionsModel
    {
        public bool IncludeRawData { get; set; } = true;
        public bool IncludeCalculations { get; set; } = true;
        public bool IncludeStatistics { get; set; } = true;
        public bool IncludeDiagrams { get; set; } = false;
        public string ExportFormat { get; set; } = "PDF";
        public string FileName { get; set; } = string.Empty;
        public string Title { get; set; } = "Rapport de Compensation Altimétrique";
        public string Operator { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Modèle pour l'état de progression.
    /// </summary>
    public class ProgressModel
    {
        public int Percentage { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public bool IsIndeterminate { get; set; } = false;
        public string EstimatedTimeRemaining { get; set; } = string.Empty;
    }

    /// <summary>
    /// Modèle pour les notifications utilisateur.
    /// </summary>
    public class NotificationModel
    {
        public enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error
        }

        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Info;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool AutoDismiss { get; set; } = true;
        public int DismissAfterMs { get; set; } = 5000;
        
        public string CssClass => Type switch
        {
            NotificationType.Success => "alert-success",
            NotificationType.Warning => "alert-warning",
            NotificationType.Error => "alert-danger",
            _ => "alert-info"
        };
        
        public string Icon => Type switch
        {
            NotificationType.Success => "✅",
            NotificationType.Warning => "⚠️",
            NotificationType.Error => "❌",
            _ => "ℹ️"
        };
    }

    /// <summary>
    /// Modèle pour la validation des données.
    /// </summary>
    public class DataValidationModel
    {
        public bool IsValid { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new();
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
        public int WarningRows { get; set; }
        
        public class ValidationIssue
        {
            public string Type { get; set; } = string.Empty; // Error, Warning, Info
            public string Message { get; set; } = string.Empty;
            public int? RowNumber { get; set; }
            public string? Column { get; set; }
            public string? Value { get; set; }
        }
    }
}