// ================================================================
// TESTS UNITAIRES - ÉTAPE 4 - MATRICE DE CONCEPTION
// Tests complets pour DesignMatrixBuilder et orchestration
// Validation théorie moindres carrés et cas géodésiques réels
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using MathNet.Numerics.LinearAlgebra;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Calculations.Design;
using CompensationAltimetrique.Calculations.Validation;

namespace CompensationAltimetrique.Tests.Design
{
    /// <summary>
    /// Tests unitaires pour le constructeur de matrice de conception.
    /// Validation complète de l'algorithme de construction selon théorie géodésique.
    /// </summary>
    public class DesignMatrixBuilderTests
    {
        #region Tests de Configuration du Réseau

        [Fact]
        public void NetworkConfiguration_ValidClosedTraverse_ShouldBeValid()
        {
            // Arrange
            var config = NetworkConfigurationFactory.CreateClosedTraverse(
                "REF", 100.000, new[] { "REF", "P1", "P2", "P3" });

            // Act
            var validation = config.ValidateConfiguration();

            // Assert
            Assert.True(validation.IsValid);
            Assert.Equal("REF", config.ReferencePoint);
            Assert.Equal(ConstraintType.FixedPoint, config.PointConstraints["REF"]);
            Assert.Equal(ConstraintType.FreePoint, config.PointConstraints["P1"]);
        }

        [Fact]
        public void NetworkConfiguration_NoReferencePoint_ShouldBeInvalid()
        {
            // Arrange
            var config = new NetworkConfiguration
            {
                ReferencePoint = "",
                ReferenceAltitude = 100.0
            };

            // Act
            var validation = config.ValidateConfiguration();

            // Assert
            Assert.False(validation.IsValid);
            Assert.Contains("Aucun point de référence défini", validation.Errors);
        }

        [Fact]
        public void NetworkConfiguration_OpenTraverse_ShouldHaveTwoFixedPoints()
        {
            // Arrange
            var config = NetworkConfigurationFactory.CreateOpenTraverse(
                "START", "END", 100.000, 105.000, new[] { "P1", "P2" });

            // Act
            var validation = config.ValidateConfiguration();

            // Assert
            Assert.True(validation.IsValid);
            Assert.Equal(ConstraintType.FixedPoint, config.PointConstraints["START"]);
            Assert.Equal(ConstraintType.FixedPoint, config.PointConstraints["END"]);
            Assert.Equal(ConstraintType.FreePoint, config.PointConstraints["P1"]);
        }

        #endregion

        #region Tests de Construction de Matrice de Conception

        [Fact]
        public void BuildDesignMatrix_SimpleClosedTraverse_CorrectDimensions()
        {
            // Arrange
            var config = NetworkConfigurationFactory.CreateClosedTraverse(
                "REF", 100.000, new[] { "REF", "P1", "P2" });
            var builder = new DesignMatrixBuilder(config);

            var levelingData = CreateTestLevelingData(new[]
            {
                ("P1", 1.500, 50.0),
                ("P2", -0.800, 75.0)
            });

            // Act
            var (matrix, info) = builder.BuildDesignMatrix(levelingData, includeClosureConstraint: true);

            // Assert
            Assert.Equal(3, matrix.RowCount); // 2 observations + 1 contrainte fermeture
            Assert.Equal(3, matrix.ColumnCount); // REF, P1, P2
            Assert.Equal(3, info.ObservationCount);
            Assert.Equal(3, info.UnknownCount);
            Assert.Equal(0, info.DegreesOfFreedom); // Système déterminé
            Assert.True(info.IsDetermined);
        }

        [Fact]
        public void BuildDesignMatrix_OverdeterminedSystem_CorrectClassification()
        {
            // Arrange
            var config = NetworkConfigurationFactory.CreateClosedTraverse(
                "REF", 100.000, new[] { "REF", "P1", "P2", "P3", "P4" });
            var builder = new DesignMatrixBuilder(config);

            var levelingData = CreateTestLevelingData(new[]
            {
                ("P1", 1.500, 50.0),
                ("P2", -0.800, 75.0),
                ("P3", 2.100, 60.0),
                ("P4", -1.200, 80.0)
            });

            // Act
            var (matrix, info) = builder.BuildDesignMatrix(levelingData, includeClosureConstraint: true);

            // Assert
            Assert.Equal(5, matrix.RowCount); // 4 observations + 1 contrainte
            Assert.Equal(5, matrix.ColumnCount); // REF, P1, P2, P3, P4
            Assert.Equal(0, info.DegreesOfFreedom); // 5 - 5 = 0 (système juste déterminé)
            Assert.True(info.IsDetermined);
        }

        [Fact]
        public void BuildDesignMatrix_MatrixCoefficients_CorrectStructure()
        {
            // Arrange
            var config = NetworkConfigurationFactory.CreateClosedTraverse(
                "REF", 100.000, new[] { "REF", "P1", "P2" });
            var builder = new DesignMatrixBuilder(config);

            var levelingData = CreateTestLevelingData(new[]
            {
                ("P1", 1.500, 50.0),
                ("P2", -0.800, 75.0)
            });

            // Act
            var (matrix, info) = builder.BuildDesignMatrix(levelingData, includeClosureConstraint: false);

            // Assert - Vérifier la structure de la matrice
            // Première observation: REF -> P1, donc [-1, +1, 0]
            Assert.Equal(-1.0, matrix[0, info.PointToColumnMapping["REF"]], 1e-10);
            Assert.Equal(1.0, matrix[0, info.PointToColumnMapping["P1"]], 1e-10);
            Assert.Equal(0.0, matrix[0, info.PointToColumnMapping["P2"]], 1e-10);

            // Deuxième observation: P1 -> P2, donc [0, -1, +1]
            Assert.Equal(0.0, matrix[1, info.PointToColumnMapping["REF"]], 1e-10);
            Assert.Equal(-1.0, matrix[1, info.PointToColumnMapping["P1"]], 1e-10);
            Assert.Equal(1.0, matrix[1, info.PointToColumnMapping["P2"]], 1e-10);
        }

        #endregion

        #region Tests de Vecteur des Misclosures

        [Fact]
        public void BuildMisclosureVector_StandardLeveling_ZeroMisclosures()
        {
            // Arrange
            var config = NetworkConfigurationFactory.CreateClosedTraverse(
                "REF", 100.000, new[] { "REF", "P1", "P2" });
            var builder = new DesignMatrixBuilder(config);

            var levelingData = CreateTestLevelingData(new[]
            {
                ("P1", 1.500, 50.0),
                ("P2", -0.800, 75.0)
            });

            var (_, info) = builder.BuildDesignMatrix(levelingData);

            // Act
            var misclosures = builder.BuildMisclosureVector(levelingData, info, includeClosureConstraint: false);

            // Assert
            Assert.Equal(2, misclosures.Count);
            Assert.All(misclosures, m => Assert.Equal(0.0, m, 1e-10));
        }

        [Fact]
        public void BuildMisclosureVector_WithClosureConstraint_IncludesClosureError()
        {
            // Arrange
            var config = NetworkConfigurationFactory.CreateClosedTraverse(
                "REF", 100.000, new[] { "REF", "P1", "P2" });
            var builder = new DesignMatrixBuilder(config);

            var levelingData = CreateTestLevelingData(new[]
            {
                ("P1", 1.500, 50.0),
                ("P2", -0.800, 75.0)
            });

            var (_, info) = builder.BuildDesignMatrix(levelingData, includeClosureConstraint: true);

            // Act
            var misclosures = builder.BuildMisclosureVector(levelingData, info, includeClosureConstraint: true);

            // Assert
            Assert.Equal(3, misclosures.Count); // 2 observations + 1 fermeture
            
            // Les deux premières sont des observations (misclosure = 0)
            Assert.Equal(0.0, misclosures[0], 1e-10);
            Assert.Equal(0.0, misclosures[1], 1e-10);
            
            // La dernière est l'erreur de fermeture (sum des DH)
            double expectedClosureError = 1.500 + (-0.800); // = 0.700
            Assert.Equal(expectedClosureError, misclosures[2], 1e-10);
        }

        #endregion

        #region Tests de Vecteur d'Observations

        [Fact]
        public void BuildObservationVector_WithReferenceAltitude_CorrectValues()
        {
            // Arrange
            var config = NetworkConfigurationFactory.CreateClosedTraverse(
                "REF", 100.000, new[] { "REF", "P1", "P2" });
            var builder = new DesignMatrixBuilder(config);

            var levelingData = CreateTestLevelingData(new[]
            {
                ("P1", 1.500, 50.0),
                ("P2", -0.800, 75.0)
            });

            // Act
            var observations = builder.BuildObservationVector(levelingData, 100.000);

            // Assert
            Assert.Equal(3, observations.Count);
            Assert.Equal(100.000, observations[0], 1e-10); // Altitude de référence
            Assert.Equal(1.500, observations[1], 1e-10);   // Première dénivelation
            Assert.Equal(-0.800, observations[2], 1e-10);  // Deuxième dénivelation
        }

        #endregion

        #region Tests de Validation et Erreurs

        [Fact]
        public void DesignMatrixBuilder_InvalidConfiguration_ThrowsException()
        {
            // Arrange
            var config = new NetworkConfiguration
            {
                ReferencePoint = "", // Configuration invalide
                ReferenceAltitude = 100.0
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => new DesignMatrixBuilder(config));
            Assert.Contains("Configuration réseau invalide", exception.Message);
        }

        [Fact]
        public void BuildDesignMatrix_EmptyLevelingData_ThrowsException()
        {
            // Arrange
            var config = NetworkConfigurationFactory.CreateClosedTraverse(
                "REF", 100.000, new[] { "REF", "P1" });
            var builder = new DesignMatrixBuilder(config);
            var emptyData = new List<LevelingData>();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                builder.BuildDesignMatrix(emptyData));
            Assert.Contains("Erreur construction matrice conception", exception.Message);
        }

        [Fact]
        public void CalculateMatrixRank_FullRankMatrix_CorrectRank()
        {
            // Arrange
            var config = NetworkConfigurationFactory.CreateClosedTraverse(
                "REF", 100.000, new[] { "REF", "P1", "P2" });
            var builder = new DesignMatrixBuilder(config);

            var levelingData = CreateTestLevelingData(new[]
            {
                ("P1", 1.500, 50.0),
                ("P2", -0.800, 75.0)
            });

            // Act
            var (matrix, info) = builder.BuildDesignMatrix(levelingData, includeClosureConstraint: false);

            // Assert
            Assert.True(info.MatrixRank >= 2); // Au moins rang 2 pour système non trivial
            Assert.True(info.MatrixRank <= Math.Min(matrix.RowCount, matrix.ColumnCount));
        }

        #endregion

        #region Tests de Cas Réels Géodésiques

        [Fact]
        public void BuildDesignMatrix_RealGeodeticCase_AccurateDimensions()
        {
            // Arrange - Cas réel: traverse fermée avec 5 points
            var config = NetworkConfigurationFactory.CreateClosedTraverse(
                "RN", 156.432, new[] { "RN", "A", "B", "C", "D" });
            var builder = new DesignMatrixBuilder(config);

            var levelingData = CreateTestLevelingData(new[]
            {
                ("A", 2.345, 125.5),
                ("B", -1.678, 89.2),
                ("C", 3.012, 156.8),
                ("D", -2.234, 203.1)
            });

            // Act
            var (matrix, info) = builder.BuildDesignMatrix(levelingData, includeClosureConstraint: true);

            // Assert
            Assert.Equal(5, matrix.RowCount); // 4 observations + 1 contrainte
            Assert.Equal(5, matrix.ColumnCount); // RN, A, B, C, D
            Assert.Equal(0, info.DegreesOfFreedom); // Système juste déterminé
            Assert.True(info.IsDetermined);
            
            // Vérifier que le point de référence est bien mappé
            Assert.True(info.PointToColumnMapping.ContainsKey("RN"));
            Assert.True(info.PointToColumnMapping.ContainsKey("A"));
            Assert.True(info.PointToColumnMapping.ContainsKey("D"));
        }

        [Fact]
        public void BuildDesignMatrix_NetworkWithRedundancy_OverdeterminedSystem()
        {
            // Arrange - Réseau avec redondance (plus d'observations que d'inconnues)
            var config = NetworkConfigurationFactory.CreateNetwork(
                "BASE", 200.000, new[] { "BASE", "P1", "P2" });
            var builder = new DesignMatrixBuilder(config);

            // 4 observations pour 3 points = surdéterminé
            var levelingData = CreateTestLevelingData(new[]
            {
                ("P1", 1.234, 100.0),
                ("P2", -2.456, 150.0),
                ("P1", 1.238, 98.5),  // Observation redondante vers P1
                ("P2", -2.451, 152.3) // Observation redondante vers P2
            });

            // Act
            var (matrix, info) = builder.BuildDesignMatrix(levelingData, includeClosureConstraint: false);

            // Assert
            Assert.Equal(4, matrix.RowCount); // 4 observations
            Assert.Equal(3, matrix.ColumnCount); // BASE, P1, P2
            Assert.Equal(1, info.DegreesOfFreedom); // 4 - 3 = 1 DDL
            Assert.True(info.IsOverdetermined);
        }

        #endregion

        #region Méthodes Utilitaires

        /// <summary>
        /// Créer des données de test pour nivellement.
        /// </summary>
        private List<LevelingData> CreateTestLevelingData((string matricule, double dh, double distance)[] observations)
        {
            var data = new List<LevelingData>();

            foreach (var (matricule, dh, distance) in observations)
            {
                var levelingData = new LevelingData
                {
                    Matricule = matricule,
                    AR1 = 1000.0, // Valeur fixe pour calcul
                    AV1 = 1000.0 + dh, // AR + dénivelation = AV
                    DIST1 = distance,
                    AR2 = null, // Session unique pour simplicité
                    AV2 = null,
                    DIST2 = null
                };

                data.Add(levelingData);
            }

            return data;
        }

        /// <summary>
        /// Créer des données avec sessions multiples.
        /// </summary>
        private LevelingData CreateMultiSessionData(string matricule, double dh1, double dh2, double dist1, double dist2)
        {
            return new LevelingData
            {
                Matricule = matricule,
                AR1 = 1000.0,
                AV1 = 1000.0 + dh1,
                DIST1 = dist1,
                AR2 = 1000.0,
                AV2 = 1000.0 + dh2,
                DIST2 = dist2
            };
        }

        #endregion

        #region Tests d'Intégration avec Factory

        [Theory]
        [InlineData("fermé")]
        [InlineData("ouvert")]
        [InlineData("maillé")]
        public void NetworkConfigurationFactory_DifferentNetworkTypes_ValidConfigurations(string networkType)
        {
            // Arrange & Act
            NetworkConfiguration config = networkType switch
            {
                "fermé" => NetworkConfigurationFactory.CreateClosedTraverse("REF", 100.0, new[] { "REF", "P1", "P2" }),
                "ouvert" => NetworkConfigurationFactory.CreateOpenTraverse("START", "END", 100.0, 105.0, new[] { "P1" }),
                "maillé" => NetworkConfigurationFactory.CreateNetwork("BASE", 200.0, new[] { "BASE", "P1", "P2", "P3" }),
                _ => throw new ArgumentException("Type de réseau inconnu")
            };

            // Assert
            var validation = config.ValidateConfiguration();
            Assert.True(validation.IsValid, $"Configuration {networkType} devrait être valide");
            Assert.Equal(networkType, config.NetworkType);
        }

        #endregion
    }
}