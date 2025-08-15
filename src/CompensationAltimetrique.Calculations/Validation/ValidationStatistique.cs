// ================================================================
// ÉTAPE 2 - ARCHITECTURE DE VALIDATION STATISTIQUE
// Transposition FIDÈLE du module Python validators.py + compensator.py
// Tests χ², Student, résidus normalisés selon théorie géodésique
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Calculations.Validation
{
    /// <summary>
    /// Résultat de validation avec détails.
    /// Transposition exacte de la dataclass ValidationResult Python.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> Details { get; set; } = new();

        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        public void AddInfo(string info)
        {
            if (!Details.ContainsKey("info"))
                Details["info"] = new List<string>();
            ((List<string>)Details["info"]).Add(info);
        }

        public void AddSuccess(string success)
        {
            if (!Details.ContainsKey("success"))
                Details["success"] = new List<string>();
            ((List<string>)Details["success"]).Add(success);
        }

        public string GetSummary()
        {
            var summary = IsValid ? "✅ VALIDATION RÉUSSIE" : "❌ VALIDATION ÉCHOUÉE";

            if (Errors.Count > 0)
            {
                summary += $"\n🔴 Erreurs ({Errors.Count}):";
                foreach (var error in Errors)
                    summary += $"\n  • {error}";
            }

            if (Warnings.Count > 0)
            {
                summary += $"\n🟡 Avertissements ({Warnings.Count}):";
                foreach (var warning in Warnings)
                    summary += $"\n  • {warning}";
            }

            return summary;
        }
    }

    /// <summary>
    /// Statistiques de compensation selon la théorie géodésique.
    /// Transposition exacte de CompensationStatistics Python.
    /// </summary>
    public class CompensationStatistics
    {
        /// <summary>Écart-type a posteriori σ₀</summary>
        public double Sigma0Hat { get; set; }

        /// <summary>Degrés de liberté (n_obs - n_unknowns)</summary>
        public int DegreesOfFreedom { get; set; }

        /// <summary>Statistique du test χ² (v^T P v)</summary>
        public double Chi2TestStatistic { get; set; }

        /// <summary>Valeur critique χ² au seuil de confiance</summary>
        public double Chi2CriticalValue { get; set; }

        /// <summary>Validation du poids unitaire (test χ²)</summary>
        public bool UnitWeightValid { get; set; }

        /// <summary>Résidu normalisé maximum</summary>
        public double MaxStandardizedResidual { get; set; }

        /// <summary>Seuil de détection des fautes grossières (Student)</summary>
        public double BlunderDetectionThreshold { get; set; }

        /// <summary>Niveau de confiance utilisé</summary>
        public double ConfidenceLevel { get; set; } = 0.95;
    }

    /// <summary>
    /// Analyseur de qualité statistique selon la théorie des moindres carrés.
    /// TRANSPOSITION EXACTE de QualityAnalyzer Python.
    /// 
    /// Calcule les statistiques de qualité et détecte les fautes grossières
    /// selon la théorie statistique des moindres carrés.
    /// </summary>
    public class QualityAnalyzer
    {
        private readonly double _confidenceLevel;
        private readonly double _alpha;

        public QualityAnalyzer(double confidenceLevel = 0.95)
        {
            _confidenceLevel = confidenceLevel;
            _alpha = 1 - confidenceLevel;
        }

        /// <summary>
        /// Analyse statistique complète de la compensation.
        /// TRANSPOSITION EXACTE de analyze_compensation Python.
        /// 
        /// Calcule:
        /// - Écart-type a posteriori σ₀
        /// - Test du poids unitaire (χ²)
        /// - Résidus normalisés
        /// - Détection des fautes grossières
        /// </summary>
        public CompensationStatistics AnalyzeCompensation(
            Matrix<double> designMatrix,   // A
            Matrix<double> weightMatrix,   // P
            Vector<double> misclosures,    // f
            Vector<double> corrections,    // x_hat
            Matrix<double> covarianceMatrix) // Qx
        {
            try
            {
                var A = designMatrix;
                var P = weightMatrix;
                var f = misclosures;
                var x_hat = corrections;

                int n_obs = A.RowCount;
                int n_unknowns = A.ColumnCount;

                // Calcul des résidus (équivalent Python: v = A @ x_hat - f)
                var v = A * x_hat - f;

                // Degrés de liberté (équivalent Python: r = n_obs - n_unknowns)
                int r = n_obs - n_unknowns;

                if (r <= 0)
                {
                    throw new InvalidOperationException(
                        $"Système sous-déterminé: {n_obs} observations, {n_unknowns} inconnues");
                }

                // Écart-type a posteriori (équivalent Python: vtPv = (v.T @ P @ v)[0, 0])
                var vtPv_vector = v.ToRowMatrix() * P * v.ToColumnMatrix();
                double vtPv = vtPv_vector[0, 0];
                double sigma_0_hat = Math.Sqrt(vtPv / r);

                // Test du χ² pour le poids unitaire (équivalent Python)
                double chi2_statistic = vtPv;
                var chi2Dist = new ChiSquared(r);
                double chi2_critical = chi2Dist.InverseCumulativeDistribution(_confidenceLevel);
                bool unit_weight_valid = chi2_statistic <= chi2_critical;

                // Résidus normalisés pour détection de fautes (équivalent Python)
                var normalized_residuals = CalculateNormalizedResiduals(v, A, P, covarianceMatrix, sigma_0_hat);
                double max_normalized_residual = normalized_residuals.AbsoluteMaximum();

                // Seuil de détection des fautes (test de Student)
                // Python: t_critical = t.ppf(1 - self.alpha/2, r)  # Test bilatéral
                var tDist = new StudentT(0, 1, r);
                double t_critical = tDist.InverseCumulativeDistribution(1 - _alpha / 2);

                return new CompensationStatistics
                {
                    Sigma0Hat = sigma_0_hat,
                    DegreesOfFreedom = r,
                    Chi2TestStatistic = chi2_statistic,
                    Chi2CriticalValue = chi2_critical,
                    UnitWeightValid = unit_weight_valid,
                    MaxStandardizedResidual = max_normalized_residual,
                    BlunderDetectionThreshold = t_critical,
                    ConfidenceLevel = _confidenceLevel
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur analyse statistique: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calcul des résidus normalisés.
        /// TRANSPOSITION EXACTE de _calculate_normalized_residuals Python.
        /// 
        /// Formule: r̂ᵢ = vᵢ / (σ₀ √qᵥᵥᵢ)
        /// </summary>
        private Vector<double> CalculateNormalizedResiduals(
            Vector<double> residuals,      // v
            Matrix<double> designMatrix,   // A
            Matrix<double> weightMatrix,   // P
            Matrix<double> covarianceMatrix, // Qx
            double sigma0)
        {
            try
            {
                var A = designMatrix;
                var P = weightMatrix;
                var Qx = covarianceMatrix;

                // Matrice de covariance des résidus: Qv = P^(-1) - A * Qx * A^T
                // Python: Qv = P_inv - A @ Qx @ A.T
                var P_inv = P.Inverse();
                var Qv = P_inv - A * Qx * A.Transpose();

                // Résidus normalisés: r̂ᵢ = vᵢ / (σ₀ √qᵥᵥᵢ)
                var normalizedResiduals = Vector<double>.Build.Dense(residuals.Count);

                for (int i = 0; i < residuals.Count; i++)
                {
                    double qvv_i = Qv[i, i]; // Élément diagonal de Qv
                    if (qvv_i > 0)
                    {
                        normalizedResiduals[i] = residuals[i] / (sigma0 * Math.Sqrt(qvv_i));
                    }
                    else
                    {
                        normalizedResiduals[i] = 0.0; // Éviter division par zéro
                    }
                }

                return normalizedResiduals;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur calcul résidus normalisés: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Détection des fautes grossières selon le test de Student.
        /// Équivalent de detect_blunders Python.
        /// </summary>
        public List<BlunderDetection> DetectBlunders(Vector<double> normalizedResiduals, int degreesOfFreedom)
        {
            var blunders = new List<BlunderDetection>();
            var tDist = new StudentT(0, 1, degreesOfFreedom);
            double t_critical = tDist.InverseCumulativeDistribution(1 - _alpha / 2);

            for (int i = 0; i < normalizedResiduals.Count; i++)
            {
                double residual = normalizedResiduals[i];
                if (Math.Abs(residual) > t_critical)
                {
                    blunders.Add(new BlunderDetection
                    {
                        ObservationIndex = i,
                        NormalizedResidual = residual,
                        Significance = Math.Abs(residual) / t_critical,
                        Critical = t_critical
                    });
                }
            }

            return blunders;
        }
    }

    /// <summary>
    /// Détection de faute grossière.
    /// </summary>
    public class BlunderDetection
    {
        public int ObservationIndex { get; set; }
        public double NormalizedResidual { get; set; }
        public double Significance { get; set; }
        public double Critical { get; set; }
    }

    /// <summary>
    /// Validateur de précision 2mm.
    /// TRANSPOSITION EXACTE de PrecisionValidator Python.
    /// </summary>
    public class PrecisionValidator
    {
        private readonly double _targetPrecisionMm;
        private readonly double _targetPrecisionM;

        public PrecisionValidator(double targetPrecisionMm = 2.0)
        {
            _targetPrecisionMm = targetPrecisionMm;
            _targetPrecisionM = targetPrecisionMm / 1000.0;

            if (targetPrecisionMm <= 0)
                throw new ArgumentException("La précision cible doit être positive", nameof(targetPrecisionMm));
        }

        /// <summary>
        /// Validation de l'erreur de fermeture.
        /// TRANSPOSITION EXACTE de validate_closure_error Python.
        /// </summary>
        public ValidationResult ValidateClosureError(double closureErrorM, double totalDistanceKm, string traverseType = "fermé")
        {
            var result = new ValidationResult();

            try
            {
                // Conversion en millimètres (équivalent Python)
                double closureErrorMm = Math.Abs(closureErrorM * 1000);

                // Calcul de la tolérance selon la norme géodésique
                // Python: tolerance_mm = 4 * np.sqrt(total_distance_km)
                double toleranceMm = 4 * Math.Sqrt(totalDistanceKm);

                // Validation (équivalent Python)
                bool isAcceptable = closureErrorMm <= toleranceMm;

                // Détails (équivalent Python)
                result.Details = new Dictionary<string, object>
                {
                    ["closure_error_mm"] = Math.Round(closureErrorMm, 2),
                    ["tolerance_mm"] = Math.Round(toleranceMm, 2),
                    ["total_distance_km"] = totalDistanceKm,
                    ["traverse_type"] = traverseType,
                    ["precision_ratio"] = toleranceMm > 0 ? closureErrorMm / toleranceMm : 0,
                    ["target_precision_mm"] = _targetPrecisionMm
                };

                if (!isAcceptable)
                {
                    result.AddError($"Erreur de fermeture {closureErrorMm:F2}mm > tolérance {toleranceMm:F2}mm");
                }

                // Vérifier si respecte l'objectif 2mm (équivalent Python)
                if (closureErrorMm > _targetPrecisionMm)
                {
                    result.AddWarning($"Erreur de fermeture {closureErrorMm:F2}mm > objectif {_targetPrecisionMm}mm");
                }
            }
            catch (Exception e)
            {
                result.AddError($"Erreur validation fermeture: {e.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validation des ajustements de compensation.
        /// TRANSPOSITION EXACTE de validate_adjustments Python.
        /// </summary>
        public ValidationResult ValidateAdjustments(Vector<double> adjustmentsM)
        {
            var result = new ValidationResult();

            try
            {
                if (adjustmentsM == null || adjustmentsM.Count == 0)
                {
                    result.AddError("Aucun ajustement fourni");
                    return result;
                }

                // Conversion en millimètres
                var adjustmentsMm = adjustmentsM.Map(x => x * 1000);

                // Statistiques des ajustements
                double maxAdjustmentMm = adjustmentsMm.AbsoluteMaximum();
                double rmsAdjustmentMm = Math.Sqrt(adjustmentsMm.Map(x => x * x).Sum() / adjustmentsMm.Count);

                result.Details = new Dictionary<string, object>
                {
                    ["max_adjustment_mm"] = Math.Round(maxAdjustmentMm, 2),
                    ["rms_adjustment_mm"] = Math.Round(rmsAdjustmentMm, 2),
                    ["target_precision_mm"] = _targetPrecisionMm,
                    ["precision_achieved"] = maxAdjustmentMm <= _targetPrecisionMm
                };

                // Validation selon critère 2mm
                if (maxAdjustmentMm > _targetPrecisionMm)
                {
                    result.AddWarning($"Ajustement maximal {maxAdjustmentMm:F2}mm > objectif {_targetPrecisionMm}mm");
                }
                else
                {
                    result.AddSuccess($"Précision {_targetPrecisionMm}mm atteinte (max: {maxAdjustmentMm:F2}mm)");
                }

                // Vérification de cohérence des ajustements
                if (rmsAdjustmentMm > _targetPrecisionMm * 2)
                {
                    result.AddWarning($"RMS des ajustements élevé: {rmsAdjustmentMm:F2}mm");
                }
            }
            catch (Exception e)
            {
                result.AddError($"Erreur validation ajustements: {e.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validation des résidus statistiques.
        /// </summary>
        public ValidationResult ValidateResiduals(Vector<double> residuals, CompensationStatistics statistics)
        {
            var result = new ValidationResult();

            try
            {
                var residualsMm = residuals.Map(x => x * 1000);
                double maxResidualMm = residualsMm.AbsoluteMaximum();
                double rmsResidualMm = Math.Sqrt(residualsMm.Map(x => x * x).Sum() / residualsMm.Count);

                result.Details = new Dictionary<string, object>
                {
                    ["max_residual_mm"] = Math.Round(maxResidualMm, 2),
                    ["rms_residual_mm"] = Math.Round(rmsResidualMm, 2),
                    ["sigma0_mm"] = Math.Round(statistics.Sigma0Hat * 1000, 2),
                    ["chi2_valid"] = statistics.UnitWeightValid,
                    ["max_standardized_residual"] = Math.Round(statistics.MaxStandardizedResidual, 2)
                };

                // Validation test χ²
                if (!statistics.UnitWeightValid)
                {
                    result.AddWarning("Test χ² échoué - modèle stochastique à réviser");
                }
                else
                {
                    result.AddSuccess("Test χ² validé - modèle stochastique cohérent");
                }

                // Validation résidus normalisés
                if (statistics.MaxStandardizedResidual > statistics.BlunderDetectionThreshold)
                {
                    result.AddWarning($"Fautes grossières détectées (seuil: {statistics.BlunderDetectionThreshold:F2})");
                }
                else
                {
                    result.AddSuccess("Aucune faute grossière détectée");
                }
            }
            catch (Exception e)
            {
                result.AddError($"Erreur validation résidus: {e.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// Validateur géodésique pour contrôles de cohérence.
    /// Équivalent de GeodeticValidator Python.
    /// </summary>
    public class GeodeticValidator
    {
        /// <summary>
        /// Validation des distances de visée.
        /// </summary>
        public ValidationResult ValidateDistances(IEnumerable<double> distances)
        {
            var result = new ValidationResult();

            try
            {
                var distanceList = distances.ToList();
                if (!distanceList.Any())
                {
                    result.AddWarning("Aucune distance fournie");
                    return result;
                }

                double minDistance = distanceList.Min();
                double maxDistance = distanceList.Max();
                double avgDistance = distanceList.Average();

                result.Details = new Dictionary<string, object>
                {
                    ["min_distance_m"] = Math.Round(minDistance, 1),
                    ["max_distance_m"] = Math.Round(maxDistance, 1),
                    ["avg_distance_m"] = Math.Round(avgDistance, 1),
                    ["count"] = distanceList.Count
                };

                // Validation plages recommandées pour nivellement de précision
                if (minDistance < 5.0)
                {
                    result.AddWarning($"Distance minimale très courte: {minDistance:F1}m");
                }

                if (maxDistance > 100.0)
                {
                    result.AddWarning($"Distance maximale importante: {maxDistance:F1}m (corrections atmosphériques recommandées)");
                }

                if (avgDistance > 60.0)
                {
                    result.AddWarning($"Distance moyenne élevée: {avgDistance:F1}m");
                }
                else
                {
                    result.AddSuccess($"Distances de visée appropriées (moyenne: {avgDistance:F1}m)");
                }
            }
            catch (Exception e)
            {
                result.AddError($"Erreur validation distances: {e.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// Validateur de compensation intégrant tous les contrôles.
    /// Équivalent de CompensationValidator Python.
    /// </summary>
    public class CompensationValidator
    {
        private readonly PrecisionValidator _precisionValidator;
        private readonly GeodeticValidator _geodeticValidator;
        private readonly QualityAnalyzer _qualityAnalyzer;

        public CompensationValidator(double targetPrecisionMm = 2.0, double confidenceLevel = 0.95)
        {
            _precisionValidator = new PrecisionValidator(targetPrecisionMm);
            _geodeticValidator = new GeodeticValidator();
            _qualityAnalyzer = new QualityAnalyzer(confidenceLevel);
        }

        /// <summary>
        /// Validation complète de la compensation.
        /// Intègre tous les contrôles Python.
        /// </summary>
        public ValidationResult ValidateCompensation(
            Vector<double> residuals,
            Vector<double> adjustments,
            CompensationStatistics statistics,
            double closureError,
            double totalDistance,
            IEnumerable<double> distances)
        {
            var result = new ValidationResult();

            try
            {
                // 1. Validation de la fermeture
                var closureValidation = _precisionValidator.ValidateClosureError(closureError, totalDistance);
                if (!closureValidation.IsValid)
                {
                    foreach (var error in closureValidation.Errors)
                        result.AddError(error);
                }
                foreach (var warning in closureValidation.Warnings)
                    result.AddWarning(warning);

                // 2. Validation des ajustements
                var adjustmentValidation = _precisionValidator.ValidateAdjustments(adjustments);
                foreach (var warning in adjustmentValidation.Warnings)
                    result.AddWarning(warning);

                // 3. Validation des résidus
                var residualValidation = _precisionValidator.ValidateResiduals(residuals, statistics);
                foreach (var warning in residualValidation.Warnings)
                    result.AddWarning(warning);

                // 4. Validation des distances
                var distanceValidation = _geodeticValidator.ValidateDistances(distances);
                foreach (var warning in distanceValidation.Warnings)
                    result.AddWarning(warning);

                // 5. Consolidation des détails
                result.Details = new Dictionary<string, object>
                {
                    ["closure_validation"] = closureValidation.Details,
                    ["adjustment_validation"] = adjustmentValidation.Details,
                    ["residual_validation"] = residualValidation.Details,
                    ["distance_validation"] = distanceValidation.Details,
                    ["overall_statistics"] = new
                    {
                        sigma0_mm = Math.Round(statistics.Sigma0Hat * 1000, 2),
                        chi2_valid = statistics.UnitWeightValid,
                        precision_achieved = (bool)(adjustmentValidation.Details["precision_achieved"] ?? false)
                    }
                };

                // Validation globale
                bool precisionAchieved = (bool)(adjustmentValidation.Details["precision_achieved"] ?? false);
                if (precisionAchieved && statistics.UnitWeightValid)
                {
                    result.AddSuccess("🎯 COMPENSATION VALIDÉE - Précision 2mm atteinte");
                }
                else if (precisionAchieved)
                {
                    result.AddWarning("⚠️ Précision atteinte mais modèle stochastique à réviser");
                }
                else
                {
                    result.AddWarning("⚠️ Précision cible non atteinte");
                }
            }
            catch (Exception e)
            {
                result.AddError($"Erreur validation compensation: {e.Message}");
            }

            return result;
        }
    }
}