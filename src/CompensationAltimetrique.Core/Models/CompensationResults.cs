using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

namespace CompensationAltimetrique.Core.Models
{
    /// <summary>
    /// Résultats de la compensation altimétrique.
    /// </summary>
    public class CompensationResults
    {
        /// <summary>Écart-type a posteriori σ₀</summary>
        public double Sigma0 { get; set; }

        /// <summary>Corrections calculées</summary>
        public Vector<double> Corrections { get; set; } = Vector<double>.Build.Dense(0);

        /// <summary>Résidus</summary>
        public Vector<double> Residuals { get; set; } = Vector<double>.Build.Dense(0);

        /// <summary>RMS des résidus</summary>
        public double RmsResiduals { get; set; }

        /// <summary>Validation réussie</summary>
        public bool IsValid { get; set; }

        /// <summary>Points ajustés</summary>
        public List<AltitudePoint> AdjustedPoints { get; set; } = new();

        /// <summary>Correction maximale</summary>
        public double MaxCorrection { get; set; }

        /// <summary>Erreur de fermeture</summary>
        public double ClosureError { get; set; }

        /// <summary>Distance totale</summary>
        public double TotalDistance { get; set; }

        /// <summary>Nombre d'itérations</summary>
        public int Iterations { get; set; }

        /// <summary>Précision atteinte</summary>
        public bool PrecisionAchieved { get; set; }

        /// <summary>Détails de validation</summary>
        public Dictionary<string, object> ValidationDetails { get; set; } = new();

        /// <summary>Messages d'erreur</summary>
        public List<string> ErrorMessages { get; set; } = new();

        /// <summary>Messages d'avertissement</summary>
        public List<string> WarningMessages { get; set; } = new();

        /// <summary>Temps de calcul</summary>
        public TimeSpan ComputationTime { get; set; } = TimeSpan.Zero;

        /// <summary>Horodatage</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>Nombre de points traités</summary>
        public int ProcessedPoints { get; set; } = 0;

        /// <summary>Statut du traitement</summary>
        public string Status { get; set; } = "Success";

        /// <summary>Détails textuels</summary>
        public string Details { get; set; } = "";

        /// <summary>Précision numérique</summary>
        public double Precision { get; set; }
    }
}