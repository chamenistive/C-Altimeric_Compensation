using System;
using System.Collections.Generic;
using System.Linq;

namespace CompensationAltimetrique.Calculations.Validation
{
    /// <summary>
    /// Validateur de précision selon normes géodésiques
    /// </summary>
    public class GeodeticPrecisionValidator
    {
        private readonly double _precisionMm;
        private readonly double _toleranceFactorMm = 4.0; // Tolérance = 4√K mm
        
        public GeodeticPrecisionValidator(double precisionMm = 2.0)
        {
            _precisionMm = precisionMm;
        }
        
        /// <summary>
        /// Calcul de la tolérance de fermeture
        /// T = 4√K (mm) où K est la distance totale en km
        /// </summary>
        public double CalculateClosureTolerance(double totalDistanceKm)
        {
            return _toleranceFactorMm * Math.Sqrt(totalDistanceKm);
        }
        
        /// <summary>
        /// Validation de la fermeture d'un cheminement
        /// </summary>
        public ValidationResult ValidateClosure(double closureErrorMm, double totalDistanceKm)
        {
            var result = new ValidationResult { IsValid = true };
            
            double tolerance = CalculateClosureTolerance(totalDistanceKm);
            result.Details["tolerance_mm"] = tolerance;
            result.Details["closure_error_mm"] = closureErrorMm;
            result.Details["distance_km"] = totalDistanceKm;
            
            if (Math.Abs(closureErrorMm) > tolerance)
            {
                result.AddError($"Erreur de fermeture {closureErrorMm:F1}mm dépasse la tolérance {tolerance:F1}mm");
                result.Details["depassement_percent"] = (Math.Abs(closureErrorMm) / tolerance - 1) * 100;
            }
            else if (Math.Abs(closureErrorMm) > tolerance * 0.8)
            {
                result.AddWarning($"Erreur de fermeture {closureErrorMm:F1}mm proche de la tolérance {tolerance:F1}mm");
            }
            
            // Ratio de précision
            double precisionRatio = Math.Abs(closureErrorMm) / tolerance;
            result.Details["precision_ratio"] = precisionRatio;
            
            return result;
        }
        
        /// <summary>
        /// Validation des dénivelées entre instruments
        /// </summary>
        public ValidationResult ValidateInstrumentalControl(List<double> denivelations)
        {
            var result = new ValidationResult { IsValid = true };
            
            if (denivelations.Count < 2)
            {
                result.AddWarning("Pas de contrôle instrumental (un seul instrument)");
                return result;
            }
            
            double mean = denivelations.Average();
            double maxDiff = denivelations.Max(d => Math.Abs(d - mean)) * 1000; // en mm
            
            result.Details["mean_denivelation_m"] = mean;
            result.Details["max_difference_mm"] = maxDiff;
            result.Details["instrument_count"] = denivelations.Count;
            
            const double CONTROL_TOLERANCE_MM = 5.0;
            
            if (maxDiff > CONTROL_TOLERANCE_MM)
            {
                result.AddError($"Écart entre instruments {maxDiff:F2}mm > {CONTROL_TOLERANCE_MM}mm");
            }
            else if (maxDiff > CONTROL_TOLERANCE_MM * 0.6)
            {
                result.AddWarning($"Écart entre instruments {maxDiff:F2}mm proche de la limite");
            }
            
            return result;
        }
        
        /// <summary>
        /// Validation des résidus après compensation
        /// </summary>
        public ValidationResult ValidateResiduals(double[] residuals, double sigmaPosteriori)
        {
            var result = new ValidationResult { IsValid = true };
            
            double maxResidualMm = residuals.Max(r => Math.Abs(r)) * 1000;
            double rmsResidualMm = Math.Sqrt(residuals.Sum(r => r * r) / residuals.Length) * 1000;
            
            result.Details["max_residual_mm"] = maxResidualMm;
            result.Details["rms_residual_mm"] = rmsResidualMm;
            result.Details["sigma_posteriori"] = sigmaPosteriori;
            
            // Critère de précision 2mm
            if (maxResidualMm > _precisionMm * 3)
            {
                result.AddError($"Résidu max {maxResidualMm:F2}mm > 3×{_precisionMm}mm");
            }
            else if (maxResidualMm > _precisionMm * 2)
            {
                result.AddWarning($"Résidu max {maxResidualMm:F2}mm > 2×{_precisionMm}mm");
            }
            
            if (rmsResidualMm > _precisionMm)
            {
                result.AddWarning($"RMS des résidus {rmsResidualMm:F2}mm > {_precisionMm}mm");
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Validateur statistique pour compensation
    /// </summary>
    public class GeodeticStatisticalValidator
    {
        private readonly double _confidenceLevel;
        
        public GeodeticStatisticalValidator(double confidenceLevel = 0.95)
        {
            _confidenceLevel = confidenceLevel;
        }
        
        /// <summary>
        /// Test du χ² pour validation du poids unitaire
        /// </summary>
        public ValidationResult ValidateUnitWeight(double vtPv, int degreesOfFreedom)
        {
            var result = new ValidationResult { IsValid = true };
            
            // Calcul de la valeur critique du χ²
            double alpha = 1 - _confidenceLevel;
            double chi2Critical = CalculateChi2Critical(degreesOfFreedom, _confidenceLevel);
            
            result.Details["chi2_statistic"] = vtPv;
            result.Details["chi2_critical"] = chi2Critical;
            result.Details["degrees_of_freedom"] = degreesOfFreedom;
            
            if (vtPv > chi2Critical)
            {
                result.AddError($"Test χ² échoué: {vtPv:F2} > {chi2Critical:F2}");
                result.AddWarning("Le modèle stochastique pourrait être inadéquat");
            }
            
            return result;
        }
        
        /// <summary>
        /// Détection des fautes grossières (test de Student)
        /// </summary>
        public ValidationResult DetectBlunders(double[] normalizedResiduals, int degreesOfFreedom)
        {
            var result = new ValidationResult { IsValid = true };
            
            double tCritical = CalculateTCritical(degreesOfFreedom, _confidenceLevel);
            var blunders = new List<int>();
            
            for (int i = 0; i < normalizedResiduals.Length; i++)
            {
                if (Math.Abs(normalizedResiduals[i]) > tCritical)
                {
                    blunders.Add(i);
                }
            }
            
            result.Details["t_critical"] = tCritical;
            result.Details["blunder_count"] = blunders.Count;
            result.Details["blunder_indices"] = blunders;
            
            if (blunders.Count > 0)
            {
                result.AddWarning($"{blunders.Count} observation(s) suspecte(s) détectée(s)");
                result.Details["max_normalized_residual"] = normalizedResiduals.Max(Math.Abs);
            }
            
            return result;
        }
        
        /// <summary>
        /// Calcul approximatif de la valeur critique du χ²
        /// </summary>
        private double CalculateChi2Critical(int df, double confidence)
        {
            // Approximation de Wilson-Hilferty pour χ²
            double z = GetZScore(confidence);
            double a = 2.0 / (9.0 * df);
            return df * Math.Pow(1 - a + z * Math.Sqrt(a), 3);
        }
        
        /// <summary>
        /// Calcul approximatif de la valeur critique de Student
        /// </summary>
        private double CalculateTCritical(int df, double confidence)
        {
            // Approximation pour grandes valeurs de df
            if (df > 30)
            {
                return GetZScore(confidence);
            }
            
            // Table simplifiée pour petites valeurs
            double alpha = 1 - confidence;
            double[] tValues = { 12.706, 4.303, 3.182, 2.776, 2.571, 2.447, 2.365, 2.306, 2.262, 2.228 };
            
            if (df <= 10)
                return tValues[df - 1];
            else
                return 2.228 - (df - 10) * 0.01; // Approximation linéaire
        }
        
        /// <summary>
        /// Score Z pour niveau de confiance
        /// </summary>
        private double GetZScore(double confidence)
        {
            // Valeurs standards
            if (Math.Abs(confidence - 0.95) < 0.001) return 1.96;
            if (Math.Abs(confidence - 0.99) < 0.001) return 2.576;
            if (Math.Abs(confidence - 0.90) < 0.001) return 1.645;
            
            // Approximation générale
            return 1.96; // Par défaut pour 95%
        }
    }
    
    /// <summary>
    /// Validateur de structure de données
    /// </summary>
    public class DataStructureValidator
    {
        /// <summary>
        /// Validation des colonnes AR/AV
        /// </summary>
        public ValidationResult ValidateColumns(List<string> arColumns, List<string> avColumns)
        {
            var result = new ValidationResult { IsValid = true };
            
            if (arColumns.Count == 0)
            {
                result.AddError("Aucune colonne AR trouvée");
            }
            
            if (avColumns.Count == 0)
            {
                result.AddError("Aucune colonne AV trouvée");
            }
            
            if (arColumns.Count != avColumns.Count)
            {
                result.AddError($"Nombre de colonnes AR ({arColumns.Count}) ≠ AV ({avColumns.Count})");
            }
            
            result.Details["ar_count"] = arColumns.Count;
            result.Details["av_count"] = avColumns.Count;
            
            return result;
        }
        
        /// <summary>
        /// Validation des valeurs de lectures
        /// </summary>
        public ValidationResult ValidateReadings(double ar, double av, string pointId)
        {
            var result = new ValidationResult { IsValid = true };
            
            // Plages normales pour mires de nivellement
            const double MIN_READING = -10.0;
            const double MAX_READING = 10.0;
            
            if (ar < MIN_READING || ar > MAX_READING)
            {
                result.AddError($"Lecture AR {ar:F4}m hors plage [{MIN_READING}, {MAX_READING}] au point {pointId}");
            }
            
            if (av < MIN_READING || av > MAX_READING)
            {
                result.AddError($"Lecture AV {av:F4}m hors plage [{MIN_READING}, {MAX_READING}] au point {pointId}");
            }
            
            // Vérifier la dénivelée
            double denivelation = ar - av;
            const double MAX_DENIVELATION = 50.0;
            
            if (Math.Abs(denivelation) > MAX_DENIVELATION)
            {
                result.AddError($"Dénivelée {denivelation:F2}m excessive au point {pointId}");
            }
            
            result.Details["ar"] = ar;
            result.Details["av"] = av;
            result.Details["denivelation"] = denivelation;
            result.Details["point_id"] = pointId;
            
            return result;
        }
    }
}