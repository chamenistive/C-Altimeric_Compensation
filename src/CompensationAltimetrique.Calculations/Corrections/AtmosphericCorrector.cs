// ================================================================
// IMPLÉMENTATION PRODUCTION - Corrections Atmosphériques Avancées
// Transposition FIDÈLE du module Python atmospheric_corrections.py
// Version: 1.0 Production Ready
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Calculations.Corrections
{
    /// <summary>
    /// Conditions atmosphériques pour calcul de réfraction.
    /// Transposition exacte de la dataclass AtmosphericConditions Python.
    /// 
    /// Formules appliquées:
    /// - Correction courbure: C₁ = k × d² / (2R)  
    /// - Correction réfraction: C₂ = -r × d² / (2R)
    /// - Correction niveau apparent: n.a = (1-m.r.a) × Dh²/(2×Rn)
    /// </summary>
    public class AtmosphericConditions
    {
        // Constantes géodésiques (équivalent Python EARTH_RADIUS_M = 6371000.0)
        public const double EARTH_RADIUS_M = 6371000.0;
        public const double STANDARD_REFRACTION_COEFF = 0.13;
        public const double CURVATURE_COEFF = 1.0;

        public double TemperatureCelsius { get; set; } = 15.0;
        public double PressureHpa { get; set; } = 1013.25;
        public double HumidityPercent { get; set; } = 60.0;
        public DateTime? TimeOfDay { get; set; } = null;
        public string WeatherCondition { get; set; } = "normal";

        public AtmosphericConditions() { }

        public AtmosphericConditions(double temperature, double pressure, double humidity)
        {
            TemperatureCelsius = temperature;
            PressureHpa = pressure;
            HumidityPercent = humidity;
        }

        /// <summary>
        /// Calcule le coefficient de réfraction selon conditions atmosphériques.
        /// TRANSPOSITION EXACTE de la méthode Python.
        /// </summary>
        public double CalculateRefractionCoefficient()
        {
            try
            {
                double k_base = STANDARD_REFRACTION_COEFF;
                
                // Formules Python exactes
                double temp_correction = -(TemperatureCelsius - 15.0) * 0.004;
                double pressure_correction = (PressureHpa - 1013.25) * 0.0001;
                double humidity_correction = (HumidityPercent - 60.0) * 0.0002;
                
                double time_correction = 0.0;
                if (TimeOfDay.HasValue)
                {
                    int hour = TimeOfDay.Value.Hour;
                    if (hour >= 10 && hour <= 16) time_correction = 0.02;
                    else if (hour <= 8 || hour >= 18) time_correction = -0.01;
                }
                
                double k_adjusted = k_base + temp_correction + pressure_correction + 
                                  humidity_correction + time_correction;
                
                return Math.Max(0.05, Math.Min(0.25, k_adjusted));
            }
            catch { return STANDARD_REFRACTION_COEFF; }
        }

        public override string ToString() =>
            $"T={TemperatureCelsius:F1}°C, P={PressureHpa:F1}hPa, H={HumidityPercent:F1}%, r={CalculateRefractionCoefficient():F3}";
    }

    /// <summary>
    /// Résultat d'une correction de réfraction - Équivalent Python RefractionCorrection
    /// </summary>
    public class RefractionCorrection
    {
        public double DistanceM { get; set; }
        public double RawDeltaH { get; set; }
        public double CurvatureCorrectionMm { get; set; }
        public double RefractionCorrectionMm { get; set; }
        public double TotalCorrectionMm { get; set; }
        public double CorrectedDeltaH { get; set; }
        public double RefractionCoefficient { get; set; }
        public double LevelApparentCorrectionMm { get; set; } = 0.0;

        public string GetSignificance()
        {
            double abs_correction = Math.Abs(TotalCorrectionMm);
            return abs_correction switch
            {
                < 0.1 => "négligeable",
                < 1.0 => "faible", 
                < 5.0 => "modérée",
                _ => "importante"
            };
        }
    }

    /// <summary>
    /// Calculateur de corrections atmosphériques - Transposition AtmosphericCorrector Python
    /// </summary>
    public class AtmosphericCorrector
    {
        private readonly double _earthRadius;
        private readonly double _standardRefraction;

        public AtmosphericCorrector(double earthRadiusM = AtmosphericConditions.EARTH_RADIUS_M,
                                  double standardRefraction = AtmosphericConditions.STANDARD_REFRACTION_COEFF)
        {
            _earthRadius = earthRadiusM;
            _standardRefraction = standardRefraction;
        }

        /// <summary>
        /// Calcul de la correction de niveau apparent - Nouvelle formule Python
        /// </summary>
        public double CalculateLevelApparentCorrection(double distanceM, double deltaHM, double refractionCoeff)
        {
            if (distanceM <= 0 || Math.Abs(deltaHM) < 1e-6) return 0.0;
            
            double m_r_a = refractionCoeff * 0.8;
            double correction = (1.0 - m_r_a) * deltaHM * deltaHM / (2.0 * _earthRadius);
            return correction * 1000.0;
        }

        /// <summary>
        /// Calcul complet des corrections atmosphériques - Méthode principale Python
        /// </summary>
        public RefractionCorrection CalculateAtmosphericCorrection(double distanceM, double rawDeltaH, 
                                                                 AtmosphericConditions conditions)
        {
            try
            {
                if (distanceM <= 0)
                {
                    return new RefractionCorrection
                    {
                        DistanceM = distanceM,
                        RawDeltaH = rawDeltaH,
                        CorrectedDeltaH = rawDeltaH,
                        RefractionCoefficient = _standardRefraction
                    };
                }

                double refractionCoeff = conditions.CalculateRefractionCoefficient();

                // Formules Python exactes
                double curvature_correction = AtmosphericConditions.CURVATURE_COEFF * distanceM * distanceM / (2.0 * _earthRadius);
                double curvature_mm = curvature_correction * 1000.0;

                double refraction_correction = -refractionCoeff * distanceM * distanceM / (2.0 * _earthRadius);
                double refraction_mm = refraction_correction * 1000.0;

                double level_apparent_mm = CalculateLevelApparentCorrection(distanceM, rawDeltaH, refractionCoeff);
                double total_mm = curvature_mm + refraction_mm + level_apparent_mm;
                double final_corrected_delta_h = rawDeltaH + (total_mm / 1000.0);

                return new RefractionCorrection
                {
                    DistanceM = distanceM,
                    RawDeltaH = rawDeltaH,
                    CurvatureCorrectionMm = curvature_mm,
                    RefractionCorrectionMm = refraction_mm,
                    TotalCorrectionMm = total_mm,
                    CorrectedDeltaH = final_corrected_delta_h,
                    RefractionCoefficient = refractionCoeff,
                    LevelApparentCorrectionMm = level_apparent_mm
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur calcul correction atmosphérique: {ex.Message}");
            }
        }

        /// <summary>
        /// Application des corrections à des données de nivellement - Adaptation Python
        /// </summary>
        public List<LevelingData> ApplyCorrections(List<LevelingData> levelingData, 
                                                  AtmosphericConditions? conditions = null)
        {
            conditions ??= new AtmosphericConditions();
            
            Console.WriteLine("🌡️ Application des corrections atmosphériques avancées...");
            Console.WriteLine($"   Conditions: {conditions}");

            var correctedData = new List<LevelingData>();
            double totalCorrection = 0.0;
            int correctedCount = 0;

            foreach (var data in levelingData)
            {
                var correctedItem = new LevelingData(data.Matricule)
                {
                    AR1 = data.AR1, AV1 = data.AV1, AR2 = data.AR2, AV2 = data.AV2,
                    DIST1 = data.DIST1, DIST2 = data.DIST2
                };

                // Session 1
                if (data.DIST1.HasValue && data.AR1.HasValue && data.AV1.HasValue)
                {
                    double deltaH1 = data.AR1.Value - data.AV1.Value;
                    var correction1 = CalculateAtmosphericCorrection(data.DIST1.Value, deltaH1, conditions);
                    correctedItem.AV1 = data.AV1.Value + (correction1.TotalCorrectionMm / 1000.0);
                    totalCorrection += Math.Abs(correction1.TotalCorrectionMm);
                    correctedCount++;
                }

                // Session 2
                if (data.DIST2.HasValue && data.AR2.HasValue && data.AV2.HasValue)
                {
                    double deltaH2 = data.AR2.Value - data.AV2.Value;
                    var correction2 = CalculateAtmosphericCorrection(data.DIST2.Value, deltaH2, conditions);
                    correctedItem.AV2 = data.AV2.Value + (correction2.TotalCorrectionMm / 1000.0);
                    totalCorrection += Math.Abs(correction2.TotalCorrectionMm);
                    correctedCount++;
                }

                correctedData.Add(correctedItem);
            }

            var avgCorrection = correctedCount > 0 ? totalCorrection / correctedCount : 0.0;
            Console.WriteLine($"   ✅ {correctedCount} observations corrigées");
            Console.WriteLine($"   📊 Correction moyenne: {avgCorrection:F2} mm");

            return correctedData;
        }
    }

    /// <summary>
    /// Factory pour conditions atmosphériques - Équivalent create_standard_conditions Python
    /// </summary>
    public static class AtmosphericConditionsFactory
    {
        public static AtmosphericConditions CreateStandardConditions(string region = "france") =>
            region.ToLower() switch
            {
                "france" => new(15.0, 1013.25, 65.0),
                "sahel" => new(32.0, 1008.0, 40.0),
                "tropical" => new(28.0, 1010.0, 80.0),
                "arid" => new(35.0, 1005.0, 25.0),
                _ => new()
            };

        public static Dictionary<int, double> GetPythonReferenceTable() => new()
        {
            { 50, 0.10 }, { 100, 0.38 }, { 150, 0.86 }, { 200, 1.53 }, { 300, 3.44 }
        };
    }
}
