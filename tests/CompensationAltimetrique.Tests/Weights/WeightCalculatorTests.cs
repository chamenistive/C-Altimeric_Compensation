using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CompensationAltimetrique.Calculations.Weights;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Tests.Weights
{
    /// <summary>
    /// Tests unitaires pour le calculateur de poids géodésiques.
    /// </summary>
    public class WeightCalculatorTests
    {
        [Fact]
        public void WeightCalculator_Constructor_DefaultParameters_ShouldInitialize()
        {
            // Arrange & Act
            var calculator = new WeightCalculator();

            // Assert - vérifie que le constructeur ne lève pas d'exception
            Assert.NotNull(calculator);
        }

        [Fact]
        public void WeightCalculator_Constructor_InvalidParameters_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new WeightCalculator(-1.0, 1.0));
            Assert.Throws<ArgumentException>(() => new WeightCalculator(1.0, -1.0));
            Assert.Throws<ArgumentException>(() => new WeightCalculator(0.0, 1.0));
            Assert.Throws<ArgumentException>(() => new WeightCalculator(1.0, 0.0));
        }

        [Fact]
        public void GeodeticParameters_ToString_ShouldFormatCorrectly()
        {
            // Arrange
            var parameters = new GeodeticParameters
            {
                InstrumentalErrorMm = 1.5,
                KilometricErrorMm = 2.0,
                DefaultDistanceM = 60.0
            };

            // Act
            var result = parameters.ToString();

            // Assert
            Assert.Contains("a=1.5mm", result);
            Assert.Contains("b=2mm/km", result);
            Assert.Contains("défaut=60m", result);
        }

        [Fact]
        public void CalculateObservationWeight_StandardDistance_ShouldCalculateCorrectly()
        {
            // Arrange
            var calculator = new WeightCalculator(1.0, 1.0); // a=1mm, b=1mm/km
            double distanceM = 50.0; // 0.05 km

            // Act
            double weight = calculator.CalculateObservationWeight(distanceM);

            // Assert
            // Variance = 1² + (1 * 0.05)² = 1 + 0.0025 = 1.0025 mm²
            // Poids = 1 / 1.0025 = 0.9975
            Assert.True(Math.Abs(weight - 0.9975) < 0.001);
        }

        [Fact]
        public void CalculateObservationWeight_ZeroDistance_ShouldUseDefault()
        {
            // Arrange
            var parameters = new GeodeticParameters
            {
                InstrumentalErrorMm = 1.0,
                KilometricErrorMm = 1.0,
                DefaultDistanceM = 100.0
            };
            var calculator = new WeightCalculator(parameters);

            // Act
            double weight = calculator.CalculateObservationWeight(0.0);

            // Assert
            // Doit utiliser la distance par défaut (100m = 0.1km)
            // Variance = 1² + (1 * 0.1)² = 1.01 mm²
            double expectedWeight = 1.0 / 1.01;
            Assert.True(Math.Abs(weight - expectedWeight) < 0.001);
        }

        [Theory]
        [InlineData(10.0, 1.0001)] // 10m -> variance ≈ 1.0001
        [InlineData(100.0, 1.01)]  // 100m -> variance = 1.01
        [InlineData(1000.0, 2.0)]  // 1000m -> variance = 2.0
        public void CalculateTheoreticalVariance_DifferentDistances_ShouldCalculateCorrectly(
            double distanceM, double expectedVariance)
        {
            // Arrange
            var calculator = new WeightCalculator(1.0, 1.0);

            // Act
            double variance = calculator.CalculateTheoreticalVariance(distanceM);

            // Assert
            Assert.True(Math.Abs(variance - expectedVariance) < 0.001);
        }

        [Fact]
        public void CalculateTheoreticalStandardDeviation_ShouldReturnSqrtOfVariance()
        {
            // Arrange
            var calculator = new WeightCalculator(2.0, 1.5);
            double distanceM = 200.0; // 0.2 km

            // Act
            double variance = calculator.CalculateTheoreticalVariance(distanceM);
            double stdDev = calculator.CalculateTheoreticalStandardDeviation(distanceM);

            // Assert
            double expectedStdDev = Math.Sqrt(variance);
            Assert.True(Math.Abs(stdDev - expectedStdDev) < 1e-10);
        }

        [Fact]
        public void CalculateWeights_SingleDistance_ShouldReturnDiagonalMatrix()
        {
            // Arrange
            var calculator = new WeightCalculator(1.0, 1.0);
            var distances = new[] { 50.0 };

            // Act
            var weightMatrix = calculator.CalculateWeights(distances);

            // Assert
            Assert.Equal(1, weightMatrix.RowCount);
            Assert.Equal(1, weightMatrix.ColumnCount);
            Assert.True(weightMatrix[0, 0] > 0);
            Assert.True(weightMatrix.IsDiagonal());
        }

        [Fact]
        public void CalculateWeights_MultipleDistances_ShouldReturnCorrectMatrix()
        {
            // Arrange
            var calculator = new WeightCalculator(1.0, 1.0);
            var distances = new[] { 25.0, 50.0, 100.0 };

            // Act
            var weightMatrix = calculator.CalculateWeights(distances);

            // Assert
            Assert.Equal(3, weightMatrix.RowCount);
            Assert.Equal(3, weightMatrix.ColumnCount);
            Assert.True(weightMatrix.IsDiagonal());
            
            // Les poids doivent diminuer avec la distance
            Assert.True(weightMatrix[0, 0] > weightMatrix[1, 1]); // 25m > 50m
            Assert.True(weightMatrix[1, 1] > weightMatrix[2, 2]); // 50m > 100m
        }

        [Fact]
        public void CalculateWeights_EmptyDistances_ShouldThrow()
        {
            // Arrange
            var calculator = new WeightCalculator();
            var distances = new double[0];

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => calculator.CalculateWeights(distances));
        }

        [Fact]
        public void CalculateWeightsForLevelingData_ValidData_ShouldUseAverageDistances()
        {
            // Arrange
            var calculator = new WeightCalculator(1.0, 1.0);
            var levelingData = new List<LevelingData>
            {
                new LevelingData("P1") { DIST1 = 40.0, DIST2 = 60.0 }, // moyenne = 50m
                new LevelingData("P2") { DIST1 = 80.0 }, // 80m
                new LevelingData("P3") { DIST2 = 120.0 } // 120m
            };

            // Act
            var weightMatrix = calculator.CalculateWeightsForLevelingData(levelingData);

            // Assert
            Assert.Equal(3, weightMatrix.RowCount);
            Assert.True(weightMatrix.IsDiagonal());
            
            // Vérifier l'ordre des poids (plus courte distance = poids plus élevé)
            Assert.True(weightMatrix[0, 0] > weightMatrix[1, 1]); // 50m > 80m
            Assert.True(weightMatrix[1, 1] > weightMatrix[2, 2]); // 80m > 120m
        }

        [Fact]
        public void CalculateWeightsForLevelingData_NoDistances_ShouldUseDefault()
        {
            // Arrange
            var parameters = new GeodeticParameters
            {
                InstrumentalErrorMm = 1.0,
                KilometricErrorMm = 1.0,
                DefaultDistanceM = 75.0
            };
            var calculator = new WeightCalculator(parameters);
            
            var levelingData = new List<LevelingData>
            {
                new LevelingData("P1"), // pas de distances
                new LevelingData("P2")  // pas de distances
            };

            // Act
            var weightMatrix = calculator.CalculateWeightsForLevelingData(levelingData);

            // Assert
            Assert.Equal(2, weightMatrix.RowCount);
            
            // Les deux poids doivent être égaux (même distance par défaut)
            Assert.True(Math.Abs(weightMatrix[0, 0] - weightMatrix[1, 1]) < 1e-10);
        }

        [Fact]
        public void GetWeightStatistics_ValidMatrix_ShouldCalculateCorrectly()
        {
            // Arrange
            var calculator = new WeightCalculator(1.0, 1.0);
            var distances = new[] { 25.0, 50.0, 100.0, 200.0 };
            var weightMatrix = calculator.CalculateWeights(distances);

            // Act
            var stats = calculator.GetWeightStatistics(weightMatrix);

            // Assert
            Assert.True(stats.MinWeight > 0);
            Assert.True(stats.MaxWeight > stats.MinWeight);
            Assert.True(stats.MeanWeight > 0);
            Assert.True(stats.WeightRatio > 1);
            Assert.Equal(4, stats.TotalObservations);
            Assert.True(stats.ConditionNumber > 0);
        }

        [Fact]
        public void GetWeightStatistics_NonSymmetricMatrix_ShouldThrow()
        {
            // Arrange
            var calculator = new WeightCalculator();
            var matrix = DenseMatrix.OfArray(new[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => calculator.GetWeightStatistics(matrix));
        }

        [Fact]
        public void WeightStatistics_GetSummary_ShouldFormatCorrectly()
        {
            // Arrange
            var stats = new WeightStatistics
            {
                MinWeight = 0.1,
                MaxWeight = 1.0,
                WeightRatio = 10.0,
                TotalObservations = 5
            };

            // Act
            var summary = stats.GetSummary();

            // Assert
            Assert.Contains("min=1.00E-01", summary);
            Assert.Contains("max=1.00E+00", summary);
            Assert.Contains("ratio=10.0", summary);
            Assert.Contains("obs=5", summary);
        }

        [Fact]
        public void OptimizeParameters_ValidData_ShouldOptimizeCorrectly()
        {
            // Arrange
            var calculator = new WeightCalculator(1.0, 1.0);
            
            // Données simulées avec a=1.5, b=2.0
            var distances = new[] { 50.0, 100.0, 200.0, 500.0 };
            var residuals = new[] { 0.0015, 0.002, 0.0025, 0.004 }; // Résidus cohérents avec paramètres

            // Act
            var optimizedParams = calculator.OptimizeParameters(distances, residuals);

            // Assert
            Assert.True(optimizedParams.InstrumentalErrorMm > 0);
            Assert.True(optimizedParams.KilometricErrorMm > 0);
            Assert.Equal("optimized_geodetic", optimizedParams.WeightingMethod);
        }

        [Fact]
        public void OptimizeParameters_InconsistentData_ShouldReturnOriginal()
        {
            // Arrange
            var originalParams = new GeodeticParameters
            {
                InstrumentalErrorMm = 1.0,
                KilometricErrorMm = 1.0
            };
            var calculator = new WeightCalculator(originalParams);
            
            var distances = new[] { 50.0, 100.0 };
            var residuals = new[] { 0.001 }; // Nombre différent

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                calculator.OptimizeParameters(distances, residuals));
        }

        [Fact]
        public void ValidateWeights_GoodWeights_ShouldPassValidation()
        {
            // Arrange
            var calculator = new WeightCalculator(1.0, 1.0);
            var distances = new[] { 40.0, 50.0, 60.0 };
            var weightMatrix = calculator.CalculateWeights(distances);

            // Act
            var validation = calculator.ValidateWeights(weightMatrix, distances);

            // Assert
            Assert.True(validation.IsValid);
            Assert.Contains(validation.Details.Keys, k => k == "weight_statistics");
            Assert.Contains(validation.Details.Keys, k => k == "parameters");
        }

        [Fact]
        public void ValidateWeights_ExtremeWeights_ShouldHaveWarnings()
        {
            // Arrange
            var calculator = new WeightCalculator(1.0, 10.0); // Erreur kilométrique élevée
            var distances = new[] { 10.0, 5000.0 }; // Distances très différentes
            var weightMatrix = calculator.CalculateWeights(distances);

            // Act
            var validation = calculator.ValidateWeights(weightMatrix, distances);

            // Assert
            Assert.True(validation.IsValid);
            Assert.True(validation.Warnings.Count > 0);
        }

        [Theory]
        [InlineData(1.0, 1.0, 50.0, 0.9975)] // Cas standard
        [InlineData(2.0, 1.0, 100.0, 0.9615)] // Erreur instrumentale plus élevée
        [InlineData(1.0, 2.0, 100.0, 0.7692)] // Erreur kilométrique plus élevée
        public void CalculateObservationWeight_TheoryValidation(
            double instrumentalError, double kilometricError, double distance, double expectedWeight)
        {
            // Arrange
            var calculator = new WeightCalculator(instrumentalError, kilometricError);

            // Act
            double actualWeight = calculator.CalculateObservationWeight(distance);

            // Assert
            Assert.True(Math.Abs(actualWeight - expectedWeight) < 0.01, 
                $"Attendu: {expectedWeight}, Actuel: {actualWeight}");
        }

        [Fact]
        public void WeightMatrixBuilder_BuildMultiSessionWeights_ShouldHandleMultipleSessions()
        {
            // Arrange
            var calculator = new WeightCalculator(1.0, 1.0);
            var builder = new WeightMatrixBuilder(calculator);
            
            var levelingData = new List<LevelingData>
            {
                new LevelingData("P1") 
                { 
                    AR1 = 1.500, AV1 = 1.400, DIST1 = 50.0,
                    AR2 = 1.502, AV2 = 1.401, DIST2 = 55.0
                }
            };

            // Act
            var weightMatrix = builder.BuildMultiSessionWeights(levelingData);

            // Assert
            Assert.Equal(2, weightMatrix.RowCount); // Deux sessions
            Assert.True(weightMatrix.IsDiagonal());
            Assert.True(weightMatrix[0, 0] > 0);
            Assert.True(weightMatrix[1, 1] > 0);
        }

        [Fact]
        public void WeightMatrixBuilder_BuildCorrelatedWeights_WithoutCorrelation_ShouldBeDiagonal()
        {
            // Arrange
            var calculator = new WeightCalculator(1.0, 1.0);
            var builder = new WeightMatrixBuilder(calculator);
            var distances = new[] { 50.0, 100.0 };

            // Act
            var weightMatrix = builder.BuildCorrelatedWeights(distances, 0.0);

            // Assert
            Assert.True(weightMatrix.IsDiagonal());
            Assert.Equal(0.0, weightMatrix[0, 1], 10);
            Assert.Equal(0.0, weightMatrix[1, 0], 10);
        }

        [Fact]
        public void WeightMatrixBuilder_BuildCorrelatedWeights_WithCorrelation_ShouldHaveOffDiagonal()
        {
            // Arrange
            var calculator = new WeightCalculator(1.0, 1.0);
            var builder = new WeightMatrixBuilder(calculator);
            var distances = new[] { 50.0, 100.0 };

            // Act
            var weightMatrix = builder.BuildCorrelatedWeights(distances, 0.5);

            // Assert
            Assert.False(weightMatrix.IsDiagonal());
            Assert.True(Math.Abs(weightMatrix[0, 1]) > 1e-6);
            Assert.Equal(weightMatrix[0, 1], weightMatrix[1, 0], 10);
        }

        [Fact]
        public void WeightMatrixBuilder_Constructor_NullCalculator_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WeightMatrixBuilder(null));
        }
    }
}