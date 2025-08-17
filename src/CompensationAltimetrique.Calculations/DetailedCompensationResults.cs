using System.Collections.Generic;
using CompensationAltimetrique.Calculations.Validation;
using CompensationAltimetrique.Core.Models;
using MathNet.Numerics.LinearAlgebra;

namespace CompensationAltimetrique.Calculations
{
    /// <summary>
    /// Résultats de compensation avec validation statistique complète
    /// Extension de CompensationResults
    /// </summary>
    public class DetailedCompensationResults : CompensationResults
    {
        /// <summary>
        /// Statistiques de compensation (χ², σ₀, etc.)
        /// </summary>
        public CompensationStatistics? Statistics { get; set; }
        
        /// <summary>
        /// Observations suspectes détectées
        /// </summary>
        public List<BlunderDetection> SuspectObservations { get; set; } = new List<BlunderDetection>();
        
        /// <summary>
        /// Résumé de validation complet
        /// </summary>
        public ValidationSummary? ValidationSummary { get; set; }
        
        /// <summary>
        /// Grade de qualité du nivellement
        /// </summary>
        public QualityGrade QualityGrade { get; set; }
        
        /// <summary>
        /// Recommandations d'amélioration
        /// </summary>
        public List<string> Recommendations { get; set; } = new List<string>();
        
        /// <summary>
        /// Génère un résumé exécutif
        /// </summary>
        public string GetExecutiveSummary()
        {
            var status = ValidationSummary?.IsFullyValid == true ? "✅ CONFORME" : "⚠️ ATTENTION REQUISE";
            var precision = Statistics?.Sigma0Hat * 1000 ?? 0;
            
            return $"""
            🎯 RÉSUMÉ EXÉCUTIF - COMPENSATION ALTIMÉTRIQUE
            ============================================
            
            📊 STATUT: {status}
            🎯 Grade de qualité: {QualityGrade}
            📏 Précision atteinte: {precision:F2}mm
            🧪 Test χ²: {(Statistics?.UnitWeightValid == true ? "VALIDÉ" : "ÉCHOUÉ")}
            🔍 Observations suspectes: {SuspectObservations.Count}
            📈 Degrés de liberté: {Statistics?.DegreesOfFreedom ?? 0}
            """;
        }
    }
    
    /// <summary>
    /// Grade de qualité du nivellement
    /// </summary>
    public enum QualityGrade
    {
        Excellent,  // σ₀ ≤ 1.0mm
        Good,       // 1.0mm < σ₀ ≤ 2.0mm  
        Acceptable, // 2.0mm < σ₀ ≤ 5.0mm
        Poor,       // σ₀ > 5.0mm
        Failed      // Tests statistiques échoués
    }
    
    /// <summary>
    /// Système matriciel pour analyse
    /// </summary>
    public class MatrixSystemForAnalysis
    {
        public Matrix<double>? DesignMatrix { get; set; }
        public Matrix<double>? WeightMatrix { get; set; }
        public Vector<double>? Misclosures { get; set; }
    }
}