using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CompensationAltimetrique.Calculations.Validation;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Tests.Validation
{
    /// <summary>
    /// Tests unitaires pour les validateurs statistiques.
    /// </summary>
    public class ValidationStatistiqueTests
    {
        [Fact]
        public void ValidationResult_InitialState_ShouldBeValid()
        {
            // Arrange & Act
            var result = new ValidationResult();

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Empty(result.Warnings);
            Assert.Empty(result.Details);
        }

        [Fact]
        public void ValidationResult_AddError_ShouldSetInvalid()
        {
            // Arrange
            var result = new ValidationResult();

            // Act
            result.AddError("Test error");

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Test error", result.Errors[0]);
        }

        [Fact]
        public void ValidationResult_AddWarning_ShouldKeepValid()
        {
            // Arrange
            var result = new ValidationResult();

            // Act
            result.AddWarning("Test warning");

            // Assert
            Assert.True(result.IsValid);
            Assert.Single(result.Warnings);
            Assert.Equal("Test warning", result.Warnings[0]);
        }

        [Fact]
        public void ValidationResult_GetSummary_ShouldIncludeErrorsAndWarnings()
        {
            // Arrange
            var result = new ValidationResult();
            result.AddError("Error 1");
            result.AddWarning("Warning 1");

            // Act
            var summary = result.GetSummary();

            // Assert
            Assert.Contains("❌ VALIDATION ÉCHOUÉE", summary);
            Assert.Contains("Error 1", summary);
            Assert.Contains("Warning 1", summary);
        }

        [Fact]
        public void PrecisionValidator_ValidateClosureError_WithinTolerance_ShouldBeValid()
        {
            // Arrange
            var validator = new PrecisionValidator(2.0);
            double closureErrorM = 0.001; // 1mm
            double totalDistanceKm = 1.0;  // 1km -> tolerance = 4mm

            // Act
            var result = validator.ValidateClosureError(closureErrorM, totalDistanceKm);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(1.0, result.Details["closure_error_mm"]);
            Assert.Equal(4.0, result.Details["tolerance_mm"]);
        }

        [Fact]
        public void PrecisionValidator_ValidateClosureError_ExceedsTolerance_ShouldBeInvalid()
        {
            // Arrange
            var validator = new PrecisionValidator(2.0);
            double closureErrorM = 0.005; // 5mm
            double totalDistanceKm = 1.0;  // 1km -> tolerance = 4mm

            // Act
            var result = validator.ValidateClosureError(closureErrorM, totalDistanceKm);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Contains("5.00mm > tolérance 4.00mm", result.Errors[0]);
        }

        [Fact]
        public void PrecisionValidator_ValidateAdjustments_WithinTarget_ShouldBeValid()
        {
            // Arrange
            var validator = new PrecisionValidator(2.0);
            var adjustments = Vector<double>.Build.DenseOfArray(new[] { 0.001, -0.0015, 0.0005 });

            // Act
            var result = validator.ValidateAdjustments(adjustments);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(1.5, result.Details["max_adjustment_mm"]);
            Assert.True((bool)result.Details["precision_achieved"]);
        }

        [Fact]
        public void PrecisionValidator_ValidateAdjustments_ExceedsTarget_ShouldHaveWarning()
        {
            // Arrange
            var validator = new PrecisionValidator(2.0);
            var adjustments = Vector<double>.Build.DenseOfArray(new[] { 0.003, -0.0025, 0.001 });

            // Act
            var result = validator.ValidateAdjustments(adjustments);

            // Assert
            Assert.True(result.IsValid);
            Assert.Single(result.Warnings);
            Assert.Contains("3.00mm > objectif 2.00mm", result.Warnings[0]);
            Assert.False((bool)result.Details["precision_achieved"]);
        }

        [Fact]
        public void QualityAnalyzer_AnalyzeCompensation_ValidData_ShouldCalculateStatistics()
        {
            // Arrange
            var analyzer = new QualityAnalyzer(0.95);

            // Matrice de design simple (3 observations, 2 inconnues)
            var A = Matrix<double>.Build.DenseOfArray(new[,] {
                { 1.0, 0.0 },
                { 0.0, 1.0 },
                { 1.0, 1.0 }
            });

            var P = Matrix<double>.Build.DenseIdentity(3); // Poids unitaires
            var f = Vector<double>.Build.DenseOfArray(new[] { 0.001, 0.002, 0.003 });
            var x_hat = Vector<double>.Build.DenseOfArray(new[] { 0.001, 0.002 });
            var Qx = Matrix<double>.Build.DenseIdentity(2) * 0.001;

            // Act
            var stats = analyzer.AnalyzeCompensation(A, P, f, x_hat, Qx);

            // Assert
            Assert.True(stats.Sigma0Hat > 0);
            Assert.Equal(1, stats.DegreesOfFreedom); // 3 obs - 2 inconnues
            Assert.True(stats.Chi2TestStatistic >= 0);
            Assert.True(stats.Chi2CriticalValue > 0);
            Assert.Equal(0.95, stats.ConfidenceLevel);
        }

        [Fact]
        public void QualityAnalyzer_AnalyzeCompensation_UnderdeterminedSystem_ShouldThrow()
        {
            // Arrange
            var analyzer = new QualityAnalyzer(0.95);

            // Système sous-déterminé (2 observations, 3 inconnues)
            var A = Matrix<double>.Build.DenseOfArray(new[,] {
                { 1.0, 0.0, 1.0 },
                { 0.0, 1.0, 1.0 }
            });

            var P = Matrix<double>.Build.DenseIdentity(2);
            var f = Vector<double>.Build.DenseOfArray(new[] { 0.001, 0.002 });
            var x_hat = Vector<double>.Build.DenseOfArray(new[] { 0.001, 0.002, 0.001 });
            var Qx = Matrix<double>.Build.DenseIdentity(3) * 0.001;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                analyzer.AnalyzeCompensation(A, P, f, x_hat, Qx));
        }

        [Fact]
        public void QualityAnalyzer_DetectBlunders_NoBlunders_ShouldReturnEmpty()
        {
            // Arrange
            var analyzer = new QualityAnalyzer(0.95);
            var normalizedResiduals = Vector<double>.Build.DenseOfArray(new[] { 0.5, -0.3, 0.8, -0.2 });
            int degreesOfFreedom = 10;

            // Act
            var blunders = analyzer.DetectBlunders(normalizedResiduals, degreesOfFreedom);

            // Assert
            Assert.Empty(blunders);
        }

        [Fact]
        public void QualityAnalyzer_DetectBlunders_WithBlunders_ShouldDetect()
        {
            // Arrange
            var analyzer = new QualityAnalyzer(0.95);
            var normalizedResiduals = Vector<double>.Build.DenseOfArray(new[] { 5.0, -0.3, 0.8, -4.5 });
            int degreesOfFreedom = 10;

            // Act
            var blunders = analyzer.DetectBlunders(normalizedResiduals, degreesOfFreedom);

            // Assert
            Assert.True(blunders.Count >= 2);
            Assert.Contains(blunders, b => b.ObservationIndex == 0);
            Assert.Contains(blunders, b => b.ObservationIndex == 3);
        }

        [Fact]
        public void GeodeticValidator_ValidateDistances_GoodRange_ShouldHaveSuccess()
        {
            // Arrange
            var validator = new GeodeticValidator();
            var distances = new[] { 15.0, 25.0, 35.0, 45.0 };

            // Act
            var result = validator.ValidateDistances(distances);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains("appropriées", string.Join(" ", result.Details.Values));
        }

        [Fact]
        public void GeodeticValidator_ValidateDistances_LongDistances_ShouldHaveWarnings()
        {
            // Arrange
            var validator = new GeodeticValidator();
            var distances = new[] { 80.0, 120.0, 150.0 };

            // Act
            var result = validator.ValidateDistances(distances);

            // Assert
            Assert.True(result.IsValid);
            Assert.True(result.Warnings.Count > 0);
            Assert.Contains(result.Warnings, w => w.Contains("Distance maximale importante"));
        }

        [Fact]
        public void GeodeticValidator_ValidateDistances_ShortDistances_ShouldHaveWarnings()
        {
            // Arrange
            var validator = new GeodeticValidator();
            var distances = new[] { 2.0, 3.0, 4.0 };

            // Act
            var result = validator.ValidateDistances(distances);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("Distance minimale très courte"));
        }

        [Fact]
        public void CompensationValidator_ValidateCompensation_GoodData_ShouldBeValid()
        {
            // Arrange
            var validator = new CompensationValidator(2.0, 0.95);
            
            var residuals = Vector<double>.Build.DenseOfArray(new[] { 0.0005, -0.0003, 0.0001 });
            var adjustments = Vector<double>.Build.DenseOfArray(new[] { 0.001, -0.0008 });
            var statistics = new CompensationStatistics
            {
                Sigma0Hat = 0.0005,
                DegreesOfFreedom = 1,
                Chi2TestStatistic = 0.5,
                Chi2CriticalValue = 3.84,
                UnitWeightValid = true,
                MaxStandardizedResidual = 1.5,
                BlunderDetectionThreshold = 2.0,
                ConfidenceLevel = 0.95
            };
            double closureError = 0.001;
            double totalDistance = 1.0;
            var distances = new[] { 20.0, 30.0, 40.0 };

            // Act
            var result = validator.ValidateCompensation(residuals, adjustments, statistics, 
                closureError, totalDistance, distances);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains("COMPENSATION VALIDÉE", string.Join(" ", result.Details.Values));
        }

        [Fact]
        public void PrecisionValidator_Constructor_InvalidPrecision_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new PrecisionValidator(-1.0));
            Assert.Throws<ArgumentException>(() => new PrecisionValidator(0.0));
        }

        [Fact]
        public void PrecisionValidator_ValidateAdjustments_NullInput_ShouldBeInvalid()
        {
            // Arrange
            var validator = new PrecisionValidator(2.0);

            // Act
            var result = validator.ValidateAdjustments(null);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Aucun ajustement fourni", result.Errors[0]);
        }

        [Fact]
        public void CompensationStatistics_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var stats = new CompensationStatistics();

            // Assert
            Assert.Equal(0.95, stats.ConfidenceLevel);
            Assert.Equal(0.0, stats.Sigma0Hat);
            Assert.Equal(0, stats.DegreesOfFreedom);
            Assert.False(stats.UnitWeightValid);
        }

        [Fact]
        public void BlunderDetection_Properties_ShouldBeSettable()
        {
            // Arrange & Act
            var blunder = new BlunderDetection
            {
                ObservationIndex = 5,
                NormalizedResidual = 3.2,
                Significance = 1.6,
                Critical = 2.0
            };

            // Assert
            Assert.Equal(5, blunder.ObservationIndex);
            Assert.Equal(3.2, blunder.NormalizedResidual);
            Assert.Equal(1.6, blunder.Significance);
            Assert.Equal(2.0, blunder.Critical);
        }

        [Theory]
        [InlineData(0.001, 1.0, true)]   // 1mm pour 1km -> OK
        [InlineData(0.005, 1.0, false)]  // 5mm pour 1km -> NOK (tolérance 4mm)
        [InlineData(0.008, 4.0, true)]   // 8mm pour 4km -> OK (tolérance 8mm)
        [InlineData(0.012, 4.0, false)]  // 12mm pour 4km -> NOK (tolérance 8mm)
        public void PrecisionValidator_ValidateClosureError_Theory(double closureErrorM, 
            double totalDistanceKm, bool shouldBeValid)
        {
            // Arrange
            var validator = new PrecisionValidator(2.0);

            // Act
            var result = validator.ValidateClosureError(closureErrorM, totalDistanceKm);

            // Assert
            Assert.Equal(shouldBeValid, result.IsValid);
        }

        [Theory]
        [InlineData(new[] { 0.001, -0.0008, 0.0005 }, true)]   // Max 1mm -> OK
        [InlineData(new[] { 0.003, -0.0025, 0.001 }, false)]   // Max 3mm -> Warning
        public void PrecisionValidator_ValidateAdjustments_Theory(double[] adjustmentsArray, 
            bool shouldAchievePrecision)
        {
            // Arrange
            var validator = new PrecisionValidator(2.0);
            var adjustments = Vector<double>.Build.DenseOfArray(adjustmentsArray);

            // Act
            var result = validator.ValidateAdjustments(adjustments);

            // Assert
            Assert.Equal(shouldAchievePrecision, (bool)result.Details["precision_achieved"]);
        }
    }
}