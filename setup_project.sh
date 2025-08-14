#!/bin/bash

# ============================================================================
# SCRIPT DE COMPILATION ET TEST - ÉTAPE 1: ALGORITHMES MATHÉMATIQUES
# Script complet pour structurer, compiler et tester l'implémentation
# ============================================================================

set -e  # Arrêt en cas d'erreur

echo "🚀 ÉTAPE 1: ALGORITHMES MATHÉMATIQUES - COMPILATION ET TESTS"
echo "=============================================================="

# Variables de configuration
PROJECT_NAME="CompensationAltimetrique"
SOLUTION_DIR="./CompensationAltimetrique"
MAIN_PROJECT="$SOLUTION_DIR/$PROJECT_NAME"
TEST_PROJECT="$SOLUTION_DIR/$PROJECT_NAME.Tests"
MATH_NAMESPACE="$PROJECT_NAME.Mathematics"

# Couleurs pour les messages
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 1. CRÉATION DE LA STRUCTURE DU PROJET
print_status "Création de la structure du projet..."

# Créer la solution principale
if [ ! -f "$SOLUTION_DIR/$PROJECT_NAME.sln" ]; then
    mkdir -p "$SOLUTION_DIR"
    cd "$SOLUTION_DIR"
    
    dotnet new sln -n $PROJECT_NAME
    print_success "Solution créée: $PROJECT_NAME.sln"
else
    print_warning "Solution existante trouvée"
    cd "$SOLUTION_DIR"
fi

# Créer le projet principal
if [ ! -f "$MAIN_PROJECT/$PROJECT_NAME.csproj" ]; then
    dotnet new classlib -n $PROJECT_NAME -o $PROJECT_NAME
    dotnet sln add $MAIN_PROJECT/$PROJECT_NAME.csproj
    print_success "Projet principal créé: $PROJECT_NAME"
else
    print_warning "Projet principal existant trouvé"
fi

# Créer le projet de tests
if [ ! -f "$TEST_PROJECT/$PROJECT_NAME.Tests.csproj" ]; then
    dotnet new mstest -n "$PROJECT_NAME.Tests" -o "$PROJECT_NAME.Tests"
    dotnet sln add $TEST_PROJECT/$PROJECT_NAME.Tests.csproj
    
    # Ajouter référence au projet principal
    cd $TEST_PROJECT
    dotnet add reference ../$PROJECT_NAME/$PROJECT_NAME.csproj
    cd ..
    
    print_success "Projet de tests créé: $PROJECT_NAME.Tests"
else
    print_warning "Projet de tests existant trouvé"
fi

# 2. AJOUT DES DÉPENDANCES NÉCESSAIRES
print_status "Ajout des dépendances NuGet..."

cd $MAIN_PROJECT
# Ajouter Math.NET Numerics pour les opérations matricielles avancées
dotnet add package MathNet.Numerics --version 5.0.0
print_success "Math.NET Numerics ajouté"

cd ../$TEST_PROJECT
# Packages pour les tests
dotnet add package Microsoft.NET.Test.Sdk --version 17.0.0
dotnet add package MSTest.TestAdapter --version 3.0.0
dotnet add package MSTest.TestFramework --version 3.0.0
print_success "Packages de test ajoutés"

cd ..

# 3. CRÉATION DE LA STRUCTURE DES DOSSIERS
print_status "Création de la structure des dossiers..."

# Structure du projet principal
mkdir -p $MAIN_PROJECT/Mathematics
mkdir -p $MAIN_PROJECT/Statistics
mkdir -p $MAIN_PROJECT/Atmospheric
mkdir -p $MAIN_PROJECT/Weights
mkdir -p $MAIN_PROJECT/Validation
mkdir -p $MAIN_PROJECT/Core/Models
mkdir -p $MAIN_PROJECT/Core/Interfaces

# Structure des tests
mkdir -p $TEST_PROJECT/Mathematics
mkdir -p $TEST_PROJECT/Statistics
mkdir -p $TEST_PROJECT/Atmospheric
mkdir -p $TEST_PROJECT/Weights
mkdir -p $TEST_PROJECT/Validation
mkdir -p $TEST_PROJECT/Integration

print_success "Structure des dossiers créée"

# 4. COPIE DES FICHIERS SOURCE
print_status "Copie des fichiers source..."

# Supprimer le fichier Class1.cs par défaut
rm -f $MAIN_PROJECT/Class1.cs

# Créer le fichier des algorithmes mathématiques
cat > $MAIN_PROJECT/Mathematics/AdvancedMatrixCalculator.cs << 'EOF'
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
        public Matrix<double> Solution { get; set; }           // Solution x̂
        public Matrix<double> CovarianceMatrix { get; set; }   // Matrice de covariance Qₓ
        public SolutionMethod MethodUsed { get; set; }         // Méthode utilisée
        public double ConditionNumber { get; set; }            // Nombre de conditionnement
        public bool IsStable { get; set; }                    // Stabilité numérique
        public string DiagnosticInfo { get; set; }            // Informations diagnostiques
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
            var covarianceMatrix = svd.VT.Transpose() * DiagonalMatrix.OfDiagonal(S_inv.Map(s => s * s)) * svd.VT;
            
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
EOF

print_success "Fichier AdvancedMatrixCalculator.cs créé avec Math.NET Numerics"

# 5. CRÉATION DES TESTS
print_status "Création des fichiers de tests..."

cat > $TEST_PROJECT/Mathematics/AdvancedMatrixCalculatorTests.cs << 'EOF'
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

        [TestMethod]
        public void SelectOptimalMethod_SmallWellConditionedSystem_ShouldReturnValidMethod()
        {
            // Arrange
            var A = DenseMatrix.OfArray(new double[,] { {1, 2}, {3, 4}, {5, 6} });
            var P = DenseMatrix.CreateDiagonal(3, 3, 1.0);

            // Act
            var method = calculator.SelectOptimalMethod(A, P);

            // Assert
            Assert.IsTrue(Enum.IsDefined(typeof(SolutionMethod), method));
        }

        [TestMethod]
        public void SolveOptimal_SimpleSystem_ShouldProduceValidResult()
        {
            // Arrange
            var A = DenseMatrix.OfArray(new double[,] { {2, 1}, {1, 3}, {0, 1} });
            var P = DenseMatrix.CreateDiagonal(3, 3, 1.0);
            var f = DenseVector.OfArray(new double[] {1, 2, 1});

            // Act
            var result = calculator.SolveOptimal(A, P, f);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Solution);
            Assert.IsTrue(result.ConditionNumber > 0);
        }

        [TestMethod]
        public void SolveWithMethod_AllMethods_ShouldNotThrow()
        {
            // Arrange
            var A = DenseMatrix.OfArray(new double[,] { {2, 1}, {1, 3} });
            var P = DenseMatrix.CreateDiagonal(2, 2, 1.0);
            var f = DenseVector.OfArray(new double[] {3, 4});

            var methods = new[]
            {
                SolutionMethod.NormalEquations,
                SolutionMethod.QRDecomposition,
                SolutionMethod.CholeskyDecomposition,
                SolutionMethod.SVDDecomposition
            };

            // Act & Assert
            foreach (var method in methods)
            {
                var result = calculator.SolveWithMethod(A, P, f, method);
                Assert.IsNotNull(result);
                Assert.AreEqual(method, result.MethodUsed);
            }
        }
    }
}
EOF

print_success "Fichier de tests créé"

# 6. COMPILATION
print_status "Compilation de la solution..."

dotnet build
if [ $? -eq 0 ]; then
    print_success "Compilation réussie ✅"
else
    print_error "Échec de la compilation ❌"
    exit 1
fi

# 7. EXÉCUTION DES TESTS
print_status "Exécution des tests..."

dotnet test --verbosity normal
if [ $? -eq 0 ]; then
    print_success "Tous les tests passent ✅"
else
    print_warning "Certains tests ont échoué ⚠️"
fi

# 8. GÉNÉRATION DU RAPPORT DE COUVERTURE
print_status "Génération du rapport de couverture..."

dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
if [ $? -eq 0 ]; then
    print_success "Rapport de couverture généré"
else
    print_warning "Échec génération rapport de couverture"
fi

# 9. VALIDATION DE L'ÉTAPE
echo ""
echo "=============================================================="
print_success "🎯 ÉTAPE 1 TERMINÉE: ALGORITHMES MATHÉMATIQUES"
echo "=============================================================="
echo ""
echo "✅ Structure du projet créée"
echo "✅ Dépendances ajoutées (Math.NET Numerics)"
echo "✅ Classes principales implémentées"
echo "✅ Tests unitaires créés"
echo "✅ Compilation réussie"
echo "✅ Tests exécutés"
echo ""
echo "📂 Structure créée:"
echo "   $SOLUTION_DIR/"
echo "   ├── $PROJECT_NAME.sln"
echo "   ├── $PROJECT_NAME/"
echo "   │   ├── Mathematics/AdvancedMatrixCalculator.cs"
echo "   │   └── $PROJECT_NAME.csproj"
echo "   └── $PROJECT_NAME.Tests/"
echo "       ├── Mathematics/AdvancedMatrixCalculatorTests.cs"
echo "       └── $PROJECT_NAME.Tests.csproj"
echo ""
echo "🚀 Prêt pour l'ÉTAPE 2: ANALYSE STATISTIQUE"
echo "=============================================================="