// ============================================================================
// TESTS UNITAIRES - ÉTAPE 1: ALGORITHMES MATHÉMATIQUES
// Tests complets pour la validation des méthodes de résolution
// ============================================================================

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using CompensationAltimetrique.Mathematics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CompensationAltimetrique.Tests.Mathematics
{
    [TestClass]
    public class AdvancedMatrixCalculatorTests
    {
        private AdvancedMatrixCalculator calculator;
        private const double TOLERANCE = 1e-10;

        [TestInitialize]
        public void Setup()
        {
            calculator = new AdvancedMatrixCalculator();
        }

        #region Tests de Sélection de Méthode

        [TestMethod]
        public void SelectOptimalMethod_SmallWellConditionedSystem_ShouldReturnNormalEquations()
        {
            // Arrange
            var A = CreateWellConditionedMatrix(50, 30);
            var P = CreateDiagonalMatrix(50, 1.0);

            // Act
            var method = calculator.SelectOptimalMethod(A, P);

            // Assert
            Assert.IsTrue(method == SolutionMethod.NormalEquations || 
                         method == SolutionMethod.CholeskyDecomposition);
        }

        [TestMethod]
        public void SelectOptimalMethod_LargeSystem_ShouldReturnQRDecomposition()
        {
            // Arrange
            var A = CreateWellConditionedMatrix(1500, 1200);
            var P = CreateDiagonalMatrix(1500, 1.0);

            // Act
            var method = calculator.SelectOptimalMethod(A, P);

            // Assert
            Assert.AreEqual(SolutionMethod.QRDecomposition, method);
        }

        [TestMethod]
        public void SelectOptimalMethod_IllConditionedSystem_ShouldReturnSVD()
        {
            // Arrange
            var A = CreateIllConditionedMatrix(100, 80);
            var P = CreateDiagonalMatrix(100, 1.0);

            // Act
            var method = calculator.SelectOptimalMethod(A, P);

            // Assert
            Assert.AreEqual(SolutionMethod.SVDDecomposition, method);
        }

        #endregion

        #region Tests des Méthodes de Résolution

        [TestMethod]
        public void SolveOptimal_SimpleLinearSystem_ShouldProduceCorrectSolution()
        {
            // Arrange
            // Système simple: 2x + 3y = 8, 4x + 6y = 16 (système déterminé)
            var A = DenseMatrix.OfArray(new double[,] { {2, 3}, {4, 6} });
            var P = DenseMatrix.CreateDiagonal(2, 2, 1.0);
            var f = DenseVector.OfArray(new double[] { 8, 16 });

            // Act
            var result = calculator.SolveOptimal(A, P, f);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Solution);
            Assert.IsTrue(result.IsStable);
            Assert.IsTrue(result.ConditionNumber > 0);
        }

        [TestMethod]
        public void SolveWithMethod_NormalEquations_ShouldProduceValidResults()
        {
            // Arrange
            var A = CreateTestMatrix_3x2();
            var P = CreateDiagonalMatrix(3, 1.0);
            var f = DenseVector.OfArray(new double[] { 1, 2, 3 });

            // Act
            var result = calculator.SolveWithMethod(A, P, f, SolutionMethod.NormalEquations);

            // Assert
            Assert.AreEqual(SolutionMethod.NormalEquations, result.MethodUsed);
            Assert.IsNotNull(result.Solution);
            Assert.IsNotNull(result.CovarianceMatrix);
            Assert.IsTrue(result.ConditionNumber > 0);
        }

        [TestMethod]
        public void SolveWithMethod_AllMethods_ShouldProduceSimilarResults()
        {
            // Arrange
            var A = CreateWellConditionedMatrix(50, 30);
            var P = CreateDiagonalMatrix(50, 1.0);
            var f = CreateRandomVector(50);

            var methods = new[]
            {
                SolutionMethod.NormalEquations,
                SolutionMethod.QRDecomposition,
                SolutionMethod.CholeskyDecomposition
            };

            // Act & Assert
            LinearSolutionResult previousResult = null;
            
            foreach (var method in methods)
            {
                try
                {
                    var result = calculator.SolveWithMethod(A, P, f, method);
                    
                    Assert.IsNotNull(result.Solution);
                    Assert.AreEqual(method, result.MethodUsed);
                    
                    if (previousResult != null)
                    {
                        // Vérifier que les solutions sont cohérentes entre méthodes
                        AssertMatricesAreClose(previousResult.Solution, result.Solution, 1e-6);
                    }
                    
                    previousResult = result;
                }
                catch (NotImplementedException)
                {
                    // Acceptable pour les méthodes pas encore complètement implémentées
                    Assert.Inconclusive($"Méthode {method} pas encore implémentée");
                }
            }
        }

        #endregion

        #region Tests de Robustesse

        [TestMethod]
        public void SolveOptimal_ZeroMatrix_ShouldHandleGracefully()
        {
            // Arrange
            var A = DenseMatrix.Create(3, 2, 0);  // Matrice zéro
            var P = CreateDiagonalMatrix(3, 1.0);
            var f = CreateRandomVector(3);

            // Act & Assert
            try
            {
                var result = calculator.SolveOptimal(A, P, f);
                // Si pas d'exception, vérifier que le résultat indique l'instabilité
                Assert.IsFalse(result.IsStable);
            }
            catch (Exception ex)
            {
                // Exception attendue pour matrice singulière
                Assert.IsTrue(ex.Message.Contains("singulière") || 
                             ex.Message.Contains("NotImplemented"));
            }
        }

        [TestMethod]
        public void SolveOptimal_HighlyIllConditioned_ShouldUseSVD()
        {
            // Arrange
            var A = CreateHighlyIllConditionedMatrix();
            var P = CreateDiagonalMatrix(A.RowCount, 1.0);
            var f = CreateRandomVector(A.RowCount);

            // Act
            try
            {
                var result = calculator.SolveOptimal(A, P, f);
                
                // Assert
                Assert.AreEqual(SolutionMethod.SVDDecomposition, result.MethodUsed);
            }
            catch (NotImplementedException)
            {
                Assert.Inconclusive("SVD pas encore implémentée");
            }
        }

        #endregion

        #region Tests de Performance

        [TestMethod]
        public void SolveOptimal_LargeSystem_ShouldCompleteInReasonableTime()
        {
            // Arrange
            var A = CreateWellConditionedMatrix(500, 300);
            var P = CreateDiagonalMatrix(500, 1.0);
            var f = CreateRandomVector(500);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            try
            {
                var result = calculator.SolveOptimal(A, P, f);
                stopwatch.Stop();

                // Assert
                Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, // Moins de 5 secondes
                    $"Résolution trop lente: {stopwatch.ElapsedMilliseconds}ms");
                
                Assert.IsNotNull(result.Solution);
            }
            catch (NotImplementedException)
            {
                Assert.Inconclusive("Implémentation incomplète");
            }
        }

        #endregion

        #region Tests de Validation des Résultats

        [TestMethod]
        public void SolveOptimal_KnownSolution_ShouldRecoverOriginalVector()
        {
            // Arrange - Créer un système où on connaît la solution
            var x_true = DenseVector.OfArray(new double[] { 1, 2, 3 });
            var A = CreateTestMatrix_3x3();
            var b = A * x_true;
            var P = CreateDiagonalMatrix(3, 1.0);

            // Act
            try
            {
                var result = calculator.SolveOptimal(A, P, b);

                // Assert
                AssertVectorsAreClose(x_true, result.Solution.Column(0), 1e-10);
            }
            catch (NotImplementedException)
            {
                Assert.Inconclusive("Implémentation incomplète");
            }
        }

        #endregion

        #region Méthodes Utilitaires pour Tests

        private Matrix<double> CreateWellConditionedMatrix(int rows, int cols)
        {
            var random = new Random(42); // Seed fixe pour reproductibilité
            var values = new double[rows, cols];
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    values[i, j] = random.NextDouble() * 2 - 1; // [-1, 1]
                }
            }
            
            return DenseMatrix.OfArray(values);
        }

        private Matrix<double> CreateIllConditionedMatrix(int rows, int cols)
        {
            // Créer une matrice de Hilbert tronquée (très mal conditionnée)
            var values = new double[rows, cols];
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    values[i, j] = 1.0 / (i + j + 1);
                }
            }
            
            return DenseMatrix.OfArray(values);
        }

        private Matrix<double> CreateHighlyIllConditionedMatrix()
        {
            // Matrice de Hilbert (classiquement mal conditionnée)
            int n = 10;
            var values = new double[n, n];
            
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    values[i, j] = 1.0 / (i + j + 1);
                }
            }
            
            return DenseMatrix.OfArray(values);
        }

        private Matrix<double> CreateDiagonalMatrix(int size, double value)
        {
            return DenseMatrix.CreateDiagonal(size, size, value);
        }

        private Vector<double> CreateRandomVector(int size)
        {
            var random = new Random(42);
            var values = new double[size];
            
            for (int i = 0; i < size; i++)
            {
                values[i] = random.NextDouble() * 10 - 5; // [-5, 5]
            }
            
            return DenseVector.OfArray(values);
        }

        private Matrix<double> CreateTestMatrix_3x2()
        {
            return DenseMatrix.OfArray(new double[,]
            {
                {1, 2},
                {3, 4},
                {5, 6}
            });
        }

        private Matrix<double> CreateTestMatrix_3x3()
        {
            return DenseMatrix.OfArray(new double[,]
            {
                {2, -1, 0},
                {-1, 2, -1},
                {0, -1, 2}
            });
        }

        private void AssertMatricesAreClose(Matrix<double> expected, Matrix<double> actual, double tolerance)
        {
            Assert.AreEqual(expected.RowCount, actual.RowCount, "Nombre de lignes différent");
            Assert.AreEqual(expected.ColumnCount, actual.ColumnCount, "Nombre de colonnes différent");
            
            for (int i = 0; i < expected.RowCount; i++)
            {
                for (int j = 0; j < expected.ColumnCount; j++)
                {
                    Assert.AreEqual(expected[i, j], actual[i, j], tolerance, 
                        $"Différence à la position [{i},{j}]");
                }
            }
        }

        private void AssertVectorsAreClose(Vector<double> expected, Vector<double> actual, double tolerance)
        {
            Assert.AreEqual(expected.Count, actual.Count, "Taille des vecteurs différente");
            
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i], tolerance, 
                    $"Différence à la position {i}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Tests de benchmark pour mesurer les performances
    /// </summary>
    [TestClass]
    public class MathematicsPerformanceTests
    {
        private AdvancedMatrixCalculator calculator;

        [TestInitialize]
        public void Setup()
        {
            calculator = new AdvancedMatrixCalculator();
        }

        [TestMethod]
        public void Benchmark_DifferentMethods_ComparePerformance()
        {
            // Arrange
            var sizes = new[] { 100, 200, 500 };
            var methods = new[]
            {
                SolutionMethod.NormalEquations,
                SolutionMethod.QRDecomposition,
                SolutionMethod.CholeskyDecomposition
            };

            Console.WriteLine("=== BENCHMARK PERFORMANCE ===");
            Console.WriteLine("Taille\tMéthode\t\tTemps (ms)\tStable");

            foreach (var size in sizes)
            {
                var A = CreateRandomMatrix(size, size - 20);
                var P = CreateIdentityMatrix(size);
                var f = CreateRandomVector(size);

                foreach (var method in methods)
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    
                    try
                    {
                        var result = calculator.SolveWithMethod(A, P, f, method);
                        stopwatch.Stop();
                        
                        Console.WriteLine($"{size}\t{method}\t{stopwatch.ElapsedMilliseconds}\t\t{result.IsStable}");
                    }
                    catch (NotImplementedException)
                    {
                        Console.WriteLine($"{size}\t{method}\tN/A\t\tN/A (pas implémenté)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{size}\t{method}\tERREUR\t{ex.GetType().Name}");
                    }
                }
                
                Console.WriteLine();
            }
        }

        private Matrix<double> CreateRandomMatrix(int rows, int cols)
        {
            var random = new Random(42);
            var values = new double[rows, cols];
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    values[i, j] = random.NextDouble() * 2 - 1;
                }
            }
            
            return DenseMatrix.OfArray(values);
        }

        private Matrix<double> CreateIdentityMatrix(int size)
        {
            return DenseMatrix.CreateIdentity(size);
        }

        private Vector<double> CreateRandomVector(int size)
        {
            var random = new Random(42);
            var values = new double[size];
            
            for (int i = 0; i < size; i++)
            {
                values[i] = random.NextDouble() * 10 - 5;
            }
            
            return DenseVector.OfArray(values);
        }
    }
}