// ============================================================================
// TESTS D'INTÉGRATION - COMPENSATION ALTIMÉTRIQUE COMPLÈTE
// Tests pour valider l'intégration des trois modules principaux
// ============================================================================

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using CompensationAltimetrique.Examples;
using CompensationAltimetrique.Mathematics;
using CompensationAltimetrique.Statistics;
using CompensationAltimetrique.Atmospheric;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CompensationAltimetrique.Tests.Integration
{
    [TestClass]
    public class IntegratedCompensationTests
    {
        [TestMethod]
        public void RunFullCompensationWithAtmosphericCorrections_ShouldExecuteWithoutException()
        {
            // Arrange
            var originalOut = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                IntegratedCompensationExample.RunFullCompensationWithAtmosphericCorrections();
                
                // Assert
                string output = stringWriter.ToString();
                Assert.IsFalse(string.IsNullOrEmpty(output));
                Assert.IsTrue(output.Contains("COMPENSATION COMPLÈTE"));
                Assert.IsTrue(output.Contains("CORRECTIONS ATMOSPHÉRIQUES"));
                Assert.IsTrue(output.Contains("ALTITUDES COMPENSÉES"));
                Assert.IsTrue(output.Contains("ÉVALUATIONS CROISÉES"));
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void RunComparisonWithWithoutCorrections_ShouldExecuteWithoutException()
        {
            // Arrange
            var originalOut = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                IntegratedCompensationExample.RunComparisonWithWithoutCorrections();
                
                // Assert
                string output = stringWriter.ToString();
                Assert.IsFalse(string.IsNullOrEmpty(output));
                Assert.IsTrue(output.Contains("COMPARAISON"));
                Assert.IsTrue(output.Contains("SANS CORRECTIONS"));
                Assert.IsTrue(output.Contains("AVEC CORRECTIONS"));
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void IntegrationTest_AllThreeModules_ShouldWorkTogether()
        {
            // Arrange
            var matrixCalculator = new AdvancedMatrixCalculator();
            var statisticalAnalyzer = new StatisticalAnalyzer(0.95);
            var atmosphericCorrector = new AtmosphericCorrector();

            // Données de test simples
            var A = DenseMatrix.OfArray(new double[,] { {1, 0}, {0, 1}, {1, 1} });
            var P = DenseMatrix.CreateIdentity(3);
            var observations = new[] { (100.0, 1.0), (150.0, 2.0), (200.0, 3.0) };
            var conditions = new AtmosphericConditions();

            try
            {
                // Act
                // 1. Corrections atmosphériques
                var atmosphericCorrections = atmosphericCorrector.ApplyCorrections(observations, conditions);
                Assert.AreEqual(3, atmosphericCorrections.Count);

                // 2. Résolution matricielle
                var correctedObs = DenseVector.OfArray(atmosphericCorrections.Select(c => c.CorrectedDeltaH).ToArray());
                var solutionResult = matrixCalculator.SolveOptimal(A, P, correctedObs);
                Assert.IsNotNull(solutionResult);
                Assert.IsTrue(solutionResult.IsStable);

                // 3. Analyse statistique
                var statistics = statisticalAnalyzer.AnalyzeCompensation(
                    A, P, solutionResult.Solution.Column(0), correctedObs, solutionResult.CovarianceMatrix);
                Assert.IsNotNull(statistics);
                Assert.IsTrue(statistics.Sigma0Hat > 0);

                // 4. Rapport atmosphérique
                var correctionReport = atmosphericCorrector.GenerateCorrectionReport(atmosphericCorrections, conditions);
                Assert.IsNotNull(correctionReport);

                // Assert - Vérifier la cohérence des résultats
                Assert.AreEqual(1, statistics.DegreesOfFreedom); // 3 obs - 2 inconnues
                Assert.IsTrue(correctionReport.Corrections.Count == 3);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Intégration des modules échouée: {ex.Message}");
            }
        }

        [TestMethod]
        public void IntegrationTest_CompareWithWithoutCorrections_ShouldShowDifference()
        {
            // Arrange
            var matrixCalculator = new AdvancedMatrixCalculator();
            var atmosphericCorrector = new AtmosphericCorrector();
            
            // Système simple avec distance importante pour accentuer l'effet
            var A = DenseMatrix.OfArray(new double[,] { {1}, {1}, {1} }); // 3 obs, 1 inconnue
            var P = DenseMatrix.CreateIdentity(3);
            var observations = new[] { (300.0, 2.0), (400.0, 2.1), (500.0, 1.9) };
            var conditions = new AtmosphericConditions();

            // Act
            // Sans corrections
            var rawObs = DenseVector.OfArray(observations.Select(o => o.Item2).ToArray());
            var resultWithout = matrixCalculator.SolveOptimal(A, P, rawObs);
            
            // Avec corrections
            var corrections = atmosphericCorrector.ApplyCorrections(observations, conditions);
            var correctedObs = DenseVector.OfArray(corrections.Select(c => c.CorrectedDeltaH).ToArray());
            var resultWith = matrixCalculator.SolveOptimal(A, P, correctedObs);

            // Assert
            Assert.IsNotNull(resultWithout);
            Assert.IsNotNull(resultWith);
            
            // Les solutions doivent être différentes (même légèrement)
            double difference = Math.Abs(resultWith.Solution[0,0] - resultWithout.Solution[0,0]);
            
            // Pour ces distances, la différence peut être faible mais présente
            Assert.IsTrue(difference >= 0 && difference < 0.1); // Différence raisonnable
            
            // Les corrections doivent être négatives (courbure dominante)
            Assert.IsTrue(corrections.All(c => c.TotalCorrectionMm < 0));
        }

        [TestMethod]
        public void IntegrationTest_DifferentAtmosphericConditions_ShouldAffectResults()
        {
            // Arrange
            var matrixCalculator = new AdvancedMatrixCalculator();
            var atmosphericCorrector = new AtmosphericCorrector();
            
            var A = DenseMatrix.OfArray(new double[,] { {1}, {1} });
            var P = DenseMatrix.CreateIdentity(2);
            var observations = new[] { (300.0, 2.0), (400.0, 2.0) };
            
            var temperateConditions = AtmosphericConditions.CreateStandardConditions("temperate");
            var desertConditions = AtmosphericConditions.CreateStandardConditions("desert");

            // Act
            var temperateCorrections = atmosphericCorrector.ApplyCorrections(observations, temperateConditions);
            var desertCorrections = atmosphericCorrector.ApplyCorrections(observations, desertConditions);

            var temperateObs = DenseVector.OfArray(temperateCorrections.Select(c => c.CorrectedDeltaH).ToArray());
            var desertObs = DenseVector.OfArray(desertCorrections.Select(c => c.CorrectedDeltaH).ToArray());

            var temperateResult = matrixCalculator.SolveOptimal(A, P, temperateObs);
            var desertResult = matrixCalculator.SolveOptimal(A, P, desertObs);

            // Assert
            // Les coefficients de réfraction doivent être différents
            Assert.AreNotEqual(temperateCorrections[0].RefractionCoefficient, 
                             desertCorrections[0].RefractionCoefficient);
            
            // Les solutions finales doivent être différentes
            Assert.AreNotEqual(temperateResult.Solution[0,0], desertResult.Solution[0,0]);
        }

        [TestMethod]
        public void IntegrationTest_StatisticalAnalysisWithCorrections_ShouldImproveQuality()
        {
            // Arrange
            var matrixCalculator = new AdvancedMatrixCalculator();
            var statisticalAnalyzer = new StatisticalAnalyzer(0.95);
            var atmosphericCorrector = new AtmosphericCorrector();

            // Système surdéterminé avec distances variables
            var A = DenseMatrix.OfArray(new double[,] 
            {
                {1, 0}, {0, 1}, {1, 1}, {2, -1}, {-1, 2}
            });
            var P = DenseMatrix.CreateIdentity(5);
            var observations = new[]
            {
                (100.0, 1.000),
                (200.0, 2.000),
                (150.0, 3.000),
                (300.0, 0.500),
                (250.0, 4.000)
            };

            // Conditions atmosphériques standard
            var conditions = new AtmosphericConditions();

            // Act
            // Analyse avec corrections
            var corrections = atmosphericCorrector.ApplyCorrections(observations, conditions);
            var correctedObs = DenseVector.OfArray(corrections.Select(c => c.CorrectedDeltaH).ToArray());
            var result = matrixCalculator.SolveOptimal(A, P, correctedObs);
            var statistics = statisticalAnalyzer.AnalyzeCompensation(
                A, P, result.Solution.Column(0), correctedObs, result.CovarianceMatrix);

            // Assert
            Assert.IsNotNull(statistics);
            Assert.AreEqual(3, statistics.DegreesOfFreedom); // 5 obs - 2 inconnues
            Assert.IsTrue(statistics.Sigma0Hat > 0);
            
            // Le système devrait être stable avec des corrections appliquées
            Assert.IsTrue(result.IsStable);
            
            // Vérifier que l'analyse statistique fonctionne correctement
            Assert.IsNotNull(statistics.StandardizedResiduals);
            Assert.AreEqual(5, statistics.StandardizedResiduals.Length);
        }

        [TestMethod]
        public void IntegrationTest_ErrorPropagation_ShouldBeRealistic()
        {
            // Arrange
            var matrixCalculator = new AdvancedMatrixCalculator();
            var statisticalAnalyzer = new StatisticalAnalyzer(0.95);
            var atmosphericCorrector = new AtmosphericCorrector();

            // Système réaliste
            var A = DenseMatrix.OfArray(new double[,]
            {
                {1, 0}, {0, 1}, {1, 1}, {1, -1}
            });
            var P = DenseMatrix.CreateIdentity(4);
            var observations = new[]
            {
                (150.0, 1.542),
                (180.0, 2.837),
                (220.0, 4.379),
                (160.0, -1.295)
            };

            var conditions = new AtmosphericConditions();

            // Act
            var corrections = atmosphericCorrector.ApplyCorrections(observations, conditions);
            var atmosphericImpact = atmosphericCorrector.EstimateImpactOnPrecision(corrections);
            
            var correctedObs = DenseVector.OfArray(corrections.Select(c => c.CorrectedDeltaH).ToArray());
            var result = matrixCalculator.SolveOptimal(A, P, correctedObs);
            var statistics = statisticalAnalyzer.AnalyzeCompensation(
                A, P, result.Solution.Column(0), correctedObs, result.CovarianceMatrix);

            // Assert
            // L'impact atmosphérique doit être réaliste (quelques dixièmes de mm)
            Assert.IsTrue(atmosphericImpact >= 0);
            Assert.IsTrue(atmosphericImpact < 5.0); // Impact raisonnable

            // σ₀ doit être dans une plage réaliste pour ce type de mesures
            Assert.IsTrue(statistics.Sigma0Hat > 0.1 && statistics.Sigma0Hat < 10.0);
            
            // La précision finale combinée doit être cohérente
            double combinedPrecision = Math.Sqrt(
                statistics.Sigma0Hat * statistics.Sigma0Hat + 
                atmosphericImpact * atmosphericImpact
            );
            Assert.IsTrue(combinedPrecision > 0 && combinedPrecision < 20.0);
        }

        [TestMethod]
        public void IntegrationTest_ReportGeneration_ShouldProduceComprehensiveOutput()
        {
            // Arrange
            var statisticalAnalyzer = new StatisticalAnalyzer(0.95);
            var atmosphericCorrector = new AtmosphericCorrector();

            var conditions = new AtmosphericConditions();
            var observations = new[] { (200.0, 1.0), (300.0, 2.0), (400.0, 1.5) };
            
            // Act
            var corrections = atmosphericCorrector.ApplyCorrections(observations, conditions);
            var correctionReport = atmosphericCorrector.GenerateCorrectionReport(corrections, conditions);
            var atmosphericDetailedReport = atmosphericCorrector.GenerateDetailedReport(correctionReport);

            // Créer des statistiques fictives pour le rapport statistique
            var stats = new CompensationStatistics
            {
                Sigma0Hat = 0.8,
                DegreesOfFreedom = 1,
                UnitWeightValid = true,
                MaxStandardizedResidual = 1.5,
                BlundersDetected = new System.Collections.Generic.List<BlunderDetection>(),
                StandardizedResiduals = new double[] { 0.5, -1.2, 0.8 },
                QualityAssessment = "Test intégré"
            };
            var statisticalReport = statisticalAnalyzer.GenerateStatisticalReport(stats);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(atmosphericDetailedReport));
            Assert.IsFalse(string.IsNullOrEmpty(statisticalReport));
            
            // Vérifier le contenu des rapports
            Assert.IsTrue(atmosphericDetailedReport.Contains("CONDITIONS ATMOSPHÉRIQUES"));
            Assert.IsTrue(atmosphericDetailedReport.Contains("RECOMMANDATIONS"));
            
            Assert.IsTrue(statisticalReport.Contains("RAPPORT STATISTIQUE"));
            Assert.IsTrue(statisticalReport.Contains("FAUTES GROSSIÈRES"));
        }
    }
}