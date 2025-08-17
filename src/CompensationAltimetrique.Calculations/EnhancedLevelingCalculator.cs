using System;
using System.Collections.Generic;
using System.Linq;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Calculations
{
    /// <summary>
    /// Calculateur de nivellement avec corrections atmosphériques intégrées
    /// Extension de LevelingCalculator existant
    /// </summary>
    public class EnhancedLevelingCalculator : LevelingCalculator
    {
        private readonly SimpleAtmosphericCorrector _atmosphericCorrector;
        private readonly bool _applyAtmosphericCorrections;
        
        public AtmosphericConditions DefaultAtmosphericConditions { get; set; }
        
        public EnhancedLevelingCalculator(
            double precisionMm = 2.0,
            bool applyAtmosphericCorrections = true,
            AtmosphericConditions? defaultConditions = null) 
            : base(precisionMm)
        {
            _applyAtmosphericCorrections = applyAtmosphericCorrections;
            _atmosphericCorrector = new SimpleAtmosphericCorrector();
            DefaultAtmosphericConditions = defaultConditions ?? 
                CreateStandardConditions("france");
        }
        
        /// <summary>
        /// Calcul complet avec corrections atmosphériques
        /// Surcharge la méthode Calculate de base
        /// </summary>
        public override CompensationResults Calculate(List<LevelingData> data, double initialAltitude)
        {
            Console.WriteLine("🌡️ Calcul de nivellement avec corrections atmosphériques...");
            
            // Appliquer corrections atmosphériques avant calculs
            if (_applyAtmosphericCorrections)
            {
                ApplyAtmosphericCorrections(data, DefaultAtmosphericConditions);
            }
            
            // Appeler méthode de base avec données corrigées
            var baseResults = base.Calculate(data, initialAltitude);
            
            // Enrichir résultats avec métadonnées atmosphériques
            baseResults.ValidationDetails["atmospheric_corrections_applied"] = _applyAtmosphericCorrections;
            baseResults.ValidationDetails["atmospheric_conditions"] = DefaultAtmosphericConditions.ToString();
            
            if (_applyAtmosphericCorrections)
            {
                var correctionSummary = GenerateCorrectionSummary(data);
                baseResults.ValidationDetails["correction_summary"] = correctionSummary;
            }
            
            return baseResults;
        }
        
        private void ApplyAtmosphericCorrections(List<LevelingData> data, AtmosphericConditions conditions)
        {
            Console.WriteLine("🌡️ Application corrections atmosphériques...");
            Console.WriteLine($"   Conditions: T={conditions.TemperatureCelsius:F1}°C, P={conditions.PressureHpa:F1}hPa, H={conditions.HumidityPercent:F0}%");
            
            int correctedCount = 0;
            double totalCorrectionMm = 0;
            
            foreach (var levelingData in data)
            {
                // Session 1
                if (levelingData.DIST1.HasValue && levelingData.AR1.HasValue && levelingData.AV1.HasValue)
                {
                    double deltaH1 = levelingData.AR1.Value - levelingData.AV1.Value;
                    var correction1 = CalculateAtmosphericCorrection(
                        levelingData.DIST1.Value, deltaH1, conditions);
                    
                    // Appliquer correction à AV1 (méthode standard géodésique)
                    levelingData.AV1 = levelingData.AV1.Value + (correction1.TotalCorrectionMm / 1000.0);
                    
                    totalCorrectionMm += Math.Abs(correction1.TotalCorrectionMm);
                    correctedCount++;
                }
                
                // Session 2
                if (levelingData.DIST2.HasValue && levelingData.AR2.HasValue && levelingData.AV2.HasValue)
                {
                    double deltaH2 = levelingData.AR2.Value - levelingData.AV2.Value;
                    var correction2 = CalculateAtmosphericCorrection(
                        levelingData.DIST2.Value, deltaH2, conditions);
                    
                    // Appliquer correction à AV2
                    levelingData.AV2 = levelingData.AV2.Value + (correction2.TotalCorrectionMm / 1000.0);
                    
                    totalCorrectionMm += Math.Abs(correction2.TotalCorrectionMm);
                    correctedCount++;
                }
            }
            
            double avgCorrectionMm = correctedCount > 0 ? totalCorrectionMm / correctedCount : 0;
            Console.WriteLine($"   ✅ {correctedCount} observations corrigées");
            Console.WriteLine($"   📊 Correction moyenne: {avgCorrectionMm:F2}mm");
        }
        
        private AtmosphericCorrectionResult CalculateAtmosphericCorrection(double distanceM, double deltaH, AtmosphericConditions conditions)
        {
            // Utiliser le correcteur atmosphérique existant
            var correctionM = _atmosphericCorrector.CalculateTotalCorrection(distanceM);
            var correctionMm = correctionM * 1000.0;
            
            return new AtmosphericCorrectionResult
            {
                DistanceM = distanceM,
                DeltaH = deltaH,
                TotalCorrectionMm = correctionMm,
                Conditions = conditions
            };
        }
        
        private string GenerateCorrectionSummary(List<LevelingData> data)
        {
            int totalObs = 0;
            int correctedObs = 0;
            
            foreach (var d in data)
            {
                if (d.DIST1.HasValue && d.AR1.HasValue && d.AV1.HasValue)
                {
                    totalObs++;
                    correctedObs++;
                }
                if (d.DIST2.HasValue && d.AR2.HasValue && d.AV2.HasValue)
                {
                    totalObs++;
                    correctedObs++;
                }
            }
            
            return $"Observations corrigées: {correctedObs}/{totalObs}";
        }
        
        private static AtmosphericConditions CreateStandardConditions(string region)
        {
            switch (region.ToLower())
            {
                case "france":
                    return new AtmosphericConditions(15.0, 1013.25, 65.0);
                case "sahel":
                    return new AtmosphericConditions(32.0, 1008.0, 40.0);
                case "tropical":
                    return new AtmosphericConditions(28.0, 1010.0, 75.0);
                default:
                    return new AtmosphericConditions();
            }
        }
    }
    
    /// <summary>
    /// Résultat d'une correction atmosphérique
    /// </summary>
    public class AtmosphericCorrectionResult
    {
        public double DistanceM { get; set; }
        public double DeltaH { get; set; }
        public double TotalCorrectionMm { get; set; }
        public AtmosphericConditions? Conditions { get; set; }
    }
    
    /// <summary>
    /// Correcteur atmosphérique simplifié pour intégration
    /// </summary>
    public class SimpleAtmosphericCorrector
    {
        private const double EARTH_RADIUS_M = 6371000.0;
        private const double STANDARD_REFRACTION_COEFF = 0.13;
        
        public double CalculateTotalCorrection(double distanceM)
        {
            if (distanceM <= 0)
                return 0.0;
            
            // Correction de courbure (positive)
            double curvature = (distanceM * distanceM) / (2.0 * EARTH_RADIUS_M);
            
            // Correction de réfraction (négative)
            double refraction = -STANDARD_REFRACTION_COEFF * (distanceM * distanceM) / (2.0 * EARTH_RADIUS_M);
            
            return curvature + refraction;
        }
    }
}