// ================================================================
// TEST SIMPLE - ÉTAPE 4 - VALIDATION ORCHESTRATION
// Test simple pour vérifier le bon fonctionnement de l'orchestrateur
// ================================================================

using System;
using System.Collections.Generic;
using Xunit;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Calculations.Design;
using CompensationAltimetrique.Calculations.Weights;

namespace CompensationAltimetrique.Tests.Design
{
    /// <summary>
    /// Test simple pour valider l'orchestration complète.
    /// </summary>
    public class SimpleOrchestrationTest
    {
        [Fact]
        public void SimpleOrchestration_BasicConfiguration_ShouldSucceed()
        {
            // Arrange
            var config = new CompensationConfiguration
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
                ApplyAtmosphericCorrections = false
            };

            var orchestrator = new LeastSquaresOrchestrator(config);

            var levelingData = new List<LevelingData>
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

            // Act
            var results = orchestrator.CompensateNetwork(levelingData);

            // Assert
            Assert.NotNull(results);
            Assert.True(results.AdjustedPoints.Count >= 3); // REF + P1 + P2
            Assert.NotNull(results.DesignMatrix);
            Assert.NotNull(results.WeightMatrix);
            Assert.True(results.ComputationTime.TotalMilliseconds > 0);
        }

        [Fact]
        public void DesignMatrixBuilder_BasicConstruction_ShouldWork()
        {
            // Arrange
            var config = NetworkConfigurationFactory.CreateClosedTraverse(
                "REF", 100.000, new[] { "REF", "P1", "P2" });
            var builder = new DesignMatrixBuilder(config);

            var levelingData = new List<LevelingData>
            {
                new LevelingData
                {
                    Matricule = "P1",
                    AR1 = 1000.0,
                    AV1 = 1001.500,
                    DIST1 = 50.0
                }
            };

            // Act
            var (matrix, info) = builder.BuildDesignMatrix(levelingData, includeClosureConstraint: false);

            // Assert
            Assert.NotNull(matrix);
            Assert.NotNull(info);
            Assert.True(matrix.RowCount > 0);
            Assert.True(matrix.ColumnCount > 0);
            Assert.True(info.ObservationCount > 0);
            Assert.True(info.UnknownCount > 0);
        }
    }
}