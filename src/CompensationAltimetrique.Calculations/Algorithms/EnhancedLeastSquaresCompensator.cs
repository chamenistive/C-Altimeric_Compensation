using System;
using System.Collections.Generic;
using System.Linq;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Calculations.Validation;

namespace CompensationAltimetrique.Calculations.Algorithms
{
    /// <summary>
    /// Méthodes de résolution disponibles
    /// </summary>
    public enum SolutionMethod
    {
        NormalEquations,
        QRDecomposition,
        Cholesky,
        Auto
    }
    
    /// <summary>
    /// Statistiques avancées de compensation
    /// </summary>
    public class AdvancedCompensationStatistics
    {
        public double SigmaPosteriori { get; set; }
        public int DegreesOfFreedom { get; set; }
        public double Chi2Statistic { get; set; }
        public double Chi2Critical { get; set; }
        public bool UnitWeightValid { get; set; }
        public double MaxStandardizedResidual { get; set; }
        public double BlunderDetectionThreshold { get; set; }
        public List<int> SuspectObservations { get; set; } = new List<int>();
        public double RMSE { get; set; }
        public double MaxCorrection { get; set; }
        public SolutionMethod MethodUsed { get; set; }
        public TimeSpan ComputationTime { get; set; }
    }
    
    /// <summary>
    /// Résultats étendus de compensation
    /// </summary>
    public class EnhancedCompensationResults
    {
        public List<double> AdjustedAltitudes { get; set; } = new List<double>();
        public double[] Corrections { get; set; } = Array.Empty<double>();
        public double[] Residuals { get; set; } = Array.Empty<double>();
        public double[] NormalizedResiduals { get; set; } = Array.Empty<double>();
        public double[,] CovarianceMatrix { get; set; } = new double[0,0];
        public AdvancedCompensationStatistics Statistics { get; set; } = new AdvancedCompensationStatistics();
        public bool IsValid { get; set; }
        public string ReferencePoint { get; set; } = string.Empty;
        public double ReferenceAltitude { get; set; }
    }
    
    /// <summary>
    /// Compensateur amélioré avec sélection automatique de méthode
    /// </summary>
    public class EnhancedLeastSquaresCompensator
    {
        private readonly double _precisionMm;
        private readonly double _instrumentalErrorMm;
        private readonly double _kilometricErrorMm;
        private readonly GeodeticStatisticalValidator _statisticalValidator;
        private readonly GeodeticPrecisionValidator _precisionValidator;
        
        public EnhancedLeastSquaresCompensator(
            double precisionMm = 2.0,
            double instrumentalErrorMm = 1.0,
            double kilometricErrorMm = 1.0)
        {
            _precisionMm = precisionMm;
            _instrumentalErrorMm = instrumentalErrorMm;
            _kilometricErrorMm = kilometricErrorMm;
            _statisticalValidator = new GeodeticStatisticalValidator(0.95);
            _precisionValidator = new GeodeticPrecisionValidator(precisionMm);
        }
        
        /// <summary>
        /// Compensation complète avec sélection automatique de méthode
        /// </summary>
        public EnhancedCompensationResults Compensate(
            List<LevelingData> levelingData, 
            string referencePoint = "REF",
            double referenceAltitude = 125.456,
            SolutionMethod method = SolutionMethod.Auto)
        {
            var startTime = DateTime.Now;
            Console.WriteLine($"🔧 Compensation avancée - Méthode: {method}");
            
            // Préparation des données
            var validData = levelingData.Where(d => d.CalculateAverageDenivelation() != 0).ToList();
            int nObs = validData.Count;
            int nPoints = nObs + 1;
            int nUnknowns = nObs; // Points libres (sauf le premier qui est fixé)
            
            Console.WriteLine($"📊 {nObs} observations, {nPoints} points, {nUnknowns} inconnues");
            
            // Construction des matrices
            var (A, P, f) = BuildMatrixSystem(validData, referenceAltitude);
            
            // Sélection automatique de la méthode si nécessaire
            if (method == SolutionMethod.Auto)
            {
                method = SelectOptimalMethod(A, P);
                Console.WriteLine($"   Méthode sélectionnée: {method}");
            }
            
            // Résolution selon la méthode choisie
            var (xHat, Qx) = SolveSystem(A, P, f, method);
            
            // Calcul des résidus
            var v = MultiplyMatrix(A, xHat);
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = f[i] - v[i]; // Résidus = observé - calculé
            }
            
            // Analyse statistique
            var statistics = CalculateStatistics(A, P, f, xHat, Qx, v, method);
            statistics.ComputationTime = DateTime.Now - startTime;
            
            // Calcul des altitudes compensées
            var adjustedAltitudes = CalculateAdjustedAltitudes(validData, xHat, referenceAltitude);
            
            // Calcul des résidus normalisés
            var normalizedResiduals = CalculateNormalizedResiduals(v, A, P, Qx, statistics.SigmaPosteriori);
            
            // Validation finale
            ValidateResults(statistics, v);
            
            return new EnhancedCompensationResults
            {
                AdjustedAltitudes = adjustedAltitudes,
                Corrections = xHat,
                Residuals = v,
                NormalizedResiduals = normalizedResiduals,
                CovarianceMatrix = Qx,
                Statistics = statistics,
                IsValid = statistics.UnitWeightValid && statistics.MaxCorrection < 1000,
                ReferencePoint = referencePoint,
                ReferenceAltitude = referenceAltitude
            };
        }
        
        /// <summary>
        /// Construction du système matriciel A×x = f avec pondération P
        /// </summary>
        private (double[,] A, double[,] P, double[] f) BuildMatrixSystem(
            List<LevelingData> data, 
            double referenceAltitude)
        {
            int n = data.Count;
            var A = new double[n, n];
            var P = new double[n, n];
            var f = new double[n];
            
            // Équations d'observation
            for (int i = 0; i < n; i++)
            {
                // Matrice de conception pour cheminement séquentiel
                A[i, i] = 1.0; // Correction sur le point courant
                
                // Calcul du poids basé sur la distance
                double distance = data[i].DIST1 ?? data[i].DIST2 ?? 100.0;
                double distanceKm = distance / 1000.0;
                double variance = Math.Pow(_instrumentalErrorMm / 1000.0, 2) + 
                                Math.Pow(_kilometricErrorMm * distanceKm / 1000.0, 2);
                P[i, i] = 1.0 / variance;
                
                // Vecteur des dénivelées observées
                f[i] = data[i].CalculateAverageDenivelation();
            }
            
            return (A, P, f);
        }
        
        /// <summary>
        /// Sélection automatique de la méthode optimale
        /// </summary>
        private SolutionMethod SelectOptimalMethod(double[,] A, double[,] P)
        {
            int n = A.GetLength(1);
            
            // Pour petits systèmes, utiliser Cholesky (plus rapide)
            if (n < 100)
                return SolutionMethod.Cholesky;
            
            // Pour systèmes moyens, équations normales
            if (n < 500)
                return SolutionMethod.NormalEquations;
            
            // Pour grands systèmes, QR (plus stable)
            return SolutionMethod.QRDecomposition;
        }
        
        /// <summary>
        /// Résolution du système selon la méthode choisie
        /// </summary>
        private (double[] xHat, double[,] Qx) SolveSystem(
            double[,] A, double[,] P, double[] f, 
            SolutionMethod method)
        {
            switch (method)
            {
                case SolutionMethod.Cholesky:
                    return SolveCholesky(A, P, f);
                    
                case SolutionMethod.QRDecomposition:
                    return SolveQR(A, P, f);
                    
                case SolutionMethod.NormalEquations:
                default:
                    return SolveNormalEquations(A, P, f);
            }
        }
        
        /// <summary>
        /// Résolution par équations normales (méthode classique)
        /// </summary>
        private (double[] xHat, double[,] Qx) SolveNormalEquations(
            double[,] A, double[,] P, double[] f)
        {
            // Formation des équations normales : N = A^T P A
            var At = TransposeMatrix(A);
            var AtP = MultiplyMatrices(At, P);
            var N = MultiplyMatrices(AtP, A);
            
            // Second membre : b = A^T P f
            var Pf = MultiplyMatrixVector(P, f);
            var b = MultiplyMatrixVector(At, Pf);
            
            // Résolution : x = N^(-1) b
            var Ninv = InvertMatrix(N);
            var xHat = MultiplyMatrixVector(Ninv, b);
            
            // Matrice de covariance
            var Qx = Ninv;
            
            return (xHat, Qx);
        }
        
        /// <summary>
        /// Résolution par décomposition de Cholesky
        /// </summary>
        private (double[] xHat, double[,] Qx) SolveCholesky(
            double[,] A, double[,] P, double[] f)
        {
            // Formation des équations normales
            var At = TransposeMatrix(A);
            var AtP = MultiplyMatrices(At, P);
            var N = MultiplyMatrices(AtP, A);
            var Pf = MultiplyMatrixVector(P, f);
            var b = MultiplyMatrixVector(At, Pf);
            
            try
            {
                // Décomposition de Cholesky : N = L L^T
                var L = CholeskyDecomposition(N);
                
                // Résolution par substitution
                var y = ForwardSubstitution(L, b);
                var xHat = BackwardSubstitution(TransposeMatrix(L), y);
                
                // Calcul de la matrice de covariance
                var Linv = InvertLowerTriangular(L);
                var Qx = MultiplyMatrices(TransposeMatrix(Linv), Linv);
                
                return (xHat, Qx);
            }
            catch
            {
                // Fallback vers équations normales si Cholesky échoue
                Console.WriteLine("⚠️ Cholesky échoué, basculement vers équations normales");
                return SolveNormalEquations(A, P, f);
            }
        }
        
        /// <summary>
        /// Résolution par décomposition QR (plus stable numériquement)
        /// </summary>
        private (double[] xHat, double[,] Qx) SolveQR(
            double[,] A, double[,] P, double[] f)
        {
            try
            {
                // Pondération : A_w = sqrt(P) A, f_w = sqrt(P) f
                var sqrtP = MatrixSquareRoot(P);
                var Aw = MultiplyMatrices(sqrtP, A);
                var fw = MultiplyMatrixVector(sqrtP, f);
                
                // Décomposition QR : A_w = Q R
                var (Q, R) = QRDecomposition(Aw);
                
                // Résolution : R x = Q^T f_w
                var Qtf = MultiplyMatrixVector(TransposeMatrix(Q), fw);
                var xHat = BackwardSubstitution(R, Qtf);
                
                // Matrice de covariance : Qx = (R^T R)^(-1)
                var Rinv = InvertUpperTriangular(R);
                var Qx = MultiplyMatrices(Rinv, TransposeMatrix(Rinv));
                
                return (xHat, Qx);
            }
            catch
            {
                // Fallback vers équations normales si QR échoue
                Console.WriteLine("⚠️ QR échoué, basculement vers équations normales");
                return SolveNormalEquations(A, P, f);
            }
        }
        
        /// <summary>
        /// Calcul des statistiques de compensation
        /// </summary>
        private AdvancedCompensationStatistics CalculateStatistics(
            double[,] A, double[,] P, double[] f,
            double[] xHat, double[,] Qx, double[] v, SolutionMethod method)
        {
            int nObs = A.GetLength(0);
            int nUnknowns = A.GetLength(1);
            int dof = nObs - nUnknowns;
            
            if (dof <= 0) dof = 1; // Éviter division par zéro
            
            // Forme quadratique v^T P v
            var Pv = MultiplyMatrixVector(P, v);
            double vtPv = 0;
            for (int i = 0; i < v.Length; i++)
            {
                vtPv += v[i] * Pv[i];
            }
            
            // Écart-type a posteriori
            double sigmaPosteriori = Math.Sqrt(vtPv / dof);
            
            // Test du χ²
            double chi2Statistic = vtPv;
            double chi2Critical = CalculateChi2Critical(dof, 0.95);
            bool unitWeightValid = chi2Statistic <= chi2Critical;
            
            // Résidus normalisés et détection de fautes
            var normalizedResiduals = CalculateNormalizedResiduals(v, A, P, Qx, sigmaPosteriori);
            double maxNormResidual = normalizedResiduals.Length > 0 ? normalizedResiduals.Max(Math.Abs) : 0;
            double blunderThreshold = CalculateTCritical(dof, 0.95);
            
            var suspectObs = new List<int>();
            for (int i = 0; i < normalizedResiduals.Length; i++)
            {
                if (Math.Abs(normalizedResiduals[i]) > blunderThreshold)
                {
                    suspectObs.Add(i);
                }
            }
            
            // RMSE et correction maximale
            double rmse = v.Length > 0 ? Math.Sqrt(v.Sum(r => r * r) / v.Length) * 1000 : 0; // en mm
            double maxCorrection = xHat.Length > 0 ? xHat.Max(Math.Abs) * 1000 : 0; // en mm
            
            return new AdvancedCompensationStatistics
            {
                SigmaPosteriori = sigmaPosteriori,
                DegreesOfFreedom = dof,
                Chi2Statistic = chi2Statistic,
                Chi2Critical = chi2Critical,
                UnitWeightValid = unitWeightValid,
                MaxStandardizedResidual = maxNormResidual,
                BlunderDetectionThreshold = blunderThreshold,
                SuspectObservations = suspectObs,
                RMSE = rmse,
                MaxCorrection = maxCorrection,
                MethodUsed = method
            };
        }
        
        /// <summary>
        /// Calcul des résidus normalisés
        /// </summary>
        private double[] CalculateNormalizedResiduals(
            double[] v, double[,] A, double[,] P, 
            double[,] Qx, double sigma0)
        {
            int n = v.Length;
            var normalized = new double[n];
            
            try
            {
                // Matrice de covariance des résidus : Qv = P^(-1) - A Qx A^T
                var Pinv = InvertMatrix(P);
                var AQx = MultiplyMatrices(A, Qx);
                var AQxAt = MultiplyMatrices(AQx, TransposeMatrix(A));
                
                for (int i = 0; i < n; i++)
                {
                    double qvv = Pinv[i, i] - AQxAt[i, i];
                    if (qvv > 1e-10) // Éviter division par zéro
                    {
                        normalized[i] = v[i] / (sigma0 * Math.Sqrt(qvv));
                    }
                    else
                    {
                        normalized[i] = 0;
                    }
                }
            }
            catch
            {
                // Si erreur, retourner résidus simples normalisés
                for (int i = 0; i < n; i++)
                {
                    normalized[i] = sigma0 > 0 ? v[i] / sigma0 : 0;
                }
            }
            
            return normalized;
        }
        
        // === Méthodes utilitaires pour les opérations matricielles ===
        
        private double[,] TransposeMatrix(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            var result = new double[cols, rows];
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[j, i] = matrix[i, j];
                }
            }
            
            return result;
        }
        
        private double[,] MultiplyMatrices(double[,] A, double[,] B)
        {
            int rowsA = A.GetLength(0);
            int colsA = A.GetLength(1);
            int colsB = B.GetLength(1);
            
            var result = new double[rowsA, colsB];
            
            for (int i = 0; i < rowsA; i++)
            {
                for (int j = 0; j < colsB; j++)
                {
                    for (int k = 0; k < colsA; k++)
                    {
                        result[i, j] += A[i, k] * B[k, j];
                    }
                }
            }
            
            return result;
        }
        
        private double[] MultiplyMatrixVector(double[,] A, double[] v)
        {
            int rows = A.GetLength(0);
            int cols = A.GetLength(1);
            var result = new double[rows];
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i] += A[i, j] * v[j];
                }
            }
            
            return result;
        }
        
        private double[] MultiplyMatrix(double[,] A, double[] x)
        {
            return MultiplyMatrixVector(A, x);
        }
        
        // Implémentation simplifiée des méthodes matricielles
        private double[,] InvertMatrix(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            var result = new double[n, n];
            var temp = new double[n, 2 * n];
            
            // Créer matrice augmentée [A|I]
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    temp[i, j] = matrix[i, j];
                    temp[i, j + n] = (i == j) ? 1.0 : 0.0;
                }
            }
            
            // Gauss-Jordan
            for (int i = 0; i < n; i++)
            {
                // Recherche du pivot
                double pivot = temp[i, i];
                if (Math.Abs(pivot) < 1e-10)
                {
                    // Rechercher ligne avec élément non nul
                    for (int k = i + 1; k < n; k++)
                    {
                        if (Math.Abs(temp[k, i]) > 1e-10)
                        {
                            // Échanger lignes
                            for (int j = 0; j < 2 * n; j++)
                            {
                                (temp[i, j], temp[k, j]) = (temp[k, j], temp[i, j]);
                            }
                            pivot = temp[i, i];
                            break;
                        }
                    }
                }
                
                // Normaliser ligne pivot
                for (int j = 0; j < 2 * n; j++)
                {
                    temp[i, j] /= pivot;
                }
                
                // Éliminer colonne
                for (int k = 0; k < n; k++)
                {
                    if (k != i)
                    {
                        double factor = temp[k, i];
                        for (int j = 0; j < 2 * n; j++)
                        {
                            temp[k, j] -= factor * temp[i, j];
                        }
                    }
                }
            }
            
            // Extraire la matrice inverse
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    result[i, j] = temp[i, j + n];
                }
            }
            
            return result;
        }
        
        private double[,] CholeskyDecomposition(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            var L = new double[n, n];
            
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < j; k++)
                    {
                        sum += L[i, k] * L[j, k];
                    }
                    
                    if (i == j)
                    {
                        double value = matrix[i, i] - sum;
                        if (value <= 0) throw new InvalidOperationException("Matrice non définie positive");
                        L[i, j] = Math.Sqrt(value);
                    }
                    else
                    {
                        L[i, j] = (matrix[i, j] - sum) / L[j, j];
                    }
                }
            }
            
            return L;
        }
        
        private (double[,] Q, double[,] R) QRDecomposition(double[,] A)
        {
            int m = A.GetLength(0);
            int n = A.GetLength(1);
            var Q = new double[m, n];
            var R = new double[n, n];
            
            // Copier A dans Q
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    Q[i, j] = A[i, j];
                }
            }
            
            // Gram-Schmidt modifié
            for (int j = 0; j < n; j++)
            {
                // Orthogonaliser contre colonnes précédentes
                for (int k = 0; k < j; k++)
                {
                    double dot = 0;
                    for (int i = 0; i < m; i++)
                    {
                        dot += Q[i, k] * Q[i, j];
                    }
                    R[k, j] = dot;
                    
                    for (int i = 0; i < m; i++)
                    {
                        Q[i, j] -= dot * Q[i, k];
                    }
                }
                
                // Normaliser
                double norm = 0;
                for (int i = 0; i < m; i++)
                {
                    norm += Q[i, j] * Q[i, j];
                }
                norm = Math.Sqrt(norm);
                R[j, j] = norm;
                
                if (norm > 1e-10)
                {
                    for (int i = 0; i < m; i++)
                    {
                        Q[i, j] /= norm;
                    }
                }
            }
            
            return (Q, R);
        }
        
        private double[,] MatrixSquareRoot(double[,] P)
        {
            int n = P.GetLength(0);
            var result = new double[n, n];
            
            for (int i = 0; i < n; i++)
            {
                result[i, i] = Math.Sqrt(Math.Max(P[i, i], 1e-10));
            }
            
            return result;
        }
        
        private double[] ForwardSubstitution(double[,] L, double[] b)
        {
            int n = b.Length;
            var x = new double[n];
            
            for (int i = 0; i < n; i++)
            {
                x[i] = b[i];
                for (int j = 0; j < i; j++)
                {
                    x[i] -= L[i, j] * x[j];
                }
                if (Math.Abs(L[i, i]) > 1e-10)
                    x[i] /= L[i, i];
            }
            
            return x;
        }
        
        private double[] BackwardSubstitution(double[,] U, double[] b)
        {
            int n = b.Length;
            var x = new double[n];
            
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = b[i];
                for (int j = i + 1; j < n; j++)
                {
                    x[i] -= U[i, j] * x[j];
                }
                if (Math.Abs(U[i, i]) > 1e-10)
                    x[i] /= U[i, i];
            }
            
            return x;
        }
        
        private double[,] InvertLowerTriangular(double[,] L)
        {
            int n = L.GetLength(0);
            var inv = new double[n, n];
            
            for (int i = 0; i < n; i++)
            {
                inv[i, i] = 1.0 / L[i, i];
                for (int j = i + 1; j < n; j++)
                {
                    double sum = 0;
                    for (int k = i; k < j; k++)
                    {
                        sum += L[j, k] * inv[k, i];
                    }
                    inv[j, i] = -sum / L[j, j];
                }
            }
            
            return inv;
        }
        
        private double[,] InvertUpperTriangular(double[,] U)
        {
            return TransposeMatrix(InvertLowerTriangular(TransposeMatrix(U)));
        }
        
        private double CalculateChi2Critical(int df, double confidence)
        {
            // Approximation de Wilson-Hilferty
            double z = 1.96; // Pour 95% de confiance
            double a = 2.0 / (9.0 * df);
            return df * Math.Pow(1 - a + z * Math.Sqrt(a), 3);
        }
        
        private double CalculateTCritical(int df, double confidence)
        {
            // Table simplifiée
            if (df > 30) return 1.96;
            if (df > 20) return 2.086;
            if (df > 10) return 2.228;
            if (df > 5) return 2.571;
            return 2.776;
        }
        
        private List<double> CalculateAdjustedAltitudes(
            List<LevelingData> data,
            double[] corrections,
            double referenceAltitude)
        {
            var altitudes = new List<double> { referenceAltitude };
            double currentAltitude = referenceAltitude;
            
            for (int i = 0; i < data.Count; i++)
            {
                double dh = data[i].CalculateAverageDenivelation();
                double correction = (i < corrections.Length) ? corrections[i] : 0;
                currentAltitude += dh + correction;
                altitudes.Add(currentAltitude);
            }
            
            return altitudes;
        }
        
        private void ValidateResults(AdvancedCompensationStatistics stats, double[] residuals)
        {
            Console.WriteLine("\n📊 Statistiques de compensation avancées:");
            Console.WriteLine($"   Méthode utilisée: {stats.MethodUsed}");
            Console.WriteLine($"   σ₀ a posteriori: {stats.SigmaPosteriori:F4}");
            Console.WriteLine($"   Degrés de liberté: {stats.DegreesOfFreedom}");
            Console.WriteLine($"   Test χ²: {stats.Chi2Statistic:F2} / {stats.Chi2Critical:F2}");
            Console.WriteLine($"   Poids unitaire valide: {(stats.UnitWeightValid ? "✅" : "❌")}");
            Console.WriteLine($"   RMSE: {stats.RMSE:F2} mm");
            Console.WriteLine($"   Correction max: {stats.MaxCorrection:F2} mm");
            Console.WriteLine($"   Temps de calcul: {stats.ComputationTime.TotalMilliseconds:F0} ms");
            
            if (stats.SuspectObservations.Count > 0)
            {
                Console.WriteLine($"   ⚠️ {stats.SuspectObservations.Count} observation(s) suspecte(s): [{string.Join(", ", stats.SuspectObservations)}]");
            }
            
            if (stats.MaxCorrection > 1000)
            {
                Console.WriteLine("   ⚠️ ATTENTION: Corrections très importantes détectées!");
                Console.WriteLine("      Vérifiez les altitudes de référence et les unités");
            }
        }
    }
}