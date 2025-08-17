using System;
using System.Collections.Generic;
using System.Linq;

namespace CompensationAltimetrique.Calculations.Corrections
{
    /// <summary>
    /// Conditions atmosphériques pour le calcul des corrections
    /// </summary>
    public class AtmosphericConditions
    {
        public double TemperatureCelsius { get; set; } = 15.0;
        public double PressureHPa { get; set; } = 1013.25;
        public double HumidityPercent { get; set; } = 60.0;
        public string Region { get; set; } = "standard";
        
        /// <summary>
        /// Créer des conditions standards pour une région
        /// </summary>
        public static AtmosphericConditions CreateForRegion(string region)
        {
            switch (region.ToLower())
            {
                case "sahel":
                case "afrique":
                    return new AtmosphericConditions
                    {
                        TemperatureCelsius = 32.0,
                        PressureHPa = 1008.0,
                        HumidityPercent = 40.0,
                        Region = region
                    };
                    
                case "tropical":
                    return new AtmosphericConditions
                    {
                        TemperatureCelsius = 28.0,
                        PressureHPa = 1010.0,
                        HumidityPercent = 75.0,
                        Region = region
                    };
                    
                case "europe":
                case "france":
                    return new AtmosphericConditions
                    {
                        TemperatureCelsius = 15.0,
                        PressureHPa = 1013.25,
                        HumidityPercent = 65.0,
                        Region = region
                    };
                    
                default:
                    return new AtmosphericConditions { Region = "standard" };
            }
        }
    }

    /// <summary>
    /// Correcteur atmosphérique pour nivellement géométrique
    /// Applique les corrections de courbure terrestre et réfraction atmosphérique
    /// </summary>
    public class AtmosphericCorrector
    {
        private readonly AtmosphericConditions _conditions;
        private readonly bool _applyCorrections;
        private const double EARTH_RADIUS_M = 6371000.0;
        private const double STANDARD_REFRACTION_COEFF = 0.13;
        
        public AtmosphericCorrector(AtmosphericConditions? conditions = null, bool applyCorrections = true)
        {
            _conditions = conditions ?? AtmosphericConditions.CreateForRegion("standard");
            _applyCorrections = applyCorrections;
        }
        
        /// <summary>
        /// Calcul du coefficient de réfraction variable selon conditions
        /// </summary>
        private double CalculateRefractionCoefficient()
        {
            double r = STANDARD_REFRACTION_COEFF;
            
            // Effet de la température
            double deltaTemp = (_conditions.TemperatureCelsius - 15.0) * 0.004;
            r -= deltaTemp;
            
            // Effet de la pression
            double deltaPressure = (_conditions.PressureHPa - 1013.25) * 0.0001;
            r += deltaPressure;
            
            // Effet de l'humidité
            double deltaHumidity = (_conditions.HumidityPercent - 60.0) * 0.0002;
            r += deltaHumidity;
            
            // Limiter entre 0.05 et 0.20
            return Math.Max(0.05, Math.Min(0.20, r));
        }
        
        /// <summary>
        /// Correction de courbure terrestre (toujours positive)
        /// C₁ = d² / (2R)
        /// </summary>
        public double CalculateEarthCurvatureCorrection(double distanceM)
        {
            if (!_applyCorrections || distanceM <= 0)
                return 0.0;
                
            return (distanceM * distanceM) / (2.0 * EARTH_RADIUS_M);
        }
        
        /// <summary>
        /// Correction de réfraction atmosphérique (toujours négative)
        /// C₂ = -r × d² / (2R)
        /// </summary>
        public double CalculateRefractionCorrection(double distanceM)
        {
            if (!_applyCorrections || distanceM <= 0)
                return 0.0;
                
            double r = CalculateRefractionCoefficient();
            return -r * (distanceM * distanceM) / (2.0 * EARTH_RADIUS_M);
        }
        
        /// <summary>
        /// Correction totale atmosphérique
        /// C_total = C₁ + C₂ = (1 - r) × d² / (2R)
        /// </summary>
        public double CalculateTotalCorrection(double distanceM)
        {
            if (!_applyCorrections || distanceM <= 0)
                return 0.0;
                
            double curvature = CalculateEarthCurvatureCorrection(distanceM);
            double refraction = CalculateRefractionCorrection(distanceM);
            
            return curvature + refraction;
        }
        
        /// <summary>
        /// Appliquer les corrections à une dénivelée
        /// </summary>
        public double ApplyCorrectionToDenivelation(double denivelationM, double distanceM)
        {
            if (!_applyCorrections)
                return denivelationM;
                
            double correction = CalculateTotalCorrection(distanceM);
            return denivelationM + correction;
        }
        
        /// <summary>
        /// Appliquer les corrections à un ensemble de dénivelées
        /// </summary>
        public List<double> ApplyCorrectionsToMultiple(List<double> denivelations, List<double> distances)
        {
            if (denivelations.Count != distances.Count)
                throw new ArgumentException("Les listes doivent avoir la même taille");
                
            var corrected = new List<double>();
            
            for (int i = 0; i < denivelations.Count; i++)
            {
                corrected.Add(ApplyCorrectionToDenivelation(denivelations[i], distances[i]));
            }
            
            return corrected;
        }
        
        /// <summary>
        /// Rapport détaillé des corrections
        /// </summary>
        public CorrectionReport GenerateReport(double distanceM)
        {
            return new CorrectionReport
            {
                DistanceM = distanceM,
                RefractionCoefficient = CalculateRefractionCoefficient(),
                CurvatureCorrectionM = CalculateEarthCurvatureCorrection(distanceM),
                RefractionCorrectionM = CalculateRefractionCorrection(distanceM),
                TotalCorrectionM = CalculateTotalCorrection(distanceM),
                TotalCorrectionMm = CalculateTotalCorrection(distanceM) * 1000,
                Conditions = _conditions,
                CorrectionApplied = _applyCorrections
            };
        }
    }

    /// <summary>
    /// Rapport de correction atmosphérique
    /// </summary>
    public class CorrectionReport
    {
        public double DistanceM { get; set; }
        public double RefractionCoefficient { get; set; }
        public double CurvatureCorrectionM { get; set; }
        public double RefractionCorrectionM { get; set; }
        public double TotalCorrectionM { get; set; }
        public double TotalCorrectionMm { get; set; }
        public AtmosphericConditions Conditions { get; set; }
        public bool CorrectionApplied { get; set; }
        
        public override string ToString()
        {
            return $"Distance: {DistanceM:F1}m | " +
                   $"Courbure: {CurvatureCorrectionMm:F2}mm | " +
                   $"Réfraction: {RefractionCorrectionMm:F2}mm | " +
                   $"Total: {TotalCorrectionMm:F2}mm | " +
                   $"Coefficient r: {RefractionCoefficient:F3}";
        }
        
        public double CurvatureCorrectionMm => CurvatureCorrectionM * 1000;
        public double RefractionCorrectionMm => RefractionCorrectionM * 1000;
    }
}
