using System;
using System.Collections.Generic;

namespace CompensationAltimetrique.Core.Models
{
    /// <summary>
    /// Résultats de la compensation par moindres carrés
    /// </summary>
    public class CompensationResults
    {
        public List<AltitudePoint> AdjustedPoints { get; set; }
        public double[] Corrections { get; set; }
        public double[] Residuals { get; set; }
        public double Sigma0 { get; set; }  // Écart-type unitaire
        public double MaxCorrection { get; set; }
        public double RmsResiduals { get; set; }
        public string Method { get; set; }
        public DateTime ComputationTime { get; set; }
        public bool IsValid { get; set; }
        
        public CompensationResults()
        {
            AdjustedPoints = new List<AltitudePoint>();
            ComputationTime = DateTime.Now;
            Method = "Moindres Carrés";
        }
        
        /// <summary>
        /// Évalue la qualité de la compensation
        /// </summary>
        public string GetQualityAssessment()
        {
            if (Sigma0 < 0.001)
                return "🌟 Excellente qualité - Compensation très précise";
            else if (Sigma0 < 0.002)
                return "✅ Bonne qualité - Compensation satisfaisante";
            else if (Sigma0 < 0.003)
                return "⚠️ Qualité acceptable - Vérifier les données";
            else
                return "❌ Qualité insuffisante - Réviser les mesures";
        }
        
        public override string ToString()
        {
            return $"Compensation: σ₀={Sigma0*1000:F1}mm, Correction max={MaxCorrection*1000:F1}mm, {AdjustedPoints.Count} points";
        }
    }
}