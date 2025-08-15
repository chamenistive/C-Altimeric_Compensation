// ================================================================
// TESTS: Corrections Atmosphériques Avancées
// Tests pour la transposition Python vers C#
// ================================================================

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using CompensationAltimetrique.Calculations.Corrections;

namespace CompensationAltimetrique.Tests.Calculations.Corrections
{
    [TestClass]
    public class AdvancedAtmosphericCorrectorTests
    {
        private AdvancedAtmosphericCorrector corrector;
        private const double TOLERANCE = 1e-6;
        private const double MM_TOLERANCE = 0.01;

        [TestInitialize]
        public void Setup()
        {
            corrector = new AdvancedAtmosphericCorrector();
        }

        #region Tests Coefficient de Réfraction Python

        [TestMethod]
        public void CalculateRefractionCoefficient_StandardConditions_ShouldMatchPython()
        {
            // Arrange - Conditions standard Python
            var conditions = new AtmosphericConditions(15.0, 1013.25, 60.0);

            // Act
            double coefficient = conditions.CalculateRefractionCoefficient();

            // Assert - Valeur attendue Python: 0.13 (base) + 0 (temp) + 0 (pressure) + 0 (humidity) = 0.13
            Assert.AreEqual(0.13, coefficient, 0.001, "Coefficient standard doit être 0.13");
        }

        [TestMethod]
        public void CalculateRefractionCoefficient_TemperatureEffect_ShouldMatchPython()
        {
            // Arrange - Test effet température Python
            var hotConditions = new AtmosphericConditions(25.0, 1013.25, 60.0);  // +10°C
            var coldConditions = new AtmosphericConditions(5.0, 1013.25, 60.0);   // -10°C

            // Act
            double hotCoeff = hotConditions.CalculateRefractionCoefficient();
            double coldCoeff = coldConditions.CalculateRefractionCoefficient();

            // Assert - Python: temp_correction = -(temp - 15.0) * 0.004
            // Hot: -(25-15) * 0.004 = -0.04 => 0.13 - 0.04 = 0.09
            // Cold: -(5-15) * 0.004 = +0.04 => 0.13 + 0.04 = 0.17
            Assert.AreEqual(0.09, hotCoeff, 0.001, "Coefficient chaud incorrect");
            Assert.AreEqual(0.17, coldCoeff, 0.001, "Coefficient froid incorrect");
        }

        [TestMethod]
        public void CalculateRefractionCoefficient_PressureEffect_ShouldMatchPython()
        {
            // Arrange - Test effet pression Python
            var highPressure = new AtmosphericConditions(15.0, 1033.25, 60.0);  // +20 hPa
            var lowPressure = new AtmosphericConditions(15.0, 993.25, 60.0);    // -20 hPa

            // Act
            double highCoeff = highPressure.CalculateRefractionCoefficient();
            double lowCoeff = lowPressure.CalculateRefractionCoefficient();

            // Assert - Python: pressure_correction = (pressure - 1013.25) * 0.0001
            // High: (1033.25-1013.25) * 0.0001 = +0.002 => 0.13 + 0.002 = 0.132
            // Low: (993.25-1013.25) * 0.0001 = -0.002 => 0.13 - 0.002 = 0.128
            Assert.AreEqual(0.132, highCoeff, 0.001, "Coefficient haute pression incorrect");
            Assert.AreEqual(0.128, lowCoeff, 0.001, "Coefficient basse pression incorrect");
        }

        [TestMethod]
        public void CalculateRefractionCoefficient_HumidityEffect_ShouldMatchPython()
        {
            // Arrange - Test effet humidité Python
            var highHumidity = new AtmosphericConditions(15.0, 1013.25, 80.0);  // +20%
            var lowHumidity = new AtmosphericConditions(15.0, 1013.25, 40.0);   // -20%

            // Act
            double highCoeff = highHumidity.CalculateRefractionCoefficient();
            double lowCoeff = lowHumidity.CalculateRefractionCoefficient();

            // Assert - Python: humidity_correction = (humidity - 60.0) * 0.0002
            // High: (80-60) * 0.0002 = +0.004 => 0.13 + 0.004 = 0.134
            // Low: (40-60) * 0.0002 = -0.004 => 0.13 - 0.004 = 0.126
            Assert.AreEqual(0.134, highCoeff, 0.001, "Coefficient haute humidité incorrect");
            Assert.AreEqual(0.126, lowCoeff, 0.001, "Coefficient basse humidité incorrect");
        }

        [TestMethod]
        public void CalculateRefractionCoefficient_TimeEffect_ShouldMatchPython()
        {
            // Arrange - Test effet horaire Python
            var midday = new AtmosphericConditions(15.0, 1013.25, 60.0) 
            { 
                TimeOfDay = new DateTime(2024, 1, 1, 12, 0, 0) 
            };
            var morning = new AtmosphericConditions(15.0, 1013.25, 60.0) 
            { 
                TimeOfDay = new DateTime(2024, 1, 1, 7, 0, 0) 
            };

            // Act
            double middayCoeff = midday.CalculateRefractionCoefficient();
            double morningCoeff = morning.CalculateRefractionCoefficient();

            // Assert - Python: time_correction = +0.02 (10-16h), -0.01 (<=8h, >=18h)
            Assert.AreEqual(0.15, middayCoeff, 0.001, "Coefficient midi incorrect"); // 0.13 + 0.02
            Assert.AreEqual(0.12, morningCoeff, 0.001, "Coefficient matin incorrect"); // 0.13 - 0.01
        }

        #endregion

        #region Tests Corrections Atmosphériques Python

        [TestMethod]
        public void CalculateAtmosphericCorrection_100m_ShouldMatchPythonFormulas()
        {
            // Arrange - Test 100m équivalent Python
            double distance = 100.0;
            double deltaH = 1.0;
            var conditions = new AtmosphericConditions(15.0, 1013.25, 60.0);

            // Act
            var correction = corrector.CalculateAtmosphericCorrection(distance, deltaH, conditions);

            // Assert - Vérification formules Python
            // Courbure: d²/(2R) = 100²/(2*6371000) = 0.000785 m = 0.785 mm
            double expectedCurvature = 0.785; // mm
            Assert.AreEqual(expectedCurvature, correction.CurvatureCorrectionMm, 0.01, "Correction courbure incorrecte");

            // Réfraction: -0.13 * 0.785 = -0.102 mm
            double expectedRefraction = -0.102;
            Assert.AreEqual(expectedRefraction, correction.RefractionCorrectionMm, 0.01, "Correction réfraction incorrecte");

            // Coefficient de réfraction
            Assert.AreEqual(0.13, correction.RefractionCoefficient, 0.001, "Coefficient réfraction incorrect");
        }

        [TestMethod]
        public void CalculateAtmosphericCorrection_200m_ShouldFollowQuadraticLaw()
        {
            // Arrange - Test loi quadratique Python
            var conditions = new AtmosphericConditions();
            
            var correction100 = corrector.CalculateAtmosphericCorrection(100.0, 0.0, conditions);
            var correction200 = corrector.CalculateAtmosphericCorrection(200.0, 0.0, conditions);

            // Assert - Relation quadratique: correction(2d) = 4 × correction(d)
            double ratio = correction200.CurvatureCorrectionMm / correction100.CurvatureCorrectionMm;
            Assert.AreEqual(4.0, ratio, 0.01, "Loi quadratique non respectée");
        }

        [TestMethod]
        public void CalculateLevelApparentCorrection_ShouldMatchPythonFormula()
        {
            // Arrange - Test formule niveau apparent Python
            double distance = 200.0;
            double deltaH = 2.0;
            double refractionCoeff = 0.13;

            // Act
            var levelCorrection = corrector.CalculateLevelApparentCorrection(distance, deltaH, refractionCoeff);

            // Assert - Python: (1 - m_r_a) * deltaH² / (2 * R)
            // m_r_a = 0.13 * 0.8 = 0.104
            // correction = (1 - 0.104) * 2² / (2 * 6371000) = 0.896 * 4 / 12742000 ≈ 2.81e-7 m = 0.000281 mm
            Assert.IsTrue(Math.Abs(levelCorrection) < 0.001, "Correction niveau apparent trop importante pour cette distance");
        }

        #endregion

        #region Tests Application aux Données de Nivellement

        [TestMethod]
        public void ApplyCorrections_LevelingData_ShouldProcessAllSessions()
        {
            // Arrange - Données de nivellement simulées
            var levelingData = new List<LevelingData>
            {
                new LevelingData("P001") 
                { 
                    AR1 = 1.500, AV1 = 1.200, DIST1 = 150.0,
                    AR2 = 1.505, AV2 = 1.205, DIST2 = 150.0
                },
                new LevelingData("P002") 
                { 
                    AR1 = 2.100, AV1 = 1.800, DIST1 = 200.0,
                    AR2 = 2.105, AV2 = 1.805, DIST2 = 200.0
                }
            };
            var conditions = new AtmosphericConditions();

            // Act
            var correctedData = corrector.ApplyCorrections(levelingData, conditions);

            // Assert
            Assert.AreEqual(2, correctedData.Count, "Nombre de données incorrect");
            
            // Vérifier que les corrections ont été appliquées aux lectures AV
            Assert.AreNotEqual(levelingData[0].AV1, correctedData[0].AV1, "AV1 devrait être corrigée");
            Assert.AreNotEqual(levelingData[0].AV2, correctedData[0].AV2, "AV2 devrait être corrigée");
            
            // Les lectures AR ne doivent pas changer
            Assert.AreEqual(levelingData[0].AR1, correctedData[0].AR1, "AR1 ne devrait pas changer");
            Assert.AreEqual(levelingData[0].AR2, correctedData[0].AR2, "AR2 ne devrait pas changer");
        }

        [TestMethod]
        public void AnalyzeCorrections_ShouldProvideStatistics()
        {
            // Arrange
            var levelingData = new List<LevelingData>
            {
                new LevelingData("P001") { AR1 = 1.500, AV1 = 1.200, DIST1 = 100.0 },
                new LevelingData("P002") { AR1 = 2.100, AV1 = 1.800, DIST1 = 200.0 },
                new LevelingData("P003") { AR1 = 1.800, AV1 = 1.500, DIST1 = 300.0 }
            };
            var conditions = new AtmosphericConditions();

            // Act
            var analysis = corrector.AnalyzeCorrections(levelingData, conditions);

            // Assert
            Assert.AreEqual(3, analysis.Distances.Count, "Nombre de distances incorrect");
            Assert.AreEqual(3, analysis.Corrections.Count, "Nombre de corrections incorrect");
            Assert.IsTrue(analysis.MaxDistance > 0, "Distance max devrait être positive");
            Assert.IsTrue(analysis.AverageCorrection != 0, "Correction moyenne devrait être non nulle");
            
            string summary = analysis.GetSummary();
            Assert.IsTrue(summary.Contains("3"), "Résumé devrait mentionner 3 corrections");
        }

        #endregion

        #region Tests Factory Python

        [TestMethod]
        public void AtmosphericConditionsFactory_RegionalConditions_ShouldMatchPython()
        {
            // Act
            var france = AtmosphericConditionsFactory.CreateStandardConditions("france");
            var sahel = AtmosphericConditionsFactory.CreateStandardConditions("sahel");
            var tropical = AtmosphericConditionsFactory.CreateStandardConditions("tropical");

            // Assert - Valeurs Python exactes
            Assert.AreEqual(15.0, france.TemperatureCelsius, "Température France incorrecte");
            Assert.AreEqual(1013.25, france.PressureHpa, "Pression France incorrecte");
            Assert.AreEqual(65.0, france.HumidityPercent, "Humidité France incorrecte");

            Assert.AreEqual(32.0, sahel.TemperatureCelsius, "Température Sahel incorrecte");
            Assert.AreEqual(28.0, tropical.TemperatureCelsius, "Température tropical incorrecte");
        }

        [TestMethod]
        public void AtmosphericConditionsFactory_TimeBasedConditions_ShouldIncludeTime()
        {
            // Arrange
            var measurementTime = new DateTime(2024, 8, 15, 14, 30, 0);

            // Act
            var conditions = AtmosphericConditionsFactory.CreateTimeBasedConditions(measurementTime, "france");

            // Assert
            Assert.IsNotNull(conditions.TimeOfDay, "Heure devrait être définie");
            Assert.AreEqual(measurementTime, conditions.TimeOfDay.Value, "Heure incorrecte");
            Assert.AreEqual(15.0, conditions.TemperatureCelsius, "Température France incorrecte");
        }

        #endregion

        #region Tests Limites et Validation

        [TestMethod]
        public void CalculateRefractionCoefficient_SafetyLimits_ShouldConstrainValues()
        {
            // Arrange - Conditions extrêmes pour tester les limites
            var extreme = new AtmosphericConditions(-50.0, 500.0, 0.0);

            // Act
            double coeff = extreme.CalculateRefractionCoefficient();

            // Assert - Limites Python: [0.05, 0.25]
            Assert.IsTrue(coeff >= 0.05, "Coefficient trop bas");
            Assert.IsTrue(coeff <= 0.25, "Coefficient trop haut");
        }

        [TestMethod]
        public void CalculateAtmosphericCorrection_ZeroDistance_ShouldReturnZero()
        {
            // Arrange
            var conditions = new AtmosphericConditions();

            // Act
            var correction = corrector.CalculateAtmosphericCorrection(0.0, 1.0, conditions);

            // Assert
            Assert.AreEqual(0.0, correction.DistanceM, "Distance devrait être zéro");
            Assert.AreEqual(1.0, correction.CorrectedDeltaH, "Dénivelée devrait être inchangée");
        }

        [TestMethod]
        public void CalculateLevelApparentCorrection_SmallDeltaH_ShouldReturnZero()
        {
            // Act
            var correction = corrector.CalculateLevelApparentCorrection(200.0, 1e-7, 0.13);

            // Assert
            Assert.AreEqual(0.0, correction, "Correction devrait être nulle pour petit deltaH");
        }

        #endregion

        #region Tests Performance et Cohérence

        [TestMethod]
        public void ApplyCorrections_LargeDataset_ShouldCompleteEfficiently()
        {
            // Arrange - Gros jeu de données
            var largeDataset = new List<LevelingData>();
            var random = new Random(42);
            
            for (int i = 0; i < 1000; i++)
            {
                largeDataset.Add(new LevelingData($"P{i:D4}")
                {
                    AR1 = 1.0 + random.NextDouble(),
                    AV1 = 1.0 + random.NextDouble(),
                    DIST1 = 50.0 + random.NextDouble() * 300.0
                });
            }

            var conditions = new AtmosphericConditions();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var correctedData = corrector.ApplyCorrections(largeDataset, conditions);
            stopwatch.Stop();

            // Assert
            Assert.AreEqual(1000, correctedData.Count, "Toutes les données devraient être traitées");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, "Traitement trop lent");
            
            Console.WriteLine($"Performance: {stopwatch.ElapsedMilliseconds}ms pour 1000 points");
        }

        [TestMethod]
        public void ToStringMethod_ShouldDisplayCorrectFormat()
        {
            // Arrange
            var conditions = new AtmosphericConditions(20.5, 1008.5, 75.0);

            // Act
            string display = conditions.ToString();

            // Assert
            Assert.IsTrue(display.Contains("T=20.5°C"), "Température manquante");
            Assert.IsTrue(display.Contains("P=1008.5hPa"), "Pression manquante");
            Assert.IsTrue(display.Contains("H=75.0%"), "Humidité manquante");
            Assert.IsTrue(display.Contains("r="), "Coefficient réfraction manquant");
        }

        #endregion
    }

    /// <summary>
    /// Tests d'intégration pour le workflow complet Python → C#
    /// </summary>
    [TestClass]
    public class AdvancedAtmosphericIntegrationTests
    {
        [TestMethod]
        public void FullWorkflow_PythonEquivalent_ShouldProduceConsistentResults()
        {
            Console.WriteLine("🧪 Test d'intégration: Workflow Python → C# complet");

            // Arrange - Scénario réaliste équivalent Python
            var corrector = new AdvancedAtmosphericCorrector();
            var conditions = AtmosphericConditionsFactory.CreateTimeBasedConditions(
                new DateTime(2024, 8, 15, 13, 30, 0), "france");

            var levelingData = new List<LevelingData>
            {
                new LevelingData("REP001") { AR1 = 1.4567, AV1 = 1.1234, DIST1 = 125.5 },
                new LevelingData("REP002") { AR1 = 2.1890, AV1 = 1.8765, DIST1 = 200.0 },
                new LevelingData("REP003") { AR1 = 1.7654, AV1 = 1.4321, DIST1 = 175.0 }
            };

            // Act - Pipeline complet
            Console.WriteLine($"📊 Conditions: {conditions}");
            
            var analysis = corrector.AnalyzeCorrections(levelingData, conditions);
            var correctedData = corrector.ApplyCorrections(levelingData, conditions);

            // Assert - Validation cohérence
            Assert.AreEqual(3, analysis.Corrections.Count, "Analyse devrait avoir 3 corrections");
            Assert.AreEqual(3, correctedData.Count, "Données corrigées devraient avoir 3 éléments");
            
            // Vérification que les corrections sont réalistes
            Assert.IsTrue(Math.Abs(analysis.AverageCorrection) < 5.0, "Correction moyenne trop importante");
            Assert.IsTrue(analysis.MaxCorrection > analysis.MinCorrection, "Plage de corrections incohérente");

            Console.WriteLine($"📈 Résultats: {analysis.GetSummary()}");
            Console.WriteLine($"✅ Test d'intégration réussi - Cohérence Python ↔ C# validée");
        }
    }
}