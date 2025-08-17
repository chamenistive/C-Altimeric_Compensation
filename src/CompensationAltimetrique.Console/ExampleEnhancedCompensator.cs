using System;
using System.Collections.Generic;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Calculations;

namespace CompensationAltimetrique.Console
{
    /// <summary>
    /// Exemple d'utilisation d'EnhancedCompensator avec validation statistique complète
    /// </summary>
    public class ExampleEnhancedCompensator
    {
        /// <summary>
        /// Démonstration de l'utilisation d'EnhancedCompensator
        /// </summary>
        public static void RunExample()
        {
            System.Console.WriteLine("=== EXEMPLE: EnhancedCompensator avec Validation Statistique ===");
            
            // ================================================================
            // REMPLACER cette section dans Program.cs:
            // var compensation = compensator.Compensate(results);
            // ================================================================
            
            // Configuration du calculateur amélioré avec corrections atmosphériques
            var calculator = new EnhancedLevelingCalculator(
                precisionMm: 2.0,
                applyAtmosphericCorrections: true);
            
            // Données de test
            var levelingData = CreateTestData();
            
            // Calcul avec corrections atmosphériques
            var results = calculator.Calculate(levelingData, initialAltitude: 125.456);
            
            // ================================================================
            // NOUVELLE APPROCHE: Compensation avec validation statistique complète
            // ================================================================
            
            DetailedCompensationResults compensation;
            if (calculator is EnhancedLevelingCalculator)
            {
                var enhancedCompensator = new EnhancedCompensator(precisionMm: 2.0);
                compensation = enhancedCompensator.CompensateWithFullValidation(levelingData);
                
                // Afficher résumé exécutif
                System.Console.WriteLine("\n" + compensation.GetExecutiveSummary());
                
                // Afficher recommandations si nécessaire
                if (compensation.Recommendations.Count > 0)
                {
                    System.Console.WriteLine("\n💡 RECOMMANDATIONS:");
                    foreach (var recommendation in compensation.Recommendations)
                    {
                        System.Console.WriteLine($"   {recommendation}");
                    }
                }
                
                // Afficher détails de validation
                if (compensation.ValidationSummary != null)
                {
                    System.Console.WriteLine("\n" + compensation.ValidationSummary.GetExecutiveSummary());
                }
            }
            else
            {
                // Fallback vers méthode standard (non applicable dans cet exemple)
                compensation = new DetailedCompensationResults
                {
                    Status = "Fallback",
                    ValidationDetails = results.ValidationDetails
                };
            }
            
            // Affichage des résultats détaillés
            DisplayDetailedResults(compensation);
        }
        
        private static List<LevelingData> CreateTestData()
        {
            return new List<LevelingData>
            {
                new LevelingData("P1") 
                { 
                    AR1 = 1.234, AV1 = 1.456, DIST1 = 50.0,
                    AR2 = 1.235, AV2 = 1.457, DIST2 = 52.0
                },
                new LevelingData("P2") 
                { 
                    AR1 = 1.456, AV1 = 1.678, DIST1 = 75.0,
                    AR2 = 1.457, AV2 = 1.679, DIST2 = 73.0
                },
                new LevelingData("P3") 
                { 
                    AR1 = 1.678, AV1 = 1.890, DIST1 = 100.0,
                    AR2 = 1.679, AV2 = 1.891, DIST2 = 98.0
                },
                new LevelingData("P4") 
                { 
                    AR1 = 1.890, AV1 = 2.012, DIST1 = 120.0,
                    AR2 = 1.891, AV2 = 2.013, DIST2 = 118.0
                },
                new LevelingData("P5") 
                { 
                    AR1 = 2.012, AV1 = 2.234, DIST1 = 80.0,
                    AR2 = 2.013, AV2 = 2.235, DIST2 = 82.0
                }
            };
        }
        
        private static void DisplayDetailedResults(DetailedCompensationResults compensation)
        {
            System.Console.WriteLine("\n");
            System.Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            System.Console.WriteLine("║               RÉSULTATS DÉTAILLÉS                        ║");
            System.Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            
            // Informations générales
            System.Console.WriteLine($"\n📊 INFORMATIONS GÉNÉRALES");
            System.Console.WriteLine($"   Grade de qualité: {compensation.QualityGrade}");
            System.Console.WriteLine($"   Points traités: {compensation.ProcessedPoints}");
            System.Console.WriteLine($"   Statut: {compensation.Status}");
            
            // Statistiques de compensation
            if (compensation.Statistics != null)
            {
                var stats = compensation.Statistics;
                System.Console.WriteLine($"\n📈 STATISTIQUES DE COMPENSATION");
                System.Console.WriteLine($"   σ₀ a posteriori: {stats.Sigma0Hat:F4}");
                System.Console.WriteLine($"   Précision (mm): {stats.Sigma0Hat * 1000:F2}");
                System.Console.WriteLine($"   Degrés de liberté: {stats.DegreesOfFreedom}");
                System.Console.WriteLine($"   Test χ²: {stats.Chi2TestStatistic:F2} / {stats.Chi2CriticalValue:F2}");
                System.Console.WriteLine($"   Poids unitaire: {(stats.UnitWeightValid ? "✅ VALIDE" : "❌ INVALIDE")}");
                System.Console.WriteLine($"   Résidu max normalisé: {stats.MaxStandardizedResidual:F2}");
            }
            
            // Validation détaillée
            if (compensation.ValidationSummary != null)
            {
                var validation = compensation.ValidationSummary;
                System.Console.WriteLine($"\n🧪 TESTS DE VALIDATION");
                System.Console.WriteLine($"   Test χ²: {(validation.Chi2Validation.IsValid ? "✅" : "❌")} {validation.Chi2Validation.Message}");
                System.Console.WriteLine($"   Précision: {(validation.PrecisionValidation.IsValid ? "✅" : "❌")} {validation.PrecisionValidation.Message}");
                System.Console.WriteLine($"   Fautes grossières: {(validation.BlunderValidation.IsValid ? "✅" : "❌")} {validation.BlunderValidation.Message}");
                System.Console.WriteLine($"   Validation géodésique: {(validation.GeodeticValidation.IsValid ? "✅" : "❌")} {validation.GeodeticValidation.Message}");
            }
            
            // Observations suspectes
            if (compensation.SuspectObservations.Count > 0)
            {
                System.Console.WriteLine($"\n🔍 OBSERVATIONS SUSPECTES ({compensation.SuspectObservations.Count})");
                foreach (var suspect in compensation.SuspectObservations)
                {
                    System.Console.WriteLine($"   - Observation {suspect.ObservationIndex}: résidu normalisé = {suspect.NormalizedResidual:F2}");
                    System.Console.WriteLine($"     Significativité: {suspect.Significance:F3}, Critique: {suspect.Critical}");
                }
            }
            
            // Altitudes ajustées
            if (compensation.AdjustedPoints.Count > 0)
            {
                System.Console.WriteLine($"\n📍 ALTITUDES AJUSTÉES");
                foreach (var point in compensation.AdjustedPoints)
                {
                    System.Console.WriteLine($"   {point.Matricule}: {point.Altitude:F6} m {(point.IsReference ? "(REF)" : "")}");
                }
            }
            
            System.Console.WriteLine("\n" + new string('═', 60));
            System.Console.WriteLine($"Compensation terminée - Grade: {compensation.QualityGrade}");
        }
    }
}