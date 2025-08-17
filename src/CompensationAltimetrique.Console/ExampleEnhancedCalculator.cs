using System;
using System.Collections.Generic;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Calculations;

namespace CompensationAltimetrique.Console
{
    /// <summary>
    /// Exemple d'utilisation d'EnhancedLevelingCalculator
    /// </summary>
    public class ExampleEnhancedCalculator
    {
        /// <summary>
        /// Démonstration de l'utilisation d'EnhancedLevelingCalculator
        /// </summary>
        public static void RunExample()
        {
            System.Console.WriteLine("=== EXEMPLE: EnhancedLevelingCalculator ===");
            
            // REMPLACER cette ligne:
            // var calculator = new LevelingCalculator();
            
            // PAR:
            var calculator = new EnhancedLevelingCalculator(
                precisionMm: 2.0,
                applyAtmosphericCorrections: true);
            
            // Configuration des conditions atmosphériques pour une région spécifique
            calculator.DefaultAtmosphericConditions = new AtmosphericConditions(
                temperature: 25.0,    // 25°C
                pressure: 1010.0,     // 1010 hPa
                humidity: 70.0        // 70%
            );
            
            // Données de test
            var testData = CreateTestData();
            
            // Calcul avec corrections atmosphériques
            var results = calculator.Calculate(testData, initialAltitude: 125.456);
            
            // Affichage des résultats
            DisplayResults(results);
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
                }
            };
        }
        
        private static void DisplayResults(CompensationResults results)
        {
            System.Console.WriteLine("\n--- RÉSULTATS ---");
            System.Console.WriteLine($"Points traités: {results.ProcessedPoints}");
            System.Console.WriteLine($"Statut: {results.Status}");
            System.Console.WriteLine($"Précision atteinte: {results.PrecisionAchieved}");
            
            System.Console.WriteLine("\n--- MÉTADONNÉES ---");
            foreach (var metadata in results.ValidationDetails)
            {
                System.Console.WriteLine($"  {metadata.Key}: {metadata.Value}");
            }
            
            System.Console.WriteLine("\n--- ALTITUDES AJUSTÉES ---");
            foreach (var point in results.AdjustedPoints)
            {
                System.Console.WriteLine($"  {point.Matricule}: {point.Altitude:F6} m");
            }
        }
    }
}