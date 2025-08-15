// ================================================================
// MODULE: Corrections Atmosphériques Avancées
// Transposition fidèle du code Python atmospheric_corrections.py
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CompensationAltimetrique.Calculations.Corrections
{
    /// <summary>
    /// Conditions atmosphériques pour calcul de réfraction.
    /// Équivalent exact de la classe AtmosphericConditions Python.
    /// </summary>
    public class AtmosphericConditions
    {
        public double TemperatureCelsius { get; set; } = 15.0;
        public double PressureHpa { get; set; } = 1013.25;
        public double HumidityPercent { get; set; } = 60.0;
        public DateTime? TimeOfDay { get; set; } = null;
        public string WeatherCondition { get; set; } = "normal";
        public double EarthRadius { get; set; } = 6371000.0;

        public AtmosphericConditions() { }

        public AtmosphericConditions(double temperature, double pressure, double humidity)
        {
            TemperatureCelsius = temperature;
            PressureHpa = pressure;
            HumidityPercent = humidity;
        }

        /// <summary>
        /// Calcule le coefficient de réfraction selon les conditions atmosphériques.
        /// Réplication exacte de la méthode Python.
        /// </summary>
        public double CalculateRefractionCoefficient()
        {
            try
            {
                // Base standard (équivalent Python)
                double k_base = 0.13;

                // Correction température (effet principal)
                // Formule Python: temp_correction = -(conditions.temperature_celsius - 15.0) * 0.004
                double temp_correction = -(TemperatureCelsius - 15.0) * 0.004;

                // Correction pression
                // Formule Python: pressure_correction = (conditions.pressure_hpa - 1013.25) * 0.0001
                double pressure_correction = (PressureHpa - 1013.25) * 0.0001;

                // Correction humidité
                // Formule Python: humidity_correction = (conditions.humidity_percent - 60.0) * 0.0002
                double humidity_correction = (HumidityPercent - 60.0) * 0.0002;

                // Correction heure de mesure (gradient thermique)
                double time_correction = 0.0;
                if (TimeOfDay.HasValue)
                {
                    int hour = TimeOfDay.Value.Hour;
                    if (hour >= 10 && hour <= 16)  // Heures chaudes
                        time_correction = 0.02;
                    else if (hour <= 8 || hour >= 18)  // Heures fraîches
                        time_correction = -0.01;
                }

                // Coefficient final (équivalent Python)
                double k_adjusted = k_base + temp_correction + pressure_correction + 
                                  humidity_correction + time_correction;

                // Limites de sécurité (équivalent Python)
                k_adjusted = Math.Max(0.05, Math.Min(0.25, k_adjusted));

                return k_adjusted;
            }
            catch (Exception)
            {
                return 0.13; // Valeur par défaut en cas d'erreur
            }
        }

        public override string ToString()
        {
            return $"T={TemperatureCelsius:F1}°C, P={PressureHpa:F1}hPa, H={HumidityPercent:F1}%, r={CalculateRefractionCoefficient():F3}";
        }
    }

    /// <summary>
    /// Résultat d'une correction de réfraction.
    /// Équivalent exact de la classe RefractionCorrection Python.
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
    }

    /// <summary>
    /// Correction de niveau apparent selon la réfraction atmosphérique.
    /// Équivalent exact de la classe LevelApparentCorrection Python.
    /// </summary>
    public class LevelApparentCorrection
    {
        public double DistanceM { get; set; }
        public double DeltaHM { get; set; }
        public double ModuleRefractionAtm { get; set; }
        public double CorrectionMm { get; set; }
        public string FormulaUsed { get; set; } = "n.a = (1-m.r.a) × Dh²/(2×Rn)";
    }

    /// <summary>
    /// Données de nivellement simplifiées pour les corrections
    /// </summary>
    public class LevelingData
    {
        public string Matricule { get; set; }
        public double? AR1 { get; set; }  // Arrière session 1
        public double? AV1 { get; set; }  // Avant session 1
        public double? AR2 { get; set; }  // Arrière session 2
        public double? AV2 { get; set; }  // Avant session 2
        public double? DIST1 { get; set; } // Distance session 1
        public double? DIST2 { get; set; } // Distance session 2

        public LevelingData(string matricule)
        {
            Matricule = matricule;
        }
    }

    /// <summary>
    /// Calculateur de corrections atmosphériques pour nivellement géométrique.
    /// Transposition fidèle de la classe AtmosphericCorrector Python.
    /// 
    /// Implémente les corrections selon les normes internationales avec
    /// adaptation aux conditions locales.
    /// </summary>
    public class AdvancedAtmosphericCorrector
    {
        // Constantes géodésiques (équivalent Python)
        private const double EARTH_RADIUS_M = 6371000.0;
        private const double STANDARD_REFRACTION_COEFF = 0.13;
        private const double CURVATURE_COEFF = 1.0;

        private readonly double _earthRadius;
        private readonly double _standardRefraction;

        public AdvancedAtmosphericCorrector(double earthRadiusM = EARTH_RADIUS_M, 
                                          double standardRefraction = STANDARD_REFRACTION_COEFF)
        {
            _earthRadius = earthRadiusM;
            _standardRefraction = standardRefraction;
        }

        /// <summary>
        /// Calcul du coefficient de réfraction selon conditions atmosphériques.
        /// Méthode équivalente à calculate_refraction_coefficient Python.
        /// </summary>
        public double CalculateRefractionCoefficient(AtmosphericConditions conditions)
        {
            return conditions.CalculateRefractionCoefficient();
        }

        /// <summary>
        /// Calcul de la correction de niveau apparent selon la réfraction atmosphérique.
        /// Transposition de calculate_level_apparent_correction Python.
        /// 
        /// Formule: n.a = (1-m.r.a) × Dh²/(2×Rn)
        /// </summary>
        public double CalculateLevelApparentCorrection(double distanceM, double deltaHM, double refractionCoeff)
        {
            try
            {
                if (distanceM <= 0 || Math.Abs(deltaHM) < 1e-6)
                    return 0.0;

                // Module de réfraction atmosphérique (formule empirique)
                double m_r_a = refractionCoeff * 0.8; // Facteur d'atténuation

                // Formule du niveau apparent (équivalent Python)
                // correction = (1 - m_r_a) * deltaHM * deltaHM / (2 * _earthRadius)
                double correction = (1.0 - m_r_a) * deltaHM * deltaHM / (2.0 * _earthRadius);

                // Conversion en millimètres
                return correction * 1000.0;
            }
            catch (Exception)
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Calcul complet des corrections atmosphériques.
        /// Transposition fidèle de calculate_atmospheric_correction Python.
        /// 
        /// Implémente:
        /// - Correction de courbure: C₁ = k × d² / (2R)
        /// - Correction de réfraction: C₂ = -r × d² / (2R)  
        /// - Correction de niveau apparent: n.a = (1-m.r.a) × Dh²/(2×Rn)
        /// - Correction totale: C = C₁ + C₂ + n.a
        /// </summary>
        public RefractionCorrection CalculateAtmosphericCorrection(double distanceM, 
                                                                 double rawDeltaH, 
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
                        CorrectedDeltaH = rawDeltaH
                    };
                }

                // Coefficient de réfraction variable (équivalent Python)
                double refractionCoeff = CalculateRefractionCoefficient(conditions);

                // 1. Correction de courbure terrestre
                // Formule Python: curvature_correction = CURVATURE_COEFF * distance_m**2 / (2 * self.earth_radius)
                double curvature_correction = CURVATURE_COEFF * distanceM * distanceM / (2.0 * _earthRadius);
                double curvature_mm = curvature_correction * 1000.0;

                // 2. Correction de réfraction atmosphérique  
                // Formule Python: refraction_correction = -refraction_coeff * distance_m**2 / (2 * self.earth_radius)
                double refraction_correction = -refractionCoeff * distanceM * distanceM / (2.0 * _earthRadius);
                double refraction_mm = refraction_correction * 1000.0;

                // 3. Correction de niveau apparent (nouvelle formule Python)
                double level_apparent_mm = CalculateLevelApparentCorrection(distanceM, rawDeltaH, refractionCoeff);

                // 4. Correction totale (équivalent Python)
                double total_mm = curvature_mm + refraction_mm + level_apparent_mm;

                // 5. Application de la correction à la dénivelée
                // Python: final_corrected_delta_h = raw_delta_h + (total_correction / 1000.0)
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
        /// Application des corrections atmosphériques à une liste de données de nivellement.
        /// Équivalent de apply_corrections_to_dataframe Python (simplifié pour LevelingData).
        /// </summary>
        public List<LevelingData> ApplyCorrections(List<LevelingData> levelingData, 
                                                 AtmosphericConditions conditions = null)
        {
            if (conditions == null)
                conditions = new AtmosphericConditions();

            Console.WriteLine("🌡️ Application des corrections atmosphériques avancées...");
            Console.WriteLine($"   Conditions: {conditions}");

            var correctedData = new List<LevelingData>();
            double totalCorrection = 0.0;
            int correctedCount = 0;

            foreach (var data in levelingData)
            {
                var correctedItem = new LevelingData(data.Matricule)
                {
                    AR1 = data.AR1,
                    AV1 = data.AV1,
                    AR2 = data.AR2,
                    AV2 = data.AV2,
                    DIST1 = data.DIST1,
                    DIST2 = data.DIST2
                };

                // Application des corrections session 1
                if (data.DIST1.HasValue && data.AR1.HasValue && data.AV1.HasValue)
                {
                    double deltaH1 = data.AR1.Value - data.AV1.Value;
                    var correction1 = CalculateAtmosphericCorrection(data.DIST1.Value, deltaH1, conditions);
                    
                    // Application de la correction à la lecture AV (équivalent Python)
                    correctedItem.AV1 = data.AV1.Value + (correction1.TotalCorrectionMm / 1000.0);
                    totalCorrection += Math.Abs(correction1.TotalCorrectionMm);
                    correctedCount++;
                }

                // Application des corrections session 2
                if (data.DIST2.HasValue && data.AR2.HasValue && data.AV2.HasValue)
                {
                    double deltaH2 = data.AR2.Value - data.AV2.Value;
                    var correction2 = CalculateAtmosphericCorrection(data.DIST2.Value, deltaH2, conditions);
                    
                    // Application de la correction à la lecture AV (équivalent Python)
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

        /// <summary>
        /// Analyse de l'impact des corrections atmosphériques.
        /// Équivalent de la méthode Python pour diagnostic.
        /// </summary>
        public CorrectionAnalysis AnalyzeCorrections(List<LevelingData> levelingData, 
                                                   AtmosphericConditions conditions = null)
        {
            if (conditions == null)
                conditions = new AtmosphericConditions();

            var analysis = new CorrectionAnalysis();

            foreach (var data in levelingData)
            {
                if (data.DIST1.HasValue && data.AR1.HasValue && data.AV1.HasValue)
                {
                    double deltaH1 = data.AR1.Value - data.AV1.Value;
                    var correction1 = CalculateAtmosphericCorrection(data.DIST1.Value, deltaH1, conditions);
                    analysis.AddCorrection(data.DIST1.Value, correction1.TotalCorrectionMm);
                }

                if (data.DIST2.HasValue && data.AR2.HasValue && data.AV2.HasValue)
                {
                    double deltaH2 = data.AR2.Value - data.AV2.Value;
                    var correction2 = CalculateAtmosphericCorrection(data.DIST2.Value, deltaH2, conditions);
                    analysis.AddCorrection(data.DIST2.Value, correction2.TotalCorrectionMm);
                }
            }

            return analysis;
        }
    }

    /// <summary>
    /// Analyse des corrections atmosphériques appliquées.
    /// Classe équivalente Python pour diagnostics.
    /// </summary>
    public class CorrectionAnalysis
    {
        public List<double> Distances { get; private set; }
        public List<double> Corrections { get; private set; }

        public CorrectionAnalysis()
        {
            Distances = new List<double>();
            Corrections = new List<double>();
        }

        public void AddCorrection(double distance, double correction)
        {
            Distances.Add(distance);
            Corrections.Add(correction);
        }

        public double MaxDistance => Distances.Count > 0 ? Distances.Max() : 0.0;
        public double MaxCorrection => Corrections.Count > 0 ? Corrections.Max() : 0.0;
        public double MinCorrection => Corrections.Count > 0 ? Corrections.Min() : 0.0;
        public double AverageCorrection => Corrections.Count > 0 ? Corrections.Average() : 0.0;

        public string GetSummary()
        {
            if (Corrections.Count == 0)
                return "Aucune correction appliquée";

            return $"Corrections: {Corrections.Count}, " +
                   $"Max: {MaxCorrection:F2}mm, " +
                   $"Min: {MinCorrection:F2}mm, " +
                   $"Moyenne: {AverageCorrection:F2}mm";
        }
    }

    /// <summary>
    /// Factory pour créer des conditions atmosphériques standards.
    /// Équivalent de create_standard_conditions Python.
    /// </summary>
    public static class AtmosphericConditionsFactory
    {
        public static AtmosphericConditions CreateStandardConditions(string region = "france")
        {
            return region.ToLower() switch
            {
                "france" => new AtmosphericConditions(15.0, 1013.25, 65.0),
                "sahel" => new AtmosphericConditions(32.0, 1008.0, 40.0),
                "tropical" => new AtmosphericConditions(28.0, 1010.0, 80.0),
                "arid" => new AtmosphericConditions(35.0, 1005.0, 25.0),
                _ => new AtmosphericConditions() // Conditions par défaut
            };
        }

        public static AtmosphericConditions CreateTimeBasedConditions(DateTime measurementTime, string region = "france")
        {
            var conditions = CreateStandardConditions(region);
            conditions.TimeOfDay = measurementTime;
            return conditions;
        }
    }
}