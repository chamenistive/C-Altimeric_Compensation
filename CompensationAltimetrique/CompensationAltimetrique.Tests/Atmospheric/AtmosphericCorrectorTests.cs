// ============================================================================
// TESTS UNITAIRES - ÉTAPE 3: CORRECTIONS ATMOSPHÉRIQUES
// Tests complets pour la validation des corrections de courbure et réfraction
// ============================================================================

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using CompensationAltimetrique.Atmospheric;
using System.Collections.Generic;

namespace CompensationAltimetrique.Tests.Atmospheric
{
    [TestClass]
    public class AtmosphericCorrectorTests
    {
        private AtmosphericCorrector corrector;
        private const double TOLERANCE = 1e-6;

        [TestInitialize]
        public void Setup()
        {
            corrector = new AtmosphericCorrector();
        }

        #region Tests de base

        [TestMethod]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var atmosphericCorrector = new AtmosphericCorrector();

            // Assert
            Assert.IsNotNull(atmosphericCorrector);
        }

        [TestMethod]
        public void AtmosphericConditions_DefaultConstructor_ShouldHaveStandardValues()
        {
            // Arrange & Act
            var conditions = new AtmosphericConditions();

            // Assert
            Assert.AreEqual(15.0, conditions.TemperatureCelsius);
            Assert.AreEqual(1013.25, conditions.PressureHPa);
            Assert.AreEqual(65.0, conditions.HumidityPercent);
            Assert.AreEqual("temperate", conditions.WeatherCondition);
            Assert.AreEqual("temperate", conditions.Region);
        }

        [TestMethod]
        public void AtmosphericConditions_ParameterizedConstructor_ShouldSetCorrectValues()
        {
            // Arrange & Act
            var conditions = new AtmosphericConditions(25.0, 1008.0, 80.0, "tropical");

            // Assert
            Assert.AreEqual(25.0, conditions.TemperatureCelsius);
            Assert.AreEqual(1008.0, conditions.PressureHPa);
            Assert.AreEqual(80.0, conditions.HumidityPercent);
            Assert.AreEqual("tropical", conditions.Region);
        }

        #endregion

        #region Tests de validation des paramètres

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CalculateAtmosphericCorrection_WithNegativeDistance_ShouldThrowException()
        {
            // Arrange, Act & Assert
            corrector.CalculateAtmosphericCorrection(-50.0, 1.0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CalculateAtmosphericCorrection_WithZeroDistance_ShouldThrowException()
        {
            // Arrange, Act & Assert
            corrector.CalculateAtmosphericCorrection(0.0, 1.0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CalculateAtmosphericCorrection_WithExcessiveDistance_ShouldThrowException()
        {
            // Arrange, Act & Assert
            corrector.CalculateAtmosphericCorrection(1500.0, 1.0); // > 1km
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CalculateAtmosphericCorrection_WithExcessiveDeltaH_ShouldThrowException()
        {
            // Arrange, Act & Assert
            corrector.CalculateAtmosphericCorrection(100.0, 150.0); // > 100m
        }

        #endregion

        #region Tests des calculs de correction

        [TestMethod]
        public void CalculateAtmosphericCorrection_ShortDistance_ShouldHaveSmallCorrection()
        {
            // Arrange
            double distance = 50.0; // 50m
            double deltaH = 1.0;    // 1m

            // Act
            var correction = corrector.CalculateAtmosphericCorrection(distance, deltaH);

            // Assert
            Assert.IsNotNull(correction);
            Assert.AreEqual(distance, correction.DistanceM);
            Assert.AreEqual(deltaH, correction.RawDeltaH);
            
            // Correction de courbure pour 50m: -50²/(2×6371000) × 1000 ≈ -0.196 mm
            Assert.IsTrue(Math.Abs(correction.CurvatureCorrectionMm + 0.196) < 0.01);
            
            // Correction totale doit être négative (courbure dominante)
            Assert.IsTrue(correction.TotalCorrectionMm < 0);
            
            // Dénivelée corrigée légèrement différente
            Assert.AreNotEqual(deltaH, correction.CorrectedDeltaH);
        }

        [TestMethod]
        public void CalculateAtmosphericCorrection_MediumDistance_ShouldHaveModerateCorrection()
        {
            // Arrange
            double distance = 200.0; // 200m
            double deltaH = 2.0;     // 2m

            // Act
            var correction = corrector.CalculateAtmosphericCorrection(distance, deltaH);

            // Assert
            // Correction de courbure pour 200m: -200²/(2×6371000) × 1000 ≈ -3.14 mm
            Assert.IsTrue(Math.Abs(correction.CurvatureCorrectionMm + 3.14) < 0.1);
            
            // La réfraction doit partiellement compenser la courbure
            Assert.IsTrue(Math.Abs(correction.RefractionCorrectionMm) > 0.1);
            Assert.IsTrue(correction.RefractionCorrectionMm > 0); // Positive, compense courbure
            
            // Correction totale moindre que courbure seule (en valeur absolue)
            // La correction de réfraction compense partiellement la courbure
            Assert.IsTrue(Math.Abs(correction.TotalCorrectionMm) <= Math.Abs(correction.CurvatureCorrectionMm));
        }

        [TestMethod]
        public void CalculateAtmosphericCorrection_LongDistance_ShouldHaveLargeCorrection()
        {
            // Arrange
            double distance = 500.0; // 500m (limite recommandée)
            double deltaH = 5.0;     // 5m

            // Act
            var correction = corrector.CalculateAtmosphericCorrection(distance, deltaH);

            // Assert
            // Correction de courbure pour 500m: -500²/(2×6371000) × 1000 ≈ -19.6 mm
            Assert.IsTrue(Math.Abs(correction.CurvatureCorrectionMm + 19.6) < 0.5);
            
            // Corrections significatives
            Assert.IsTrue(Math.Abs(correction.TotalCorrectionMm) > 5.0);
            Assert.IsTrue(correction.SignificanceLevel > 50); // Très significatif
            
            // Correction niveau apparent présente pour grande distance
            Assert.IsTrue(Math.Abs(correction.LevelApparentCorrectionMm) > 0);
        }

        #endregion

        #region Tests des conditions atmosphériques

        [TestMethod]
        public void CalculateAtmosphericCorrection_DifferentRegions_ShouldHaveDifferentRefractionCoefficients()
        {
            // Arrange
            double distance = 300.0;
            double deltaH = 1.0;
            
            var temperateConditions = AtmosphericConditions.CreateStandardConditions("temperate");
            var desertConditions = AtmosphericConditions.CreateStandardConditions("desert");
            var arcticConditions = AtmosphericConditions.CreateStandardConditions("arctic");

            // Act
            var temperateCorrection = corrector.CalculateAtmosphericCorrection(distance, deltaH, temperateConditions);
            var desertCorrection = corrector.CalculateAtmosphericCorrection(distance, deltaH, desertConditions);
            var arcticCorrection = corrector.CalculateAtmosphericCorrection(distance, deltaH, arcticConditions);

            // Assert
            // Les coefficients de réfraction doivent être différents
            Assert.AreNotEqual(temperateCorrection.RefractionCoefficient, desertCorrection.RefractionCoefficient);
            Assert.AreNotEqual(temperateCorrection.RefractionCoefficient, arcticCorrection.RefractionCoefficient);
            
            // Desert > Temperate > Arctic pour le coefficient
            Assert.IsTrue(desertCorrection.RefractionCoefficient > temperateCorrection.RefractionCoefficient);
            Assert.IsTrue(temperateCorrection.RefractionCoefficient > arcticCorrection.RefractionCoefficient);
        }

        [TestMethod]
        public void CalculateAtmosphericCorrection_ExtremeTemperatures_ShouldAffectRefractionCoefficient()
        {
            // Arrange
            double distance = 200.0;
            double deltaH = 1.0;
            
            var hotConditions = new AtmosphericConditions(40.0, 1013.25, 65.0);  // 40°C
            var coldConditions = new AtmosphericConditions(-20.0, 1013.25, 65.0); // -20°C

            // Act
            var hotCorrection = corrector.CalculateAtmosphericCorrection(distance, deltaH, hotConditions);
            var coldCorrection = corrector.CalculateAtmosphericCorrection(distance, deltaH, coldConditions);

            // Assert
            // La température chaude doit donner un coefficient plus élevé
            Assert.IsTrue(hotCorrection.RefractionCoefficient > coldCorrection.RefractionCoefficient);
            
            // Les corrections totales doivent être différentes
            Assert.AreNotEqual(hotCorrection.TotalCorrectionMm, coldCorrection.TotalCorrectionMm);
        }

        #endregion

        #region Tests des corrections multiples

        [TestMethod]
        public void ApplyCorrections_MultipleObservations_ShouldReturnAllCorrections()
        {
            // Arrange
            var observations = new List<(double distance, double deltaH)>
            {
                (50.0, 0.5),
                (100.0, 1.0),
                (150.0, -0.8),
                (200.0, 2.0),
                (300.0, 1.5)
            };
            
            var conditions = new AtmosphericConditions(20.0, 1010.0, 70.0);

            // Act
            var corrections = corrector.ApplyCorrections(observations, conditions);

            // Assert
            Assert.AreEqual(5, corrections.Count);
            
            // Vérifier que les corrections sont dans l'ordre
            for (int i = 0; i < observations.Count; i++)
            {
                Assert.AreEqual(observations[i].distance, corrections[i].DistanceM);
                Assert.AreEqual(observations[i].deltaH, corrections[i].RawDeltaH);
            }
            
            // Les corrections doivent augmenter avec la distance
            for (int i = 1; i < corrections.Count; i++)
            {
                Assert.IsTrue(Math.Abs(corrections[i].CurvatureCorrectionMm) > 
                             Math.Abs(corrections[i-1].CurvatureCorrectionMm));
            }
        }

        #endregion

        #region Tests des rapports

        [TestMethod]
        public void GenerateCorrectionReport_ValidCorrections_ShouldProduceComprehensiveReport()
        {
            // Arrange
            var observations = new List<(double, double)>
            {
                (100.0, 1.0), (200.0, 0.5), (300.0, -1.0), (400.0, 2.0)
            };
            
            var conditions = new AtmosphericConditions();
            var corrections = corrector.ApplyCorrections(observations, conditions);

            // Act
            var report = corrector.GenerateCorrectionReport(corrections, conditions);

            // Assert
            Assert.IsNotNull(report);
            Assert.AreEqual(4, report.Corrections.Count);
            Assert.IsTrue(report.TotalCorrectionMm != 0);
            Assert.IsTrue(report.MaxCorrectionMm < 0); // Corrections négatives
            Assert.IsTrue(report.SignificantCorrections >= 0);
            Assert.IsNotNull(report.QualityAssessment);
            Assert.IsTrue(report.StatisticsByDistance.Count > 0);
        }

        [TestMethod]
        public void GenerateDetailedReport_ValidReport_ShouldContainAllSections()
        {
            // Arrange
            var corrections = new List<AtmosphericCorrection>
            {
                corrector.CalculateAtmosphericCorrection(150.0, 1.0),
                corrector.CalculateAtmosphericCorrection(250.0, 2.0)
            };
            
            var conditions = new AtmosphericConditions();
            var report = corrector.GenerateCorrectionReport(corrections, conditions);

            // Act
            var detailedReport = corrector.GenerateDetailedReport(report);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(detailedReport));
            Assert.IsTrue(detailedReport.Contains("CONDITIONS ATMOSPHÉRIQUES"));
            Assert.IsTrue(detailedReport.Contains("STATISTIQUES GÉNÉRALES"));
            Assert.IsTrue(detailedReport.Contains("ANALYSE PAR DISTANCE"));
            Assert.IsTrue(detailedReport.Contains("SIGNIFICATIVITÉ"));
            Assert.IsTrue(detailedReport.Contains("RECOMMANDATIONS"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GenerateCorrectionReport_EmptyCorrections_ShouldThrowException()
        {
            // Arrange
            var emptyCorrections = new List<AtmosphericCorrection>();
            var conditions = new AtmosphericConditions();

            // Act & Assert
            corrector.GenerateCorrectionReport(emptyCorrections, conditions);
        }

        #endregion

        #region Tests des conditions prédéfinies

        [TestMethod]
        public void CreateStandardConditions_SupportedRegions_ShouldReturnCorrectValues()
        {
            // Arrange & Act
            var france = AtmosphericConditions.CreateStandardConditions("france");
            var sahel = AtmosphericConditions.CreateStandardConditions("sahel");
            var tropical = AtmosphericConditions.CreateStandardConditions("tropical");
            var desert = AtmosphericConditions.CreateStandardConditions("desert");
            var arctic = AtmosphericConditions.CreateStandardConditions("arctic");

            // Assert
            Assert.AreEqual("temperate", france.Region);
            Assert.AreEqual(15.0, france.TemperatureCelsius);
            
            Assert.AreEqual("tropical_dry", sahel.Region);
            Assert.AreEqual(28.0, sahel.TemperatureCelsius);
            
            Assert.AreEqual("tropical_humid", tropical.Region);
            Assert.AreEqual(25.0, tropical.TemperatureCelsius);
            
            Assert.AreEqual("arid", desert.Region);
            Assert.AreEqual(35.0, desert.TemperatureCelsius);
            
            Assert.AreEqual("cold", arctic.Region);
            Assert.AreEqual(-10.0, arctic.TemperatureCelsius);
        }

        [TestMethod]
        public void AtmosphericConditionsFactory_SpecialConditions_ShouldCreateCorrectly()
        {
            // Arrange & Act
            var highPrecision = AtmosphericConditionsFactory.CreateForSpecialConditions("high_precision");
            var extremeHeat = AtmosphericConditionsFactory.CreateForSpecialConditions("extreme_heat");
            var extremeCold = AtmosphericConditionsFactory.CreateForSpecialConditions("extreme_cold");

            // Assert
            Assert.AreEqual("stable", highPrecision.WeatherCondition);
            Assert.AreEqual(45.0, extremeHeat.TemperatureCelsius);
            Assert.AreEqual(-30.0, extremeCold.TemperatureCelsius);
        }

        #endregion

        #region Tests d'analyse de sensibilité

        [TestMethod]
        public void AnalyzeSensitivity_StandardDistance_ShouldReturnSensitivityData()
        {
            // Arrange
            double distance = 300.0;
            double deltaH = 1.0;

            // Act
            var sensitivity = corrector.AnalyzeSensitivity(distance, deltaH);

            // Assert
            Assert.IsNotNull(sensitivity);
            Assert.IsTrue(sensitivity.ContainsKey("Temperature_sensitivity_mm_per_10C"));
            Assert.IsTrue(sensitivity.ContainsKey("Pressure_sensitivity_mm_per_20hPa"));
            
            // Les sensibilités doivent être non nulles pour des distances significatives
            Assert.AreNotEqual(0.0, sensitivity["Temperature_sensitivity_mm_per_10C"]);
            Assert.AreNotEqual(0.0, sensitivity["Pressure_sensitivity_mm_per_20hPa"]);
        }

        #endregion

        #region Tests d'impact sur la précision

        [TestMethod]
        public void EstimateImpactOnPrecision_MultipleCorrections_ShouldReturnReasonableValue()
        {
            // Arrange
            var corrections = new List<AtmosphericCorrection>
            {
                corrector.CalculateAtmosphericCorrection(100.0, 1.0),
                corrector.CalculateAtmosphericCorrection(200.0, 0.5),
                corrector.CalculateAtmosphericCorrection(300.0, 1.5),
                corrector.CalculateAtmosphericCorrection(400.0, -1.0)
            };

            // Act
            double impact = corrector.EstimateImpactOnPrecision(corrections);

            // Assert
            Assert.IsTrue(impact > 0);
            Assert.IsTrue(impact < 10.0); // Impact raisonnable pour ces distances
        }

        [TestMethod]
        public void EstimateImpactOnPrecision_EmptyCorrections_ShouldReturnZero()
        {
            // Arrange
            var emptyCorrections = new List<AtmosphericCorrection>();

            // Act
            double impact = corrector.EstimateImpactOnPrecision(emptyCorrections);

            // Assert
            Assert.AreEqual(0.0, impact);
        }

        #endregion

        #region Tests de cohérence physique

        [TestMethod]
        public void AtmosphericCorrections_PhysicalConsistency_ShouldBeCoherent()
        {
            // Arrange
            double distance = 200.0;
            double deltaH = 1.0;

            // Act
            var correction = corrector.CalculateAtmosphericCorrection(distance, deltaH);

            // Assert
            // 1. La correction de courbure doit être négative (Terre convexe)
            Assert.IsTrue(correction.CurvatureCorrectionMm < 0);
            
            // 2. Le coefficient de réfraction doit être dans une plage réaliste
            Assert.IsTrue(correction.RefractionCoefficient >= 0.05);
            Assert.IsTrue(correction.RefractionCoefficient <= 0.20);
            
            // 3. La correction de réfraction doit partiellement compenser la courbure
            Assert.IsTrue(correction.RefractionCorrectionMm > 0);
            Assert.IsTrue(Math.Abs(correction.RefractionCorrectionMm) <= Math.Abs(correction.CurvatureCorrectionMm));
            
            // 4. Les timestamps doivent être récents
            Assert.IsTrue((DateTime.Now - correction.CalculationTimestamp).TotalSeconds < 10);
        }

        [TestMethod]
        public void AtmosphericCorrections_DistanceScaling_ShouldFollowQuadraticLaw()
        {
            // Arrange
            double deltaH = 1.0;
            var distances = new[] { 100.0, 200.0, 400.0 };

            // Act
            var corrections = distances.Select(d => corrector.CalculateAtmosphericCorrection(d, deltaH)).ToArray();

            // Assert
            // La correction de courbure suit la loi quadratique: correction ∝ d²
            double ratio1 = corrections[1].CurvatureCorrectionMm / corrections[0].CurvatureCorrectionMm;
            double ratio2 = corrections[2].CurvatureCorrectionMm / corrections[1].CurvatureCorrectionMm;
            
            // 200²/100² = 4, 400²/200² = 4
            Assert.IsTrue(Math.Abs(ratio1 - 4.0) < 0.1);
            Assert.IsTrue(Math.Abs(ratio2 - 4.0) < 0.1);
        }

        #endregion
    }

    /// <summary>
    /// Tests de performance pour le module atmosphérique
    /// </summary>
    [TestClass]
    public class AtmosphericPerformanceTests
    {
        [TestMethod]
        public void AtmosphericCorrections_LargeDataset_ShouldCompleteInReasonableTime()
        {
            // Arrange
            var corrector = new AtmosphericCorrector();
            var conditions = new AtmosphericConditions();
            
            // Générer 1000 observations
            var observations = new List<(double, double)>();
            var random = new Random(42);
            
            for (int i = 0; i < 1000; i++)
            {
                double distance = random.NextDouble() * 400 + 50; // 50-450m
                double deltaH = random.NextDouble() * 10 - 5;     // -5m à +5m
                observations.Add((distance, deltaH));
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var corrections = corrector.ApplyCorrections(observations, conditions);
            var report = corrector.GenerateCorrectionReport(corrections, conditions);
            
            stopwatch.Stop();

            // Assert
            Assert.AreEqual(1000, corrections.Count);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, // Moins de 5 secondes
                $"Calculs trop lents: {stopwatch.ElapsedMilliseconds}ms");
            
            Assert.IsNotNull(report);
            Assert.IsTrue(report.SignificantCorrections > 0);
        }
    }
}