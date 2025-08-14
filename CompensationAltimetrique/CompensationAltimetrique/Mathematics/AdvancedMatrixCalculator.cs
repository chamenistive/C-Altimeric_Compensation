// ============================================================================
// ÉTAPE 1: ALGORITHMES MATHÉMATIQUES
// Implémentation des méthodes de résolution robustes pour compensation altimétrique
// ============================================================================

using System;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CompensationAltimetrique.Mathematics
{
    /// <summary>
    /// Énumération des méthodes de résolution disponibles
    /// </summary>
    public enum SolutionMethod
    {
        NormalEquations,     // Équations normales classiques
        QRDecomposition,     // Décomposition QR (stable)
        CholeskyDecomposition, // Décomposition de Cholesky (optimisé)
        SVDDecomposition     // SVD (ultra-robuste)
    }

    /// <summary>
    /// Résultat d'une résolution de système linéaire
    /// </summary>
    public class LinearSolutionResult
    {
        public Matrix<double> Solution { get; set; } = null!;           // Solution x̂
        public Matrix<double> CovarianceMatrix { get; set; } = null!;   // Matrice de covariance Qₓ
        public SolutionMethod MethodUsed { get; set; }         // Méthode utilisée
        public double ConditionNumber { get; set; }            // Nombre de conditionnement
        public bool IsStable { get; set; }                    // Stabilité numérique
        public string DiagnosticInfo { get; set; } = string.Empty;            // Informations diagnostiques
    }

    /// <summary>
    /// Calculateur matriciel avancé pour la compensation altimétrique
    /// Utilise Math.NET Numerics pour les opérations optimisées
    /// </summary>
    public class AdvancedMatrixCalculator
    {
        private const double CONDITION_NUMBER_THRESHOLD = 1e12;
        private const int LARGE_SYSTEM_THRESHOLD = 1000;
        private const double REGULARIZATION_FACTOR = 1e-12;

        /// <summary>
        /// Résout le système Ax = b par la méthode optimale automatiquement sélectionnée
        /// </summary>
        public LinearSolutionResult SolveOptimal(Matrix<double> A, Matrix<double> P, Vector<double> f)
        {
            var optimalMethod = SelectOptimalMethod(A, P);
            Console.WriteLine($"🔧 Méthode sélectionnée: {optimalMethod}");
            return SolveWithMethod(A, P, f, optimalMethod);
        }

        /// <summary>
        /// Sélectionne automatiquement la méthode optimale
        /// </summary>
        public SolutionMethod SelectOptimalMethod(Matrix<double> A, Matrix<double> P)
        {
            int rows = A.RowCount;
            int cols = A.ColumnCount;
            
            double conditionNumber = A.ConditionNumber();
            Console.WriteLine($"📊 Système: {rows}×{cols}, Conditionnement: {conditionNumber:E2}");
            
            if (conditionNumber > CONDITION_NUMBER_THRESHOLD)
            {
                Console.WriteLine("⚠️ Système mal conditionné → SVD");
                return SolutionMethod.SVDDecomposition;
            }
            else if (cols > LARGE_SYSTEM_THRESHOLD)
            {
                Console.WriteLine("📈 Gros système → QR Décomposition");
                return SolutionMethod.QRDecomposition;
            }
            else if (IsPositiveDefinite(A, P))
            {
                Console.WriteLine("✅ Système bien conditionné → Cholesky");
                return SolutionMethod.CholeskyDecomposition;
            }
            else
            {
                Console.WriteLine("⚙️ Système standard → Équations normales");
                return SolutionMethod.NormalEquations;
            }
        }

        /// <summary>
        /// Résout le système avec une méthode spécifique
        /// </summary>
        public LinearSolutionResult SolveWithMethod(Matrix<double> A, Matrix<double> P, Vector<double> f, SolutionMethod method)
        {
            try
            {
                switch (method)
                {
                    case SolutionMethod.NormalEquations:
                        return SolveNormalEquations(A, P, f);
                    case SolutionMethod.QRDecomposition:
                        return SolveQRDecomposition(A, P, f);
                    case SolutionMethod.CholeskyDecomposition:
                        return SolveCholeskyDecomposition(A, P, f);
                    case SolutionMethod.SVDDecomposition:
                        return SolveSVDDecomposition(A, P, f);
                    default:
                        throw new ArgumentException($"Méthode non supportée: {method}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Échec méthode {method}: {ex.Message}");
                if (method != SolutionMethod.SVDDecomposition)
                {
                    Console.WriteLine("🔄 Basculement vers SVD...");
                    return SolveSVDDecomposition(A, P, f);
                }
                throw;
            }
        }

        /// <summary>
        /// Résolution par équations normales avec régularisation
        /// </summary>
        private LinearSolutionResult SolveNormalEquations(Matrix<double> A, Matrix<double> P, Vector<double> f)
        {
            var N = A.Transpose() * P * A;  // Matrice normale
            var b = A.Transpose() * P * f;  // Second membre
            
            double conditionNumber = N.ConditionNumber();
            
            // Régularisation si nécessaire
            if (conditionNumber > CONDITION_NUMBER_THRESHOLD)
            {
                Console.WriteLine($"⚠️ Régularisation appliquée (cond={conditionNumber:E2})");
                var regularization = N.Trace() * REGULARIZATION_FACTOR;
                N += DenseMatrix.CreateDiagonal(N.RowCount, N.ColumnCount, regularization);
            }
            
            var solution = N.Solve(b.ToColumnMatrix());
            var covarianceMatrix = N.Inverse();
            
            return new LinearSolutionResult
            {
                Solution = solution,
                CovarianceMatrix = covarianceMatrix,
                MethodUsed = SolutionMethod.NormalEquations,
                ConditionNumber = conditionNumber,
                IsStable = conditionNumber < CONDITION_NUMBER_THRESHOLD,
                DiagnosticInfo = $"Équations normales, cond={conditionNumber:E2}"
            };
        }

        /// <summary>
        /// Résolution par décomposition QR
        /// </summary>
        private LinearSolutionResult SolveQRDecomposition(Matrix<double> A, Matrix<double> P, Vector<double> f)
        {
            // Application de la pondération
            var sqrtP = P.PointwiseSqrt();
            var A_weighted = sqrtP * A;
            var f_weighted = sqrtP * f;
            
            // Décomposition QR
            var qr = A_weighted.QR();
            var solution = qr.Solve(f_weighted.ToColumnMatrix());
            
            // Matrice de covariance via R
            var R = qr.R;
            var R_inv = R.Inverse();
            var covarianceMatrix = R_inv.Transpose() * R_inv;
            
            return new LinearSolutionResult
            {
                Solution = solution,
                CovarianceMatrix = covarianceMatrix,
                MethodUsed = SolutionMethod.QRDecomposition,
                ConditionNumber = R.ConditionNumber(),
                IsStable = true,
                DiagnosticInfo = $"Décomposition QR"
            };
        }

        /// <summary>
        /// Résolution par décomposition de Cholesky
        /// </summary>
        private LinearSolutionResult SolveCholeskyDecomposition(Matrix<double> A, Matrix<double> P, Vector<double> f)
        {
            var N = A.Transpose() * P * A;
            var b = A.Transpose() * P * f;
            
            var chol = N.Cholesky();
            var solution = chol.Solve(b.ToColumnMatrix());
            var covarianceMatrix = N.Inverse();
            
            return new LinearSolutionResult
            {
                Solution = solution,
                CovarianceMatrix = covarianceMatrix,
                MethodUsed = SolutionMethod.CholeskyDecomposition,
                ConditionNumber = N.ConditionNumber(),
                IsStable = true,
                DiagnosticInfo = "Décomposition Cholesky"
            };
        }

        /// <summary>
        /// Résolution par décomposition SVD
        /// </summary>
        private LinearSolutionResult SolveSVDDecomposition(Matrix<double> A, Matrix<double> P, Vector<double> f)
        {
            var sqrtP = P.PointwiseSqrt();
            var A_weighted = sqrtP * A;
            var f_weighted = sqrtP * f;
            
            var svd = A_weighted.Svd();
            var solution = svd.Solve(f_weighted.ToColumnMatrix());
            
            // Pseudo-inverse pour la covariance
            var tolerance = 1e-12 * svd.S.Maximum();
            var S_inv = svd.S.Map(s => s > tolerance ? 1.0 / s : 0.0);
            var covarianceMatrix = svd.VT.Transpose() * DiagonalMatrix.OfDiagonal(S_inv.Count, S_inv.Count, S_inv.Map(s => s * s)) * svd.VT;
            
            double conditionNumber = svd.S.Maximum() / svd.S.Where(s => s > tolerance).Min();
            
            return new LinearSolutionResult
            {
                Solution = solution,
                CovarianceMatrix = covarianceMatrix,
                MethodUsed = SolutionMethod.SVDDecomposition,
                ConditionNumber = conditionNumber,
                IsStable = true,
                DiagnosticInfo = $"Décomposition SVD, seuil={tolerance:E2}"
            };
        }

        /// <summary>
        /// Vérifie si le système sera défini positif
        /// </summary>
        private bool IsPositiveDefinite(Matrix<double> A, Matrix<double> P)
        {
            try
            {
                var N = A.Transpose() * P * A;
                var chol = N.Cholesky();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}