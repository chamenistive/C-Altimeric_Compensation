// ============================================================================
// ÉTAPE 3: CORRECTIONS ATMOSPHÉRIQUES
// Implémentation complète des corrections de courbure et réfraction
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CompensationAltimetrique.Atmospheric
{
    /// <summary>
    /// Conditions atmosphériques pour le calcul des corrections
    /// </summary>
    public class AtmosphericConditions
    {
        public double TemperatureCelsius { get; set; } = 15.0;     // Température (°C)
        public double PressureHPa { get; set; } = 1013.25;        // Pression (hPa)
        public double HumidityPercent { get; set; } = 65.0;       // Humidité relative (%)
        public string WeatherCondition { get; set; } = "temperate"; // Condition météo
        public double WindSpeedMs { get; set; } = 0.0;            // Vitesse du vent (m/s)
        public string Region { get; set; } = "temperate";         // Région géographique
        
        /// <summary>
        /// Constructeur avec valeurs par défaut (conditions standard)
        /// </summary>
        public AtmosphericConditions() { }
        
        /// <summary>
        /// Constructeur avec paramètres personnalisés
        /// </summary>
        public AtmosphericConditions(double temperature, double pressure, double humidity, string region = "temperate")
        {
            TemperatureCelsius = temperature;
            PressureHPa = pressure;
            HumidityPercent = humidity;
            Region = region;
        }

        /// <summary>
        /// Crée des conditions standard selon la région
        /// </summary>
        public static AtmosphericConditions CreateStandardConditions(string region)
        {
            return region.ToLower() switch
            {
                "france" or "europe" => new AtmosphericConditions(15.0, 1013.25, 65.0, "temperate"),
                "sahel" or "africa" => new AtmosphericConditions(28.0, 1010.0, 45.0, "tropical_dry"),
                "tropical" => new AtmosphericConditions(25.0, 1008.0, 80.0, "tropical_humid"),
                "desert" => new AtmosphericConditions(35.0, 1005.0, 20.0, "arid"),
                "arctic" => new AtmosphericConditions(-10.0, 1020.0, 70.0, "cold"),
                _ => new AtmosphericConditions() // Conditions par défaut
            };
        }
    }

    /// <summary>
    /// Résultat d'une correction atmosphérique
    /// </summary>
    public class AtmosphericCorrection
    {
        public double DistanceM { get; set; }                     // Distance de visée (m)
        public double RawDeltaH { get; set; }                     // Dénivelée brute (m)
        public double CurvatureCorrectionMm { get; set; }         // Correction courbure (mm)
        public double RefractionCorrectionMm { get; set; }        // Correction réfraction (mm)
        public double TotalCorrectionMm { get; set; }             // Correction totale (mm)
        public double CorrectedDeltaH { get; set; }               // Dénivelée corrigée (m)
        public double RefractionCoefficient { get; set; }         // Coefficient de réfraction k
        public double LevelApparentCorrectionMm { get; set; }     // Correction niveau apparent (mm)
        public AtmosphericConditions Conditions { get; set; } = null!; // Conditions utilisées
        public string CalculationMethod { get; set; } = string.Empty; // Méthode de calcul
        public DateTime CalculationTimestamp { get; set; }        // Timestamp du calcul
        public double SignificanceLevel { get; set; }             // Niveau de significativité
    }

    /// <summary>
    /// Rapport d'analyse des corrections atmosphériques
    /// </summary>
    public class AtmosphericCorrectionReport
    {
        public List<AtmosphericCorrection> Corrections { get; set; }
        public AtmosphericConditions Conditions { get; set; } = null!;
        public double TotalCorrectionMm { get; set; }
        public double MaxCorrectionMm { get; set; }
        public double MinCorrectionMm { get; set; }
        public double AverageCorrectionMm { get; set; }
        public double RmsCorrectionMm { get; set; }
        public int SignificantCorrections { get; set; }
        public string QualityAssessment { get; set; } = string.Empty;
        public Dictionary<string, double> StatisticsByDistance { get; set; }

        public AtmosphericCorrectionReport()
        {
            Corrections = new List<AtmosphericCorrection>();
            StatisticsByDistance = new Dictionary<string, double>();
        }
    }

    /// <summary>
    /// Correcteur atmosphérique avancé pour la compensation altimétrique
    /// Implémente les corrections de courbure et réfraction selon les normes géodésiques
    /// </summary>
    public class AtmosphericCorrector
    {
        private const double EARTH_RADIUS = 6371000.0;           // Rayon terrestre moyen (m)
        private const double SIGNIFICANCE_THRESHOLD_MM = 0.1;     // Seuil de significativité (mm)
        private const double MAX_DISTANCE_M = 500.0;             // Distance max recommandée (m)
        
        /// <summary>
        /// Calcule la correction atmosphérique complète pour une observation
        /// </summary>
        /// <param name="distanceM">Distance de visée en mètres</param>
        /// <param name="rawDeltaH">Dénivelée brute observée en mètres</param>
        /// <param name="conditions">Conditions atmosphériques</param>
        /// <returns>Correction atmosphérique complète</returns>
        public AtmosphericCorrection CalculateAtmosphericCorrection(
            double distanceM, 
            double rawDeltaH, 
            AtmosphericConditions? conditions = null)
        {
            if (conditions == null)
                conditions = new AtmosphericConditions();

            ValidateInputParameters(distanceM, rawDeltaH);

            try
            {
                // 1. Correction de courbure terrestre: Δh_courb = -d²/(2R)
                double curvatureMm = CalculateCurvatureCorrection(distanceM);

                // 2. Coefficient de réfraction selon les conditions atmosphériques
                double refractionCoeff = CalculateRefractionCoefficient(conditions);

                // 3. Correction de réfraction: Δh_réfr = k × Δh_courb
                double refractionMm = refractionCoeff * curvatureMm;

                // 4. Correction du niveau apparent (effet de la réfraction sur l'instrument)
                double levelApparentMm = CalculateLevelApparentCorrection(distanceM, conditions);

                // 5. Correction totale
                double totalMm = curvatureMm + refractionMm + levelApparentMm;

                // 6. Dénivelée corrigée
                double correctedDeltaH = rawDeltaH + (totalMm / 1000.0);

                // 7. Évaluation de la significativité
                double significance = Math.Abs(totalMm) / SIGNIFICANCE_THRESHOLD_MM;

                var correction = new AtmosphericCorrection
                {
                    DistanceM = distanceM,
                    RawDeltaH = rawDeltaH,
                    CurvatureCorrectionMm = curvatureMm,
                    RefractionCorrectionMm = refractionMm,
                    TotalCorrectionMm = totalMm,
                    CorrectedDeltaH = correctedDeltaH,
                    RefractionCoefficient = refractionCoeff,
                    LevelApparentCorrectionMm = levelApparentMm,
                    Conditions = conditions,
                    CalculationMethod = "Standard geodetic method",
                    CalculationTimestamp = DateTime.Now,
                    SignificanceLevel = significance
                };

                LogCorrectionCalculation(correction);

                return correction;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Erreur calcul correction atmosphérique: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calcule la correction de courbure terrestre
        /// Formule: Δh_courb = -d²/(2R) où d=distance, R=rayon terrestre
        /// </summary>
        private double CalculateCurvatureCorrection(double distanceM)
        {
            // Correction en millimètres (négative car la Terre est convexe)
            double correctionMm = -(distanceM * distanceM) / (2.0 * EARTH_RADIUS) * 1000.0;
            
            return correctionMm;
        }

        /// <summary>
        /// Calcule le coefficient de réfraction selon les conditions atmosphériques
        /// Le coefficient k varie typiquement entre 0.08 et 0.15 selon les conditions
        /// </summary>
        private double CalculateRefractionCoefficient(AtmosphericConditions conditions)
        {
            // Coefficient de base selon la région
            double baseCoeff = conditions.Region.ToLower() switch
            {
                "tropical_dry" or "sahel" => 0.12,      // Conditions chaudes et sèches
                "tropical_humid" => 0.10,               // Conditions chaudes et humides
                "temperate" => 0.13,                    // Conditions tempérées
                "arid" or "desert" => 0.15,             // Conditions arides
                "cold" or "arctic" => 0.08,             // Conditions froides
                _ => 0.13                               // Valeur par défaut
            };

            // Ajustements selon la température (effet dominant)
            double tempFactor = 1.0 + (conditions.TemperatureCelsius - 15.0) * 0.002;

            // Ajustements selon la pression atmosphérique
            double pressureFactor = conditions.PressureHPa / 1013.25;

            // Ajustements selon l'humidité
            double humidityFactor = 1.0 + (conditions.HumidityPercent - 65.0) * 0.0005;

            // Coefficient final avec limitations physiques
            double finalCoeff = baseCoeff * tempFactor * pressureFactor * humidityFactor;
            
            // Contraindre dans une plage réaliste
            finalCoeff = Math.Max(0.05, Math.Min(0.20, finalCoeff));

            return finalCoeff;
        }

        /// <summary>
        /// Calcule la correction du niveau apparent
        /// Due à la réfraction qui affecte l'horizontalité de l'instrument
        /// </summary>
        private double CalculateLevelApparentCorrection(double distanceM, AtmosphericConditions conditions)
        {
            // Cette correction est généralement très petite pour les distances courtes
            // Elle devient significative pour des distances > 200m
            
            if (distanceM < 100.0)
                return 0.0; // Négligeable pour courtes distances

            // Calcul basé sur le gradient de température vertical
            double temperatureGradient = EstimateTemperatureGradient(conditions);
            
            // Correction proportionnelle à la distance et au gradient
            double correctionMm = temperatureGradient * Math.Pow(distanceM / 100.0, 1.5) * 0.1;
            
            return correctionMm;
        }

        /// <summary>
        /// Estime le gradient de température vertical selon les conditions
        /// </summary>
        private double EstimateTemperatureGradient(AtmosphericConditions conditions)
        {
            // Gradient standard = -6.5°C/km, mais varie selon les conditions
            return conditions.WeatherCondition.ToLower() switch
            {
                "stable" => -5.0,          // Atmosphère stable
                "unstable" => -8.0,        // Atmosphère instable
                "inversion" => 2.0,        // Inversion de température
                _ => -6.5                  // Gradient standard
            };
        }

        /// <summary>
        /// Applique les corrections à un ensemble d'observations
        /// </summary>
        public List<AtmosphericCorrection> ApplyCorrections(
            IEnumerable<(double distance, double deltaH)> observations,
            AtmosphericConditions? conditions = null)
        {
            if (conditions == null)
                conditions = new AtmosphericConditions();

            var corrections = new List<AtmosphericCorrection>();

            Console.WriteLine($"🌡️ Application des corrections atmosphériques à {observations.Count()} observations");
            Console.WriteLine($"   Conditions: T={conditions.TemperatureCelsius}°C, P={conditions.PressureHPa}hPa, H={conditions.HumidityPercent}%");

            foreach (var (distance, deltaH) in observations)
            {
                var correction = CalculateAtmosphericCorrection(distance, deltaH, conditions);
                corrections.Add(correction);
            }

            Console.WriteLine($"✅ Corrections appliquées, total: {corrections.Sum(c => c.TotalCorrectionMm):F2}mm");

            return corrections;
        }

        /// <summary>
        /// Génère un rapport complet d'analyse des corrections
        /// </summary>
        public AtmosphericCorrectionReport GenerateCorrectionReport(
            List<AtmosphericCorrection> corrections,
            AtmosphericConditions conditions)
        {
            if (!corrections.Any())
                throw new ArgumentException("Aucune correction à analyser");

            var totalCorrectionsMm = corrections.Select(c => c.TotalCorrectionMm).ToArray();
            
            var report = new AtmosphericCorrectionReport
            {
                Corrections = corrections,
                Conditions = conditions,
                TotalCorrectionMm = totalCorrectionsMm.Sum(),
                MaxCorrectionMm = totalCorrectionsMm.Max(),
                MinCorrectionMm = totalCorrectionsMm.Min(),
                AverageCorrectionMm = totalCorrectionsMm.Average(),
                RmsCorrectionMm = Math.Sqrt(totalCorrectionsMm.Select(c => c * c).Average()),
                SignificantCorrections = corrections.Count(c => Math.Abs(c.TotalCorrectionMm) > SIGNIFICANCE_THRESHOLD_MM)
            };

            // Analyse par tranches de distance
            AnalyzeByDistanceRanges(corrections, report);

            // Évaluation de la qualité
            report.QualityAssessment = AssessCorrectionsQuality(report);

            return report;
        }

        /// <summary>
        /// Analyse les corrections par tranches de distance
        /// </summary>
        private void AnalyzeByDistanceRanges(List<AtmosphericCorrection> corrections, AtmosphericCorrectionReport report)
        {
            var distanceRanges = new[]
            {
                (0.0, 50.0, "0-50m"),
                (50.0, 100.0, "50-100m"),
                (100.0, 200.0, "100-200m"),
                (200.0, 500.0, "200-500m"),
                (500.0, double.MaxValue, ">500m")
            };

            foreach (var (minDist, maxDist, label) in distanceRanges)
            {
                var rangeCorrections = corrections
                    .Where(c => c.DistanceM >= minDist && c.DistanceM < maxDist)
                    .Select(c => c.TotalCorrectionMm)
                    .ToArray();

                if (rangeCorrections.Any())
                {
                    report.StatisticsByDistance[label] = rangeCorrections.Average();
                }
            }
        }

        /// <summary>
        /// Évalue la qualité globale des corrections
        /// </summary>
        private string AssessCorrectionsQuality(AtmosphericCorrectionReport report)
        {
            var quality = new List<string>();

            // Évaluation de l'amplitude des corrections
            if (Math.Abs(report.MaxCorrectionMm) < 1.0)
                quality.Add("Corrections mineures");
            else if (Math.Abs(report.MaxCorrectionMm) < 5.0)
                quality.Add("Corrections modérées");
            else
                quality.Add("Corrections importantes");

            // Évaluation de la significativité
            double significanceRatio = (double)report.SignificantCorrections / report.Corrections.Count;
            if (significanceRatio < 0.1)
                quality.Add("Impact négligeable");
            else if (significanceRatio < 0.5)
                quality.Add("Impact partiel");
            else
                quality.Add("Impact significatif");

            // Évaluation de la cohérence
            double variability = report.RmsCorrectionMm / Math.Max(Math.Abs(report.AverageCorrectionMm), 0.1);
            if (variability < 2.0)
                quality.Add("Corrections cohérentes");
            else
                quality.Add("Corrections variables");

            return string.Join(" | ", quality);
        }

        /// <summary>
        /// Génère un rapport textuel détaillé
        /// </summary>
        public string GenerateDetailedReport(AtmosphericCorrectionReport report)
        {
            var reportText = new System.Text.StringBuilder();

            reportText.AppendLine("═══════════════════════════════════════");
            reportText.AppendLine("    RAPPORT CORRECTIONS ATMOSPHÉRIQUES");
            reportText.AppendLine("═══════════════════════════════════════");
            reportText.AppendLine();

            // Conditions
            reportText.AppendLine("🌡️ CONDITIONS ATMOSPHÉRIQUES:");
            reportText.AppendLine($"   Température: {report.Conditions.TemperatureCelsius:F1}°C");
            reportText.AppendLine($"   Pression: {report.Conditions.PressureHPa:F1} hPa");
            reportText.AppendLine($"   Humidité: {report.Conditions.HumidityPercent:F1}%");
            reportText.AppendLine($"   Région: {report.Conditions.Region}");
            reportText.AppendLine();

            // Statistiques générales
            reportText.AppendLine("📊 STATISTIQUES GÉNÉRALES:");
            reportText.AppendLine($"   Observations corrigées: {report.Corrections.Count}");
            reportText.AppendLine($"   Correction totale: {report.TotalCorrectionMm:F2} mm");
            reportText.AppendLine($"   Correction moyenne: {report.AverageCorrectionMm:F2} mm");
            reportText.AppendLine($"   Correction RMS: {report.RmsCorrectionMm:F2} mm");
            reportText.AppendLine($"   Plage: [{report.MinCorrectionMm:F2}, {report.MaxCorrectionMm:F2}] mm");
            reportText.AppendLine();

            // Analyse par distance
            reportText.AppendLine("📏 ANALYSE PAR DISTANCE:");
            foreach (var kvp in report.StatisticsByDistance)
            {
                reportText.AppendLine($"   {kvp.Key}: {kvp.Value:F2} mm moyen");
            }
            reportText.AppendLine();

            // Significativité
            reportText.AppendLine("🎯 SIGNIFICATIVITÉ:");
            reportText.AppendLine($"   Corrections significatives: {report.SignificantCorrections}/{report.Corrections.Count}");
            reportText.AppendLine($"   Pourcentage d'impact: {(double)report.SignificantCorrections/report.Corrections.Count*100:F1}%");
            reportText.AppendLine();

            // Évaluation
            reportText.AppendLine("✅ ÉVALUATION:");
            reportText.AppendLine($"   {report.QualityAssessment}");
            reportText.AppendLine();

            // Recommandations
            reportText.AppendLine("💡 RECOMMANDATIONS:");
            if (Math.Abs(report.MaxCorrectionMm) > 2.0)
                reportText.AppendLine("   ⚠️ Corrections importantes détectées - Application recommandée");
            if (report.SignificantCorrections > report.Corrections.Count * 0.5)
                reportText.AppendLine("   ✅ Impact significatif - Corrections essentielles pour précision 2mm");
            if (report.Corrections.Any(c => c.DistanceM > MAX_DISTANCE_M))
                reportText.AppendLine("   ⚠️ Distances élevées détectées - Vérifier la fiabilité des corrections");
            
            reportText.AppendLine();
            reportText.AppendLine("═══════════════════════════════════════");

            return reportText.ToString();
        }

        /// <summary>
        /// Calcule l'impact estimé des corrections sur la précision finale
        /// </summary>
        public double EstimateImpactOnPrecision(List<AtmosphericCorrection> corrections)
        {
            if (!corrections.Any()) return 0.0;

            // RMS des corrections comme estimateur d'impact
            var rmsCorrection = Math.Sqrt(corrections.Select(c => c.TotalCorrectionMm * c.TotalCorrectionMm).Average());
            
            // Facteur d'amplification pour propagation d'erreur
            double propagationFactor = Math.Sqrt(corrections.Count) / corrections.Count;
            
            return rmsCorrection * propagationFactor;
        }

        /// <summary>
        /// Validation des paramètres d'entrée
        /// </summary>
        private void ValidateInputParameters(double distanceM, double rawDeltaH)
        {
            if (distanceM <= 0)
                throw new ArgumentException("La distance doit être positive", nameof(distanceM));
            
            if (distanceM > 1000)
                throw new ArgumentException("Distance trop importante (>1km) - fiabilité des corrections limitée", nameof(distanceM));
            
            if (Math.Abs(rawDeltaH) > 100)
                throw new ArgumentException("Dénivelée trop importante (>100m) - vérifier les données", nameof(rawDeltaH));
        }

        /// <summary>
        /// Journalisation des calculs pour debugging
        /// </summary>
        private void LogCorrectionCalculation(AtmosphericCorrection correction)
        {
            if (Math.Abs(correction.TotalCorrectionMm) > 1.0) // Log seulement les corrections significatives
            {
                Console.WriteLine($"🔧 Correction {correction.DistanceM:F0}m: " +
                                $"courb={correction.CurvatureCorrectionMm:F2}mm, " +
                                $"réfr={correction.RefractionCorrectionMm:F2}mm, " +
                                $"total={correction.TotalCorrectionMm:F2}mm");
            }
        }

        /// <summary>
        /// Teste la sensibilité des corrections aux conditions atmosphériques
        /// </summary>
        public Dictionary<string, double> AnalyzeSensitivity(double distanceM, double deltaH)
        {
            var baseConditions = new AtmosphericConditions();
            var baseCorrection = CalculateAtmosphericCorrection(distanceM, deltaH, baseConditions);
            
            var sensitivity = new Dictionary<string, double>();

            // Test sensibilité température (±10°C)
            var hotConditions = new AtmosphericConditions(25.0, baseConditions.PressureHPa, baseConditions.HumidityPercent);
            var coldConditions = new AtmosphericConditions(5.0, baseConditions.PressureHPa, baseConditions.HumidityPercent);
            var hotCorrection = CalculateAtmosphericCorrection(distanceM, deltaH, hotConditions);
            var coldCorrection = CalculateAtmosphericCorrection(distanceM, deltaH, coldConditions);
            sensitivity["Temperature_sensitivity_mm_per_10C"] = (hotCorrection.TotalCorrectionMm - coldCorrection.TotalCorrectionMm) / 20.0 * 10.0;

            // Test sensibilité pression (±20 hPa)
            var highPressure = new AtmosphericConditions(baseConditions.TemperatureCelsius, 1033.25, baseConditions.HumidityPercent);
            var lowPressure = new AtmosphericConditions(baseConditions.TemperatureCelsius, 993.25, baseConditions.HumidityPercent);
            var highPCorrection = CalculateAtmosphericCorrection(distanceM, deltaH, highPressure);
            var lowPCorrection = CalculateAtmosphericCorrection(distanceM, deltaH, lowPressure);
            sensitivity["Pressure_sensitivity_mm_per_20hPa"] = (highPCorrection.TotalCorrectionMm - lowPCorrection.TotalCorrectionMm) / 40.0 * 20.0;

            return sensitivity;
        }
    }

    /// <summary>
    /// Factory pour créer des conditions atmosphériques prédéfinies
    /// </summary>
    public static class AtmosphericConditionsFactory
    {
        /// <summary>
        /// Crée des conditions standard pour différentes régions du monde
        /// </summary>
        public static AtmosphericConditions CreateForRegion(string region)
        {
            return AtmosphericConditions.CreateStandardConditions(region);
        }

        /// <summary>
        /// Crée des conditions pour des situations spéciales
        /// </summary>
        public static AtmosphericConditions CreateForSpecialConditions(string condition)
        {
            return condition.ToLower() switch
            {
                "high_precision" => new AtmosphericConditions(15.0, 1013.25, 65.0, "temperate") { WeatherCondition = "stable" },
                "extreme_heat" => new AtmosphericConditions(45.0, 1000.0, 15.0, "desert"),
                "extreme_cold" => new AtmosphericConditions(-30.0, 1030.0, 80.0, "arctic"),
                "high_altitude" => new AtmosphericConditions(10.0, 700.0, 50.0, "mountain"),
                "sea_level" => new AtmosphericConditions(20.0, 1013.25, 75.0, "coastal"),
                _ => new AtmosphericConditions()
            };
        }
    }
}