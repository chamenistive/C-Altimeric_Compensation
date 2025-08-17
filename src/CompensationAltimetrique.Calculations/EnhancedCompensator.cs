using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using CompensationAltimetrique.Calculations.Validation;
using CompensationAltimetrique.Calculations.Algorithms;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Calculations
{
    /// <summary>
    /// Compensateur avec validation statistique complète
    /// Extension de LeastSquaresCompensator existant
    /// </summary>
    public class EnhancedCompensator : LeastSquaresCompensator
    {
        private readonly QualityAnalyzer _qualityAnalyzer;
        private readonly double _confidenceLevel;
        private readonly double _precisionMm;
        
        public EnhancedCompensator(
            double precisionMm = 2.0,
            double confidenceLevel = 0.95) 
            : base(precisionMm)
        {
            _confidenceLevel = confidenceLevel;
            _precisionMm = precisionMm;
            _qualityAnalyzer = new QualityAnalyzer(confidenceLevel);
        }
        
        /// <summary>
        /// Compensation avec validation statistique complète
        /// Adaptation pour les données de nivellement
        /// </summary>
        public DetailedCompensationResults CompensateWithFullValidation(List<LevelingData> levelingData, double initialAltitude = 125.456)
        {
            Console.WriteLine("🔧 Compensation avec validation statistique complète...");
            
            // Compensation standard
            var baseResults = base.Compensate(levelingData, initialAltitude);
            
            // Construction système matriciel pour analyse
            var matrixSystem = BuildMatrixSystemForAnalysis(levelingData, initialAltitude);
            
            // Analyse statistique complète
            var statistics = _qualityAnalyzer.AnalyzeCompensation(
                matrixSystem.DesignMatrix,
                matrixSystem.WeightMatrix,
                matrixSystem.Misclosures,
                Vector<double>.Build.DenseOfEnumerable(baseResults.AdjustedPoints.Select(p => p.Altitude)),
                Matrix<double>.Build.DenseIdentity(baseResults.AdjustedPoints.Count)); // Matrice covariance simplifiée
            
            // Détection de fautes grossières (utilise les résidus de base)
            var blunders = _qualityAnalyzer.DetectBlunders(
                baseResults.Residuals,
                statistics.DegreesOfFreedom);
            
            // Validation complète
            var validation = PerformFullValidation(statistics, blunders, baseResults);
            
            // Grade de qualité
            var qualityGrade = DetermineQualityGrade(statistics);
            
            // Recommandations
            var recommendations = GenerateRecommendations(statistics, blunders);
            
            Console.WriteLine($"   📊 Précision: {statistics.Sigma0Hat * 1000:F2}mm");
            Console.WriteLine($"   🧪 Test χ²: {(statistics.UnitWeightValid ? "✅ VALIDÉ" : "❌ ÉCHOUÉ")}");
            Console.WriteLine($"   🔍 Fautes détectées: {blunders.Count}");
            Console.WriteLine($"   🏆 Grade: {qualityGrade}");
            
            return new DetailedCompensationResults
            {
                // Propriétés héritées de CompensationResults
                Sigma0 = statistics.Sigma0Hat,
                Corrections = baseResults.Corrections,
                Residuals = baseResults.Residuals,
                RmsResiduals = baseResults.RmsResiduals,
                IsValid = statistics.UnitWeightValid && qualityGrade != QualityGrade.Failed,
                AdjustedPoints = baseResults.AdjustedPoints,
                MaxCorrection = baseResults.MaxCorrection,
                ClosureError = baseResults.ClosureError,
                TotalDistance = baseResults.TotalDistance,
                Iterations = baseResults.Iterations,
                PrecisionAchieved = statistics.Sigma0Hat * 1000 <= _precisionMm,
                ValidationDetails = baseResults.ValidationDetails,
                ErrorMessages = baseResults.ErrorMessages,
                WarningMessages = baseResults.WarningMessages,
                ComputationTime = baseResults.ComputationTime,
                Timestamp = baseResults.Timestamp,
                ProcessedPoints = baseResults.ProcessedPoints,
                Status = statistics.UnitWeightValid ? "Success" : "Warning",
                Details = baseResults.Details,
                Precision = statistics.Sigma0Hat,
                
                // Nouvelles propriétés
                Statistics = statistics,
                SuspectObservations = blunders,
                ValidationSummary = validation,
                QualityGrade = qualityGrade,
                Recommendations = recommendations
            };
        }
        
        private MatrixSystemForAnalysis BuildMatrixSystemForAnalysis(List<LevelingData> levelingData, double initialAltitude)
        {
            // Construction simplifiée pour l'analyse
            var validData = levelingData.Where(d => d.HasValidReadings()).ToList();
            int nObs = validData.Count;
            int nPoints = validData.Count + 1; // +1 pour le point de référence
            
            var A = Matrix<double>.Build.Dense(nObs + 1, nPoints);
            var P = Matrix<double>.Build.DenseIdentity(nObs + 1);
            var f = Vector<double>.Build.Dense(nObs + 1);
            
            // Point de référence (première ligne)
            f[0] = initialAltitude;
            A[0, 0] = 1.0;
            P[0, 0] = 1000.0; // Poids fort pour la référence
            
            // Dénivelées observées
            for (int i = 0; i < nObs; i++)
            {
                var dh = validData[i].CalculateAverageDenivelation();
                f[i + 1] = dh;
                A[i + 1, i] = -1.0;      // Point arrière
                A[i + 1, i + 1] = 1.0;   // Point avant
                
                // Poids basé sur la distance
                var dist = validData[i].GetAverageDistance() ?? 100.0;
                P[i + 1, i + 1] = 1.0 / Math.Max(0.001, dist / 1000.0); // km
            }
            
            return new MatrixSystemForAnalysis
            {
                DesignMatrix = A,
                WeightMatrix = P,
                Misclosures = f
            };
        }
        
        private ValidationSummary PerformFullValidation(
            CompensationStatistics statistics,
            List<BlunderDetection> blunders,
            CompensationResults baseResults)
        {
            var summary = new ValidationSummary();
            
            // Test χ² pour poids unitaire
            summary.Chi2Validation = new ValidationTestResult
            {
                IsValid = statistics.UnitWeightValid,
                Value = statistics.Chi2TestStatistic,
                CriticalValue = statistics.Chi2CriticalValue,
                Message = statistics.UnitWeightValid 
                    ? "Test χ² validé - Modèle stochastique cohérent"
                    : "Test χ² échoué - Modèle stochastique à réviser"
            };
            
            // Validation précision
            var precisionMm = statistics.Sigma0Hat * 1000;
            summary.PrecisionValidation = new ValidationTestResult
            {
                IsValid = precisionMm <= _precisionMm,
                Value = precisionMm,
                CriticalValue = _precisionMm,
                Message = precisionMm <= _precisionMm
                    ? $"Précision {precisionMm:F2}mm ≤ {_precisionMm}mm - CONFORME"
                    : $"Précision {precisionMm:F2}mm > {_precisionMm}mm - DÉPASSEMENT"
            };
            
            // Validation fautes grossières
            summary.BlunderValidation = new ValidationTestResult
            {
                IsValid = blunders.Count == 0,
                Value = blunders.Count,
                Message = blunders.Count == 0
                    ? "Aucune faute grossière détectée"
                    : $"{blunders.Count} observation(s) suspecte(s) détectée(s)"
            };
            
            // Validation géodésique
            summary.GeodeticValidation = ValidateGeodeticConstraints(baseResults);
            
            return summary;
        }
        
        private ValidationTestResult ValidateGeodeticConstraints(CompensationResults baseResults)
        {
            var result = new ValidationTestResult { IsValid = true };
            
            // Vérifier fermeture (utilise les propriétés existantes)
            var closureErrorMm = Math.Abs(baseResults.ClosureError * 1000);
            var toleranceMm = _precisionMm * Math.Sqrt(baseResults.TotalDistance / 1000.0);
            
            if (closureErrorMm > toleranceMm)
            {
                result.IsValid = false;
                result.Message = $"Erreur de fermeture {closureErrorMm:F2}mm > tolérance {toleranceMm:F2}mm";
            }
            else
            {
                result.Message = $"Fermeture acceptable: {closureErrorMm:F2}mm ≤ {toleranceMm:F2}mm";
            }
            
            result.Value = closureErrorMm;
            result.CriticalValue = toleranceMm;
            
            return result;
        }
        
        private QualityGrade DetermineQualityGrade(CompensationStatistics statistics)
        {
            var precisionMm = statistics.Sigma0Hat * 1000;
            
            if (!statistics.UnitWeightValid)
                return QualityGrade.Failed;
            
            return precisionMm switch
            {
                <= 1.0 => QualityGrade.Excellent,
                <= 2.0 => QualityGrade.Good,
                <= 5.0 => QualityGrade.Acceptable,
                _ => QualityGrade.Poor
            };
        }
        
        private List<string> GenerateRecommendations(
            CompensationStatistics statistics, 
            List<BlunderDetection> blunders)
        {
            var recommendations = new List<string>();
            
            // Recommandations selon précision
            var precisionMm = statistics.Sigma0Hat * 1000;
            if (precisionMm > 2.0)
            {
                recommendations.Add("🎯 Précision insuffisante - Vérifier qualité des observations");
                recommendations.Add("📏 Contrôler calibration des instruments");
            }
            
            // Recommandations selon test χ²
            if (!statistics.UnitWeightValid)
            {
                recommendations.Add("📊 Test χ² échoué - Réviser modèle stochastique");
                recommendations.Add("⚖️ Ajuster poids des observations selon distances");
            }
            
            // Recommandations selon fautes grossières
            if (blunders.Count > 0)
            {
                recommendations.Add($"🔍 {blunders.Count} observation(s) suspecte(s) à vérifier");
                recommendations.Add("📝 Contrôler lectures sur terrain");
                
                // Détailler observations problématiques
                foreach (var blunder in blunders.Take(3))
                {
                    recommendations.Add($"   - Observation {blunder.ObservationIndex}: résidu normalisé = {blunder.NormalizedResidual:F2}");
                }
            }
            
            // Recommandations positives
            if (precisionMm <= 2.0 && statistics.UnitWeightValid && blunders.Count == 0)
            {
                recommendations.Add("✅ Excellente qualité - Maintenir procédures actuelles");
            }
            
            return recommendations;
        }
    }
}