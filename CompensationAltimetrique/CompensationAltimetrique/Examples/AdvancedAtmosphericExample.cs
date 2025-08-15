// ================================================================
// EXEMPLE: Utilisation des Corrections Atmosphériques Avancées
// Démonstration de la transposition Python vers C#
// ================================================================

using System;
using System.Collections.Generic;
using CompensationAltimetrique.Calculations.Corrections;

namespace CompensationAltimetrique.Examples
{
    /// <summary>
    /// Exemple complet d'utilisation des corrections atmosphériques avancées
    /// Équivalent du script Python atmospheric_corrections_example.py
    /// </summary>
    public class AdvancedAtmosphericExample
    {
        public static void RunExample()
        {
            Console.WriteLine("🌡️ EXEMPLE: CORRECTIONS ATMOSPHÉRIQUES AVANCÉES");
            Console.WriteLine("==================================================");
            Console.WriteLine();

            // 1. Création du correcteur atmosphérique
            var corrector = new AdvancedAtmosphericCorrector();

            // 2. Définition des conditions atmosphériques
            DemonstrateAtmosphericConditions();

            // 3. Calculs de corrections individuelles
            DemonstrateIndividualCorrections(corrector);

            // 4. Application aux données de nivellement
            DemonstrateLevelingDataCorrections(corrector);

            // 5. Analyse comparative par régions
            DemonstrateRegionalComparison(corrector);

            // 6. Analyse de sensibilité
            DemonstrateSensitivityAnalysis(corrector);

            Console.WriteLine();
            Console.WriteLine("✅ Exemple terminé avec succès");
        }

        private static void DemonstrateAtmosphericConditions()
        {
            Console.WriteLine("📊 1. DÉFINITION DES CONDITIONS ATMOSPHÉRIQUES");
            Console.WriteLine("-----------------------------------------------");

            // Conditions standard par région (équivalent Python)
            var regions = new[] { "france", "sahel", "tropical", "arid" };
            
            foreach (var region in regions)
            {
                var conditions = AtmosphericConditionsFactory.CreateStandardConditions(region);
                var coefficient = conditions.CalculateRefractionCoefficient();
                
                Console.WriteLine($"   {region,10}: {conditions} → k={coefficient:F3}");
            }

            // Conditions avec heure de mesure
            var timeBasedConditions = AtmosphericConditionsFactory.CreateTimeBasedConditions(
                new DateTime(2024, 8, 15, 14, 30, 0), "france");
            
            Console.WriteLine($"   Avec heure : {timeBasedConditions}");
            Console.WriteLine();
        }

        private static void DemonstrateIndividualCorrections(AdvancedAtmosphericCorrector corrector)
        {
            Console.WriteLine("🔧 2. CALCULS DE CORRECTIONS INDIVIDUELLES");
            Console.WriteLine("------------------------------------------");

            var conditions = AtmosphericConditionsFactory.CreateStandardConditions("france");
            var distances = new[] { 50.0, 100.0, 150.0, 200.0, 300.0 };

            Console.WriteLine("Distance | Courbure | Réfraction | Niveau App. | Total   | DeltaH Corrigée");
            Console.WriteLine("---------|----------|------------|-------------|---------|----------------");

            foreach (var distance in distances)
            {
                double deltaH = 1.5; // Dénivelée test
                var correction = corrector.CalculateAtmosphericCorrection(distance, deltaH, conditions);

                Console.WriteLine($"{distance,7:F0}m | " +
                                $"{correction.CurvatureCorrectionMm,7:F2}mm | " +
                                $"{correction.RefractionCorrectionMm,9:F2}mm | " +
                                $"{correction.LevelApparentCorrectionMm,10:F2}mm | " +
                                $"{correction.TotalCorrectionMm,6:F2}mm | " +
                                $"{correction.CorrectedDeltaH,14:F6}m");
            }
            Console.WriteLine();
        }

        private static void DemonstrateLevelingDataCorrections(AdvancedAtmosphericCorrector corrector)
        {
            Console.WriteLine("📏 3. APPLICATION AUX DONNÉES DE NIVELLEMENT");
            Console.WriteLine("---------------------------------------------");

            // Simulation de données de nivellement réalistes
            var levelingData = new List<LevelingData>
            {
                new LevelingData("REP001") 
                { 
                    AR1 = 1.4567, AV1 = 1.1234, DIST1 = 125.5,
                    AR2 = 1.4565, AV2 = 1.1236, DIST2 = 125.0
                },
                new LevelingData("REP002") 
                { 
                    AR1 = 2.1890, AV1 = 1.8765, DIST1 = 200.0,
                    AR2 = 2.1888, AV2 = 1.8767, DIST2 = 199.5
                },
                new LevelingData("REP003") 
                { 
                    AR1 = 1.7654, AV1 = 1.4321, DIST1 = 175.0,
                    AR2 = 1.7652, AV2 = 1.4323, DIST2 = 174.5
                }
            };

            var conditions = AtmosphericConditionsFactory.CreateTimeBasedConditions(
                new DateTime(2024, 8, 15, 13, 30, 0), "france");

            Console.WriteLine($"Conditions: {conditions}");
            Console.WriteLine();

            // Données avant correction
            Console.WriteLine("AVANT CORRECTION:");
            DisplayLevelingData(levelingData);

            // Application des corrections
            var correctedData = corrector.ApplyCorrections(levelingData, conditions);

            Console.WriteLine("APRÈS CORRECTION:");
            DisplayLevelingData(correctedData);

            // Analyse des corrections
            var analysis = corrector.AnalyzeCorrections(levelingData, conditions);
            Console.WriteLine($"Analyse: {analysis.GetSummary()}");
            Console.WriteLine();
        }

        private static void DemonstrateRegionalComparison(AdvancedAtmosphericCorrector corrector)
        {
            Console.WriteLine("🌍 4. COMPARAISON RÉGIONALE");
            Console.WriteLine("---------------------------");

            var distance = 200.0;
            var deltaH = 2.0;
            var regions = new[] { "france", "sahel", "tropical", "arid" };

            Console.WriteLine("Région    | Coeff. k | Correction Total | Diff. vs France");
            Console.WriteLine("----------|----------|------------------|----------------");

            double? franceCorrection = null;

            foreach (var region in regions)
            {
                var conditions = AtmosphericConditionsFactory.CreateStandardConditions(region);
                var correction = corrector.CalculateAtmosphericCorrection(distance, deltaH, conditions);
                
                if (region == "france")
                    franceCorrection = correction.TotalCorrectionMm;

                var diff = franceCorrection.HasValue ? 
                    correction.TotalCorrectionMm - franceCorrection.Value : 0.0;

                Console.WriteLine($"{region,9} | " +
                                $"{correction.RefractionCoefficient,7:F3} | " +
                                $"{correction.TotalCorrectionMm,15:F2}mm | " +
                                $"{diff,13:F2}mm");
            }
            Console.WriteLine();
        }

        private static void DemonstrateSensitivityAnalysis(AdvancedAtmosphericCorrector corrector)
        {
            Console.WriteLine("📈 5. ANALYSE DE SENSIBILITÉ");
            Console.WriteLine("----------------------------");

            var distance = 200.0;
            var deltaH = 1.5;

            // Test sensibilité température
            Console.WriteLine("Effet de la TEMPÉRATURE (distance 200m):");
            var temperatures = new[] { 5.0, 15.0, 25.0, 35.0 };
            
            foreach (var temp in temperatures)
            {
                var conditions = new AtmosphericConditions(temp, 1013.25, 60.0);
                var correction = corrector.CalculateAtmosphericCorrection(distance, deltaH, conditions);
                
                Console.WriteLine($"   {temp,4:F0}°C → k={correction.RefractionCoefficient:F3}, " +
                                $"correction={correction.TotalCorrectionMm:F2}mm");
            }

            Console.WriteLine();
            Console.WriteLine("Effet de la PRESSION (distance 200m):");
            var pressures = new[] { 980.0, 1013.25, 1040.0 };
            
            foreach (var pressure in pressures)
            {
                var conditions = new AtmosphericConditions(15.0, pressure, 60.0);
                var correction = corrector.CalculateAtmosphericCorrection(distance, deltaH, conditions);
                
                Console.WriteLine($"   {pressure,7:F1}hPa → k={correction.RefractionCoefficient:F3}, " +
                                $"correction={correction.TotalCorrectionMm:F2}mm");
            }

            Console.WriteLine();
            Console.WriteLine("Effet de l'HEURE DE MESURE (distance 200m):");
            var hours = new[] { 7, 12, 14, 19 };
            
            foreach (var hour in hours)
            {
                var conditions = AtmosphericConditionsFactory.CreateTimeBasedConditions(
                    new DateTime(2024, 8, 15, hour, 0, 0), "france");
                var correction = corrector.CalculateAtmosphericCorrection(distance, deltaH, conditions);
                
                Console.WriteLine($"   {hour,2:D2}h00 → k={correction.RefractionCoefficient:F3}, " +
                                $"correction={correction.TotalCorrectionMm:F2}mm");
            }
            Console.WriteLine();
        }

        private static void DisplayLevelingData(List<LevelingData> data)
        {
            Console.WriteLine("Repère   | AR1      | AV1      | DIST1   | AR2      | AV2      | DIST2   | ΔH1      | ΔH2");
            Console.WriteLine("---------|----------|----------|---------|----------|----------|---------|----------|--------");
            
            foreach (var item in data)
            {
                var deltaH1 = item.AR1.HasValue && item.AV1.HasValue ? 
                    item.AR1.Value - item.AV1.Value : (double?)null;
                var deltaH2 = item.AR2.HasValue && item.AV2.HasValue ? 
                    item.AR2.Value - item.AV2.Value : (double?)null;

                Console.WriteLine($"{item.Matricule,8} | " +
                                $"{item.AR1,8:F4} | " +
                                $"{item.AV1,8:F4} | " +
                                $"{item.DIST1,7:F1} | " +
                                $"{item.AR2,8:F4} | " +
                                $"{item.AV2,8:F4} | " +
                                $"{item.DIST2,7:F1} | " +
                                $"{deltaH1,8:F4} | " +
                                $"{deltaH2,8:F4}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Exemple spécialisé pour conditions extrêmes
        /// </summary>
        public static void DemonstrateExtremeConditions()
        {
            Console.WriteLine("⚠️  EXEMPLE: CONDITIONS EXTRÊMES");
            Console.WriteLine("=================================");

            var corrector = new AdvancedAtmosphericCorrector();
            var distance = 250.0;
            var deltaH = 3.0;

            // Conditions extrêmes
            var extremeConditions = new[]
            {
                new AtmosphericConditions(-20.0, 950.0, 90.0) { WeatherCondition = "unstable" },
                new AtmosphericConditions(45.0, 1050.0, 10.0) { WeatherCondition = "stable" },
                new AtmosphericConditions(15.0, 700.0, 50.0) { WeatherCondition = "inversion" }
            };

            Console.WriteLine("Condition        | k     | Correction | Sécurité");
            Console.WriteLine("-----------------|-------|------------|----------");

            foreach (var conditions in extremeConditions)
            {
                var correction = corrector.CalculateAtmosphericCorrection(distance, deltaH, conditions);
                var safety = (correction.RefractionCoefficient >= 0.05 && 
                             correction.RefractionCoefficient <= 0.25) ? "✅" : "⚠️";

                Console.WriteLine($"{conditions.WeatherCondition,16} | " +
                                $"{correction.RefractionCoefficient,5:F3} | " +
                                $"{correction.TotalCorrectionMm,9:F2}mm | " +
                                $"{safety,8}");
            }
            Console.WriteLine();
        }
    }
}