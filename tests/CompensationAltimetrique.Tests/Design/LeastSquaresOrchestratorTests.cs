// ================================================================
// TESTS UNITAIRES - ÉTAPE 4 - ORCHESTRATEUR MOINDRES CARRÉS
// Tests complets pour LeastSquaresOrchestrator 
// Validation intégration complète et workflow compensation
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using MathNet.Numerics.LinearAlgebra;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Calculations.Design;
using CompensationAltimetrique.Calculations.Weights;
using CompensationAltimetrique.Calculations.Corrections;

namespace CompensationAltimetrique.Tests.Design
{
    /// <summary>
    /// Tests unitaires pour l'orchestrateur de compensation par moindres carrés.
    /// Validation complète du workflow d'orchestration et intégration modules.
    /// </summary>
    public class LeastSquaresOrchestratorTests
    {
        #region Tests de Configuration

        [Fact]
        public void CompensationConfiguration_ValidConfiguration_ShouldPassValidation()
        {
            // Arrange
            var config = CreateValidCompensationConfiguration();

            // Act
            var validation = config.ValidateConfiguration();

            // Assert
            Assert.True(validation.IsValid);
            Assert.Empty(validation.Errors);
        }

        [Fact]
        public void CompensationConfiguration_InvalidParameters_ShouldFailValidation()
        {
            // Arrange
            var config = new CompensationConfiguration
            {
                NetworkConfig = NetworkConfigurationFactory.CreateClosedTraverse(
                    "REF", 100.0, new[] { "REF", "P1" }),
                GeodeticParams = new GeodeticParameters
                {
                    InstrumentalErrorMm = -1.0, // Invalide: négatif
                    KilometricErrorMm = 0.0     // Invalide: zéro
                },
                TargetPrecisionMm = -2.0,       // Invalide: négatif
                ConfidenceLevel = 1.5           // Invalide: > 1
            };

            // Act
            var validation = config.ValidateConfiguration();

            // Assert
            Assert.False(validation.IsValid);
            Assert.Contains(validation.Errors, e => e.Contains("Erreur instrumentale"));
            Assert.Contains(validation.Errors, e => e.Contains("Erreur kilométrique"));
            Assert.Contains(validation.Errors, e => e.Contains("Précision cible"));
            Assert.Contains(validation.Errors, e => e.Contains("Niveau de confiance"));
        }

        [Fact]
        public void LeastSquaresOrchestrator_InvalidConfiguration_ThrowsException()
        {
            // Arrange
            var invalidConfig = new CompensationConfiguration
            {
                NetworkConfig = new NetworkConfiguration(), // Configuration vide = invalide
                GeodeticParams = new GeodeticParameters()
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                new LeastSquaresOrchestrator(invalidConfig));
            Assert.Contains("Configuration invalide", exception.Message);
        }

        #endregion

        #region Tests de Compensation Complète

        [Fact]
        public void CompensateNetwork_SimpleClosedTraverse_SuccessfulCompensation()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var levelingData = CreateSimpleTraverseData();

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            Assert.True(results.IsValid, $"Compensation devrait réussir. Erreurs: {string.Join("; ", results.ErrorMessages)}");
            Assert.True(results.AdjustedPoints.Count >= 3); // REF + P1 + P2
            Assert.NotNull(results.DesignMatrix);
            Assert.NotNull(results.WeightMatrix);
            Assert.NotNull(results.CovarianceMatrix);
            Assert.True(results.ComputationTime.TotalMilliseconds > 0);
        }

        [Fact]
        public void CompensateNetwork_EmptyData_ShouldFail()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var emptyData = new List<LevelingData>();

            // Act
            var results = orchestrator.CompensateNetwork(emptyData);

            // Assert
            Assert.False(results.IsValid);
            Assert.Contains("Aucune donnée de nivellement fournie", 
                results.ValidationResult.Errors);
        }

        [Fact]
        public void CompensateNetwork_WithRedundancy_ProducesStatistics()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var redundantData = CreateRedundantTraverseData();

            // Act
            var results = orchestrator.CompensateNetwork(redundantData);

            // Assert
            Assert.True(results.IsValid);
            Assert.True(results.MatrixInfo.IsOverdetermined);
            Assert.True(results.MatrixInfo.DegreesOfFreedom > 0);
            Assert.NotNull(results.Statistics);
            Assert.True(results.Statistics.Sigma0Hat > 0);
        }

        #endregion

        #region Tests de Matrice de Conception

        [Fact]
        public void CompensateNetwork_MatrixConstruction_CorrectDimensions()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var levelingData = CreateSimpleTraverseData();

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            var matrix = results.DesignMatrix;
            var info = results.MatrixInfo;

            Assert.Equal(3, matrix.RowCount);      // 2 observations + 1 contrainte
            Assert.Equal(3, matrix.ColumnCount);   // REF, P1, P2
            Assert.Equal(3, info.ObservationCount);
            Assert.Equal(3, info.UnknownCount);
            Assert.True(info.IsDetermined);
        }

        [Fact]
        public void CompensateNetwork_MatrixInfo_ContainsPointMapping()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var levelingData = CreateSimpleTraverseData();

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            var pointMapping = results.MatrixInfo.PointToColumnMapping;
            Assert.True(pointMapping.ContainsKey("REF"));
            Assert.True(pointMapping.ContainsKey("P1"));
            Assert.True(pointMapping.ContainsKey("P2"));
            Assert.Equal(3, pointMapping.Count);
        }

        #endregion

        #region Tests de Calcul des Poids

        [Fact]
        public void CompensateNetwork_WeightCalculation_ProducesValidWeights()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var levelingData = CreateSimpleTraverseData();

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            var weightMatrix = results.WeightMatrix;
            var weightStats = results.WeightStats;

            Assert.True(weightMatrix.IsSymmetric());
            Assert.True(weightStats.MinWeight > 0);
            Assert.True(weightStats.MaxWeight > 0);
            Assert.True(weightStats.WeightRatio >= 1.0);
            Assert.Equal(levelingData.Count, weightStats.TotalObservations);
        }

        [Fact]
        public void CompensateNetwork_DifferentDistances_ProducesVariableWeights()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var levelingData = CreateVariableDistanceData();

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            var weightStats = results.WeightStats;
            
            // Les poids doivent varier selon les distances
            Assert.True(weightStats.WeightRatio > 1.1); // Au moins 10% de variation
            Assert.True(weightStats.StdWeight > 0);      // Écart-type non nul
        }

        #endregion

        #region Tests de Résolution du Système

        [Fact]
        public void CompensateNetwork_SystemSolution_ProducesCorrections()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var levelingData = CreateSimpleTraverseData();

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            Assert.NotNull(results.Corrections);
            Assert.True(results.Corrections.Count > 0);
            Assert.True(results.MaxCorrection >= 0);
            Assert.NotNull(results.Residuals);
            Assert.True(results.RmsResiduals >= 0);
        }

        [Fact]
        public void CompensateNetwork_CovarianceMatrix_ValidStructure()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var levelingData = CreateSimpleTraverseData();

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            var covariance = results.CovarianceMatrix;
            
            Assert.True(covariance.IsSymmetric());
            Assert.Equal(results.MatrixInfo.UnknownCount, covariance.RowCount);
            Assert.Equal(results.MatrixInfo.UnknownCount, covariance.ColumnCount);
            
            // Éléments diagonaux positifs (variances)
            for (int i = 0; i < covariance.RowCount; i++)
            {
                Assert.True(covariance[i, i] > 0, $"Variance {i} devrait être positive");
            }
        }

        #endregion

        #region Tests de Calcul des Altitudes

        [Fact]
        public void CompensateNetwork_AdjustedAltitudes_CorrectValues()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var levelingData = CreateSimpleTraverseData();

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            var adjustedPoints = results.AdjustedPoints;
            
            Assert.True(adjustedPoints.Count >= 3);
            
            // Point de référence doit garder son altitude
            var refPoint = adjustedPoints.First(p => p.IsReference);
            Assert.Equal("REF", refPoint.PointId);
            Assert.Equal(100.000, refPoint.Altitude, 1e-6);
            
            // Les autres points doivent avoir des altitudes calculées
            var freePoints = adjustedPoints.Where(p => !p.IsReference).ToList();
            Assert.All(freePoints, p => Assert.True(Math.Abs(p.Altitude - 100.0) < 10.0)); // Altitudes raisonnables
        }

        [Fact]
        public void CompensateNetwork_ClosedTraverse_CalculatesClosureError()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var levelingData = CreateSimpleTraverseData();

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            Assert.True(results.ClosureError >= 0);
            Assert.True(results.TotalDistance > 0);
        }

        #endregion

        #region Tests avec Corrections Atmosphériques

        [Fact]
        public void CompensateNetwork_WithAtmosphericCorrections_AppliesCorrections()
        {
            // Arrange
            var config = CreateValidCompensationConfiguration();
            config.ApplyAtmosphericCorrections = true;
            config.AtmosphericConditions = new CompensationAltimetrique.Calculations.Corrections.AtmosphericConditions
            {
                TemperatureCelsius = 20.0,
                PressureHpa = 1013.25,
                HumidityPercent = 60.0
            };

            var orchestrator = new LeastSquaresOrchestrator(config);
            var levelingData = CreateSimpleTraverseData();

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            Assert.True(results.IsValid);
            Assert.NotEmpty(results.AtmosphericCorrections);
            Assert.Equal(levelingData.Count, results.AtmosphericCorrections.Count);
        }

        #endregion

        #region Tests de Validation

        [Fact]
        public void CompensateNetwork_ValidationResult_ContainsDetails()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var levelingData = CreateSimpleTraverseData();

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            var validation = results.ValidationResult;
            Assert.True(validation.IsValid);
            Assert.NotEmpty(validation.Details);
        }

        #endregion

        #region Tests de Rapport

        [Fact]
        public void GenerateDetailedReport_ValidResults_ProducesComprehensiveReport()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var levelingData = CreateSimpleTraverseData();
            var results = orchestrator.CompensateNetwork(levelingData);

            // Act
            var report = orchestrator.GenerateDetailedReport(results);

            // Assert
            Assert.NotNull(report);
            Assert.NotEmpty(report);
            Assert.Contains("RAPPORT DE COMPENSATION DÉTAILLÉ", report);
            Assert.Contains("CONFIGURATION:", report);
            Assert.Contains("MATRICE DE CONCEPTION:", report);
            Assert.Contains("STATISTIQUES:", report);
            Assert.Contains("VALIDATION:", report);
            Assert.Contains("ALTITUDES AJUSTÉES:", report);
        }

        #endregion

        #region Tests de Performance

        [Fact]
        public void CompensateNetwork_PerformanceTracking_RecordsExecutionTime()
        {
            // Arrange
            var orchestrator = CreateTestOrchestrator();
            var levelingData = CreateLargeTraverseData();

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            Assert.True(results.ComputationTime.TotalMilliseconds > 0);
            Assert.True(results.ComputationTime.TotalSeconds < 10); // Temps raisonnable
            Assert.Equal(levelingData.Count, results.ProcessedPoints);
        }

        #endregion

        #region Méthodes Utilitaires

        private CompensationConfiguration CreateValidCompensationConfiguration()
        {
            return new CompensationConfiguration
            {
                NetworkConfig = NetworkConfigurationFactory.CreateClosedTraverse(
                    "REF", 100.000, new[] { "REF", "P1", "P2" }),
                GeodeticParams = new GeodeticParameters
                {
                    InstrumentalErrorMm = 1.0,
                    KilometricErrorMm = 1.5
                },
                TargetPrecisionMm = 2.0,
                ConfidenceLevel = 0.95,
                MaxIterations = 10,
                ConvergenceTolerance = 1e-6,
                ApplyAtmosphericCorrections = false
            };
        }

        private LeastSquaresOrchestrator CreateTestOrchestrator()
        {
            var config = CreateValidCompensationConfiguration();
            return new LeastSquaresOrchestrator(config);
        }

        private List<LevelingData> CreateSimpleTraverseData()
        {
            return new List<LevelingData>
            {
                new LevelingData
                {
                    Matricule = "P1",
                    AR1 = 1000.0,
                    AV1 = 1001.500,
                    DIST1 = 50.0
                },
                new LevelingData
                {
                    Matricule = "P2",
                    AR1 = 1000.0,
                    AV1 = 999.200,
                    DIST1 = 75.0
                }
            };
        }

        private List<LevelingData> CreateRedundantTraverseData()
        {
            return new List<LevelingData>
            {
                new LevelingData { Matricule = "P1", AR1 = 1000.0, AV1 = 1001.500, DIST1 = 50.0 },
                new LevelingData { Matricule = "P2", AR1 = 1000.0, AV1 = 999.200, DIST1 = 75.0 },
                new LevelingData { Matricule = "P3", AR1 = 1000.0, AV1 = 1002.100, DIST1 = 60.0 },
                new LevelingData { Matricule = "P1", AR1 = 1000.0, AV1 = 1001.498, DIST1 = 52.0 }, // Observation redondante
                new LevelingData { Matricule = "P2", AR1 = 1000.0, AV1 = 999.203, DIST1 = 73.0 }  // Observation redondante
            };
        }

        private List<LevelingData> CreateVariableDistanceData()
        {
            return new List<LevelingData>
            {
                new LevelingData { Matricule = "P1", AR1 = 1000.0, AV1 = 1001.500, DIST1 = 25.0 },  // Distance courte
                new LevelingData { Matricule = "P2", AR1 = 1000.0, AV1 = 999.200, DIST1 = 200.0 }, // Distance longue
                new LevelingData { Matricule = "P3", AR1 = 1000.0, AV1 = 1002.100, DIST1 = 100.0 }  // Distance moyenne
            };
        }

        private List<LevelingData> CreateLargeTraverseData()
        {
            var data = new List<LevelingData>();
            var random = new Random(42); // Seed fixe pour reproductibilité

            for (int i = 1; i <= 10; i++)
            {
                data.Add(new LevelingData
                {
                    Matricule = $"P{i}",
                    AR1 = 1000.0,
                    AV1 = 1000.0 + (random.NextDouble() - 0.5) * 4.0, // ±2m de variation
                    DIST1 = 50.0 + random.NextDouble() * 100.0        // 50-150m de distance
                });
            }

            return data;
        }

        #endregion
    }
}