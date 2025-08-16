// ================================================================
// ÉTAPE 3 - CALCUL DES POIDS GÉODÉSIQUES
// Transposition FIDÈLE du module Python WeightCalculator
// Théorie géodésique: σ² = a² + b²×d (variance selon distance)
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Calculations.Validation;

namespace CompensationAltimetrique.Calculations.Weights
{
    /// <summary>
    /// Paramètres de la théorie géodésique pour calcul des poids.
    /// Équivalent des paramètres Python du WeightCalculator.
    /// </summary>
    public class GeodeticParameters
    {
        /// <summary>Erreur instrumentale (mm) - paramètre 'a'</summary>
        public double InstrumentalErrorMm { get; set; } = 1.0;

        /// <summary>Erreur kilométrique (mm/km) - paramètre 'b'</summary>
        public double KilometricErrorMm { get; set; } = 1.0;

        /// <summary>Méthode de pondération utilisée</summary>
        public string WeightingMethod { get; set; } = "geodetic_variance";

        /// <summary>Distance par défaut pour observations sans distance (m)</summary>
        public double DefaultDistanceM { get; set; } = 50.0;

        public override string ToString() =>
            $"a={InstrumentalErrorMm}mm, b={KilometricErrorMm}mm/km, défaut={DefaultDistanceM}m";
    }

    /// <summary>
    /// Statistiques des poids calculés pour diagnostic.
    /// Équivalent de get_weight_statistics Python.
    /// </summary>
    public class WeightStatistics
    {
        public double MinWeight { get; set; }
        public double MaxWeight { get; set; }
        public double MeanWeight { get; set; }
        public double StdWeight { get; set; }
        public double WeightRatio { get; set; } // Max/Min ratio
        public int TotalObservations { get; set; }
        public double ConditionNumber { get; set; }

        public string GetSummary() =>
            $"Poids: min={MinWeight:E2}, max={MaxWeight:E2}, ratio={WeightRatio:F1}, obs={TotalObservations}";
    }

    /// <summary>
    /// Calculateur de poids géodésiques selon la théorie des moindres carrés.
    /// TRANSPOSITION EXACTE de WeightCalculator Python.
    /// 
    /// Implémente la formule de variance théorique:
    /// σ² = a² + b²×d (mm²)
    /// où a = erreur instrumentale, b = erreur kilométrique, d = distance
    /// 
    /// Poids = 1/σ² pour pondération optimale
    /// </summary>
    public class WeightCalculator
    {
        private readonly GeodeticParameters _parameters;
        private readonly GeodeticValidator _validator;

        public WeightCalculator(double instrumentalErrorMm = 1.0, double kilometricErrorMm = 1.0)
        {
            _parameters = new GeodeticParameters
            {
                InstrumentalErrorMm = instrumentalErrorMm,
                KilometricErrorMm = kilometricErrorMm
            };
            _validator = new GeodeticValidator();

            if (instrumentalErrorMm <= 0 || kilometricErrorMm <= 0)
                throw new ArgumentException("Les paramètres d'erreur doivent être positifs");
        }

        public WeightCalculator(GeodeticParameters parameters)
        {
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            _validator = new GeodeticValidator();
        }

        /// <summary>
        /// Calcul des poids géodésiques selon la théorie σ² = a² + b²×d.
        /// TRANSPOSITION EXACTE de calculate_weights Python.
        /// 
        /// Algorithme:
        /// 1. Validation des distances
        /// 2. Conversion distances en km
        /// 3. Calcul variance: σ² = a² + b²×d
        /// 4. Calcul poids: P = 1/σ²
        /// 5. Construction matrice diagonale
        /// </summary>
        public Matrix<double> CalculateWeights(IEnumerable<double> distancesM)
        {
            try
            {
                var distances = distancesM.ToList();
                
                // Validation des distances (équivalent Python)
                var validation = _validator.ValidateDistances(distances);
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Distances invalides: {string.Join("; ", validation.Errors)}");
                }

                var weights = new List<double>();

                foreach (var distanceM in distances)
                {
                    // Gestion des distances manquantes ou nulles (logique Python)
                    double effectiveDistance = distanceM;
                    if (double.IsNaN(distanceM) || distanceM <= 0)
                    {
                        effectiveDistance = _parameters.DefaultDistanceM;
                    }

                    // Conversion en kilomètres (équivalent Python)
                    double distanceKm = effectiveDistance / 1000.0;

                    // Calcul de la variance selon théorie géodésique (formule Python exacte)
                    // Python: variance_mm2 = self.a_mm**2 + (self.b_mm_per_km * distance_km)**2
                    double varianceMm2 = Math.Pow(_parameters.InstrumentalErrorMm, 2) + 
                                        Math.Pow(_parameters.KilometricErrorMm * distanceKm, 2);

                    // Calcul du poids (équivalent Python: weight = 1.0 / variance_mm2)
                    if (varianceMm2 <= 0)
                        throw new InvalidOperationException($"Variance négative ou nulle: {varianceMm2}");

                    double weight = 1.0 / varianceMm2;
                    weights.Add(weight);
                }

                // Construction de la matrice diagonale des poids (équivalent Python: np.diag(weights))
                return DiagonalMatrix.OfDiagonal(weights.Count, weights.Count, weights.ToArray());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur calcul poids géodésiques: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calcul du poids d'une observation individuelle.
        /// Équivalent de calculate_observation_weight Python.
        /// </summary>
        public double CalculateObservationWeight(double distanceM)
        {
            if (distanceM <= 0)
                distanceM = _parameters.DefaultDistanceM;

            double distanceKm = distanceM / 1000.0;
            double varianceMm2 = Math.Pow(_parameters.InstrumentalErrorMm, 2) + 
                                Math.Pow(_parameters.KilometricErrorMm * distanceKm, 2);

            return 1.0 / varianceMm2;
        }

        /// <summary>
        /// Calcul des poids pour les données de nivellement.
        /// Application de la théorie géodésique aux données LevelingData.
        /// </summary>
        public Matrix<double> CalculateWeightsForLevelingData(List<LevelingData> levelingData)
        {
            var distances = new List<double>();

            foreach (var data in levelingData)
            {
                // Utiliser la distance disponible (DIST1 ou DIST2 ou moyenne)
                double distance = _parameters.DefaultDistanceM;

                if (data.DIST1.HasValue && data.DIST2.HasValue)
                {
                    // Moyenne des deux sessions si disponibles
                    distance = (data.DIST1.Value + data.DIST2.Value) / 2.0;
                }
                else if (data.DIST1.HasValue)
                {
                    distance = data.DIST1.Value;
                }
                else if (data.DIST2.HasValue)
                {
                    distance = data.DIST2.Value;
                }

                distances.Add(distance);
            }

            return CalculateWeights(distances);
        }

        /// <summary>
        /// Calcul des statistiques des poids pour diagnostic.
        /// TRANSPOSITION EXACTE de get_weight_statistics Python.
        /// </summary>
        public WeightStatistics GetWeightStatistics(Matrix<double> weightMatrix)
        {
            try
            {
                if (!weightMatrix.IsSymmetric())
                    throw new ArgumentException("La matrice des poids doit être symétrique");

                // Extraire les poids diagonaux
                var diagonalWeights = weightMatrix.Diagonal().ToArray();

                if (diagonalWeights.Length == 0)
                    throw new ArgumentException("Matrice des poids vide");

                // Calcul des statistiques (équivalent Python)
                double minWeight = diagonalWeights.Min();
                double maxWeight = diagonalWeights.Max();
                double meanWeight = diagonalWeights.Average();
                double stdWeight = CalculateStandardDeviation(diagonalWeights, meanWeight);
                double weightRatio = minWeight > 0 ? maxWeight / minWeight : double.PositiveInfinity;

                // Nombre de conditionnement de la matrice
                double conditionNumber = CalculateConditionNumber(weightMatrix);

                return new WeightStatistics
                {
                    MinWeight = minWeight,
                    MaxWeight = maxWeight,
                    MeanWeight = meanWeight,
                    StdWeight = stdWeight,
                    WeightRatio = weightRatio,
                    TotalObservations = diagonalWeights.Length,
                    ConditionNumber = conditionNumber
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur calcul statistiques poids: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calcul de la variance théorique pour une distance donnée.
        /// Formule Python exacte: σ² = a² + b²×d
        /// </summary>
        public double CalculateTheoreticalVariance(double distanceM)
        {
            double distanceKm = distanceM / 1000.0;
            return Math.Pow(_parameters.InstrumentalErrorMm, 2) + 
                   Math.Pow(_parameters.KilometricErrorMm * distanceKm, 2);
        }

        /// <summary>
        /// Calcul de l'écart-type théorique pour une distance donnée.
        /// σ = √(a² + b²×d)
        /// </summary>
        public double CalculateTheoreticalStandardDeviation(double distanceM)
        {
            return Math.Sqrt(CalculateTheoreticalVariance(distanceM));
        }

        /// <summary>
        /// Optimisation des paramètres géodésiques selon les résidus observés.
        /// Méthode avancée pour calibrer a et b selon les données réelles.
        /// </summary>
        public GeodeticParameters OptimizeParameters(IEnumerable<double> distances, IEnumerable<double> residuals)
        {
            var distArray = distances.ToArray();
            var residArray = residuals.ToArray();

            if (distArray.Length != residArray.Length)
                throw new ArgumentException("Nombre de distances et résidus différent");

            // Méthode des moindres carrés pour optimiser a et b
            // σ²ᵢ = a² + b²×dᵢ ≈ residual²ᵢ
            
            var distKm = distArray.Select(d => d / 1000.0).ToArray();
            var residSquared = residArray.Select(r => r * r * 1e6).ToArray(); // Conversion en mm²

            // Construction du système normal: [n Σd; Σd Σd²] [a²; b²] = [Σr²; Σr²d]
            int n = distArray.Length;
            double sumD = distKm.Sum();
            double sumD2 = distKm.Select(d => d * d).Sum();
            double sumR2 = residSquared.Sum();
            double sumR2D = distKm.Zip(residSquared, (d, r) => r * d).Sum();

            // Résolution du système 2×2
            double det = n * sumD2 - sumD * sumD;
            if (Math.Abs(det) < 1e-10)
            {
                // Système singulier - garder les paramètres actuels
                return _parameters;
            }

            double a2 = (sumR2 * sumD2 - sumR2D * sumD) / det;
            double b2 = (n * sumR2D - sumR2 * sumD) / det;

            // Vérifier que les valeurs sont positives
            if (a2 > 0 && b2 > 0)
            {
                return new GeodeticParameters
                {
                    InstrumentalErrorMm = Math.Sqrt(a2),
                    KilometricErrorMm = Math.Sqrt(b2),
                    WeightingMethod = "optimized_geodetic",
                    DefaultDistanceM = _parameters.DefaultDistanceM
                };
            }
            else
            {
                return _parameters; // Garder les paramètres actuels si optimisation échoue
            }
        }

        /// <summary>
        /// Validation de la cohérence des poids calculés.
        /// Contrôles de qualité selon normes géodésiques.
        /// </summary>
        public ValidationResult ValidateWeights(Matrix<double> weightMatrix, IEnumerable<double> distances)
        {
            var result = new ValidationResult();

            try
            {
                var stats = GetWeightStatistics(weightMatrix);
                var distList = distances.ToList();

                // Contrôle 1: Ratio des poids acceptable
                if (stats.WeightRatio > 1000)
                {
                    result.AddWarning($"Ratio des poids élevé: {stats.WeightRatio:F1} (> 1000)");
                }
                else
                {
                    result.AddSuccess($"Ratio des poids acceptable: {stats.WeightRatio:F1}");
                }

                // Contrôle 2: Nombre de conditionnement
                if (stats.ConditionNumber > 1e12)
                {
                    result.AddWarning($"Matrice mal conditionnée: {stats.ConditionNumber:E2}");
                }
                else
                {
                    result.AddSuccess($"Conditionnement acceptable: {stats.ConditionNumber:E2}");
                }

                // Contrôle 3: Cohérence variance vs distance
                double correlationCoeff = CalculateVarianceDistanceCorrelation(distList);
                if (correlationCoeff > 0.8)
                {
                    result.AddSuccess($"Forte corrélation variance-distance: {correlationCoeff:F3}");
                }
                else
                {
                    result.AddWarning($"Corrélation variance-distance faible: {correlationCoeff:F3}");
                }

                // Détails pour analyse
                result.Details["weight_statistics"] = stats;
                result.Details["parameters"] = _parameters;
                result.Details["variance_distance_correlation"] = correlationCoeff;

            }
            catch (Exception ex)
            {
                result.AddError($"Erreur validation poids: {ex.Message}");
            }

            return result;
        }

        // Méthodes utilitaires privées
        private double CalculateStandardDeviation(double[] values, double mean)
        {
            if (values.Length <= 1) return 0.0;
            
            double sumSquaredDiffs = values.Select(v => Math.Pow(v - mean, 2)).Sum();
            return Math.Sqrt(sumSquaredDiffs / (values.Length - 1));
        }

        private double CalculateConditionNumber(Matrix<double> matrix)
        {
            try
            {
                var eigenDecomp = matrix.Evd();
                var eigenValues = eigenDecomp.EigenValues.Real().ToArray();
                
                if (eigenValues.Length == 0) return double.PositiveInfinity;
                
                double maxEV = eigenValues.Max();
                double minEV = eigenValues.Where(ev => ev > 1e-15).DefaultIfEmpty(1e-15).Min();
                
                return maxEV / minEV;
            }
            catch
            {
                return double.PositiveInfinity;
            }
        }

        private double CalculateVarianceDistanceCorrelation(List<double> distances)
        {
            if (distances.Count < 2) return 0.0;

            var variances = distances.Select(d => CalculateTheoreticalVariance(d)).ToArray();
            
            // Calcul coefficient de corrélation de Pearson
            double meanDist = distances.Average();
            double meanVar = variances.Average();
            
            double numerator = 0.0;
            double sumSquaredDist = 0.0;
            double sumSquaredVar = 0.0;

            for (int i = 0; i < distances.Count; i++)
            {
                double diffDist = distances[i] - meanDist;
                double diffVar = variances[i] - meanVar;
                
                numerator += diffDist * diffVar;
                sumSquaredDist += diffDist * diffDist;
                sumSquaredVar += diffVar * diffVar;
            }

            double denominator = Math.Sqrt(sumSquaredDist * sumSquaredVar);
            return denominator > 1e-15 ? numerator / denominator : 0.0;
        }
    }

    /// <summary>
    /// Builder pour construction de matrices de poids complexes.
    /// Extension du WeightCalculator pour cas avancés.
    /// </summary>
    public class WeightMatrixBuilder
    {
        private readonly WeightCalculator _weightCalculator;

        public WeightMatrixBuilder(WeightCalculator weightCalculator)
        {
            _weightCalculator = weightCalculator ?? throw new ArgumentNullException(nameof(weightCalculator));
        }

        /// <summary>
        /// Construction de matrice de poids pour observations multiples par point.
        /// Gestion des sessions multiples de nivellement.
        /// </summary>
        public Matrix<double> BuildMultiSessionWeights(List<LevelingData> levelingData)
        {
            var weights = new List<double>();

            foreach (var data in levelingData)
            {
                // Session 1
                if (data.AR1.HasValue && data.AV1.HasValue)
                {
                    double distance1 = data.DIST1 ?? 50.0;
                    double weight1 = _weightCalculator.CalculateObservationWeight(distance1);
                    weights.Add(weight1);
                }

                // Session 2
                if (data.AR2.HasValue && data.AV2.HasValue)
                {
                    double distance2 = data.DIST2 ?? 50.0;
                    double weight2 = _weightCalculator.CalculateObservationWeight(distance2);
                    weights.Add(weight2);
                }
            }

            return DiagonalMatrix.OfDiagonal(weights.Count, weights.Count, weights.ToArray());
        }

        /// <summary>
        /// Construction de matrice de poids avec corrélations.
        /// Pour observations non-indépendantes.
        /// </summary>
        public Matrix<double> BuildCorrelatedWeights(IEnumerable<double> distances, double correlationCoeff = 0.0)
        {
            var distArray = distances.ToArray();
            int n = distArray.Length;
            
            var weightMatrix = DenseMatrix.Create(n, n, 0.0);

            // Calcul des variances diagonales
            for (int i = 0; i < n; i++)
            {
                double variance = _weightCalculator.CalculateTheoreticalVariance(distArray[i]);
                weightMatrix[i, i] = 1.0 / variance;
            }

            // Ajout des corrélations si nécessaire
            if (Math.Abs(correlationCoeff) > 1e-6)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        double covariance = correlationCoeff * 
                            Math.Sqrt(_weightCalculator.CalculateTheoreticalVariance(distArray[i]) *
                                     _weightCalculator.CalculateTheoreticalVariance(distArray[j]));
                        
                        double weight_ij = -1.0 / covariance; // Poids = inverse covariance (signe négatif)
                        weightMatrix[i, j] = weight_ij;
                        weightMatrix[j, i] = weight_ij;
                    }
                }
            }

            return weightMatrix;
        }
    }
}