// ================================================================
// TESTS PRODUCTION - Validation Transposition Python → C#
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompensationAltimetrique.Calculations.Corrections;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Tests.Calculations.Corrections
{
    [TestClass]
    public class AtmosphericCorrectorProductionTests
    {
        private AtmosphericCorrector? _corrector;

        [TestInitialize]
        public void Setup() => _corrector = new AtmosphericCorrector();

        [TestMethod]
        public void ValidatePythonReferenceTable_ShouldMatchExactly()
        {
            var referenceTable = AtmosphericConditionsFactory.GetPythonReferenceTable();
            var franceConditions = AtmosphericConditionsFactory.CreateStandardConditions("france");

            foreach (var entry in referenceTable)
            {
                var result = _corrector!.CalculateAtmosphericCorrection(entry.Key, 0.0, franceConditions);
                Assert.AreEqual(entry.Value, result.TotalCorrectionMm, 0.05,
                    $"Distance {entry.Key}m: attendu {entry.Value}mm, obtenu {result.TotalCorrectionMm:F2}mm");
            }
        }

        [TestMethod] 
        public void ValidateRegionalConditions_ShouldMatchPythonCoefficients()
        {
            var franceConditions = AtmosphericConditionsFactory.CreateStandardConditions("france");
            var sahelConditions = AtmosphericConditionsFactory.CreateStandardConditions("sahel");

            double rFrance = franceConditions.CalculateRefractionCoefficient();
            double rSahel = sahelConditions.CalculateRefractionCoefficient();

            Assert.AreEqual(0.13, rFrance, 0.01, "Coefficient France incorrect");
            Assert.AreEqual(0.057, rSahel, 0.005, "Coefficient Sahel incorrect");
        }

        [TestMethod]
        public void ValidateBaseFormulas_ShouldMatchPythonMath()
        {
            double distance = 200.0;
            var conditions = AtmosphericConditionsFactory.CreateStandardConditions("france");
            var result = _corrector!.CalculateAtmosphericCorrection(distance, 0.2, conditions);

            // Validation courbure: k × d² / (2R)
            double expectedCurvature = 1.0 * distance * distance / (2.0 * AtmosphericConditions.EARTH_RADIUS_M) * 1000;
            Assert.AreEqual(expectedCurvature, result.CurvatureCorrectionMm, 0.01, "Formule courbure incorrecte");

            // Validation réfraction: -r × d² / (2R)
            double r = conditions.CalculateRefractionCoefficient();
            double expectedRefraction = -r * distance * distance / (2.0 * AtmosphericConditions.EARTH_RADIUS_M) * 1000;
            Assert.AreEqual(expectedRefraction, result.RefractionCorrectionMm, 0.1, "Formule réfraction incorrecte");
        }

        [TestMethod]
        public void ValidateRealDataApplication_ShouldImproveAccuracy()
        {
            var realData = new List<LevelingData>
            {
                new("1") { AR1 = 0.977, AV1 = 1.997, DIST1 = 20.23 },
                new("2") { AR1 = 1.247, AV1 = 1.891, DIST1 = 20.84 },
                new("3") { AR1 = 1.840, AV1 = 2.000, DIST1 = 22.74 }
            };

            var conditions = AtmosphericConditionsFactory.CreateStandardConditions("sahel");
            var correctedData = _corrector!.ApplyCorrections(realData, conditions);

            Assert.AreEqual(realData.Count, correctedData.Count, "Nombre de points incorrect");
            
            // Vérifier que les corrections ont été appliquées
            for (int i = 0; i < realData.Count; i++)
            {
                Assert.AreNotEqual(realData[i].AV1, correctedData[i].AV1, "Corrections non appliquées");
            }
        }

        [TestMethod]
        public void ValidateDistanceScaling_ShouldFollowQuadraticLaw()
        {
            var conditions = AtmosphericConditionsFactory.CreateStandardConditions("france");
            
            var result50 = _corrector!.CalculateAtmosphericCorrection(50.0, 0.0, conditions);
            var result100 = _corrector!.CalculateAtmosphericCorrection(100.0, 0.0, conditions);

            // Validation loi quadratique: (100/50)² = 4
            double ratio = result100.TotalCorrectionMm / result50.TotalCorrectionMm;
            Assert.AreEqual(4.0, ratio, 0.1, "Loi quadratique violée");
        }
    }
}
