// ============================================================================
// ÉTAPE 2: ANALYSE STATISTIQUE
// Implémentation complète des analyses statistiques pour compensation altimétrique
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Distributions;

namespace CompensationAltimetrique.Statistics
{
    /// <summary>
    /// Statistiques complètes de la compensation
    /// </summary>
    public class CompensationStatistics
    {
        public double Sigma0Hat { get; set; }                    // Écart-type a posteriori
        public int DegreesOfFreedom { get; set; }               // Degrés de liberté
        public double Chi2TestStatistic { get; set; }           // Statistique χ²
        public double Chi2CriticalValue { get; set; }           // Valeur critique χ²
        public bool UnitWeightValid { get; set; }               // Validation poids unitaire
        public double MaxStandardizedResidual { get; set; }     // Résidu normalisé max
        public double BlunderDetectionThreshold { get; set; }   // Seuil détection fautes
        public List<BlunderDetection> BlundersDetected { get; set; } = new(); // Fautes détectées
        public Matrix<double> ResidualCovarianceMatrix { get; set; } = null!; // Covariance des résidus
        public double[] StandardizedResiduals { get; set; } = Array.Empty<double>(); // Résidus normalisés
        public double VarianceFactorTest { get; set; }          // Test du facteur de variance
        public string QualityAssessment { get; set; } = string.Empty; // Évaluation globale qualité
    }

    /// <summary>
    /// Information sur une faute grossière détectée
    /// </summary>
    public class BlunderDetection
    {
        public int ObservationIndex { get; set; }               // Index de l'observation
        public string ObservationId { get; set; } = string.Empty; // Identifiant observation
        public double StandardizedResidual { get; set; }        // Résidu normalisé
        public double TestStatistic { get; set; }               // Statistique de test
        public double CriticalValue { get; set; }               // Valeur critique
        public double SignificanceLevel { get; set; }           // Niveau de signification
        public BlunderSeverity Severity { get; set; }           // Gravité de la faute
        public string Description { get; set; } = string.Empty; // Description
    }

    /// <summary>
    /// Gravité d'une faute grossière
    /// </summary>
    public enum BlunderSeverity
    {
        Minor,      // Faute mineure (1-2σ)
        Moderate,   // Faute modérée (2-3σ)
        Severe,     // Faute sévère (3-5σ)
        Critical    // Faute critique (>5σ)
    }

    /// <summary>
    /// Résultat d'un test statistique
    /// </summary>
    public class StatisticalTestResult
    {
        public string TestName { get; set; } = string.Empty;    // Nom du test
        public double TestStatistic { get; set; }               // Statistique calculée
        public double CriticalValue { get; set; }               // Valeur critique
        public double PValue { get; set; }                      // Valeur p
        public bool IsSignificant { get; set; }                 // Significatif ou non
        public double ConfidenceLevel { get; set; }             // Niveau de confiance
        public string Interpretation { get; set; } = string.Empty; // Interprétation
    }

    /// <summary>
    /// Analyseur statistique avancé pour la compensation altimétrique
    /// Implémente tous les tests statistiques nécessaires selon les normes géodésiques
    /// </summary>
    public class StatisticalAnalyzer
    {
        private readonly double _confidenceLevel;
        private readonly double _alpha;  // Niveau de signification

        public StatisticalAnalyzer(double confidenceLevel = 0.95)
        {
            _confidenceLevel = confidenceLevel;
            _alpha = 1.0 - confidenceLevel;
        }

        /// <summary>
        /// Analyse statistique complète de la compensation
        /// Calcule tous les indicateurs de qualité selon la théorie des moindres carrés
        /// </summary>
        /// <param name="A">Matrice de conception</param>
        /// <param name="P">Matrice de poids</param>
        /// <param name="solution">Solution estimée</param>
        /// <param name="observations">Vecteur des observations</param>
        /// <param name="covarianceMatrix">Matrice de covariance</param>
        /// <returns>Statistiques complètes</returns>
        public CompensationStatistics AnalyzeCompensation(
            Matrix<double> A, 
            Matrix<double> P, 
            Vector<double> solution,
            Vector<double> observations,
            Matrix<double> covarianceMatrix)
        {
            Console.WriteLine("📊 Début de l'analyse statistique...");

            // 1. Calcul des résidus
            var residuals = CalculateResiduals(A, solution, observations);
            
            // 2. Degrés de liberté
            int degreesOfFreedom = A.RowCount - A.ColumnCount;
            
            if (degreesOfFreedom <= 0)
            {
                throw new InvalidOperationException(
                    $"Système sous-déterminé: {A.RowCount} observations, {A.ColumnCount} inconnues");
            }

            // 3. Écart-type a posteriori σ₀ = √(vᵀPv/r)
            double sigma0Hat = CalculateSigma0Posteriori(residuals, P, degreesOfFreedom);

            // 4. Test χ² du poids unitaire
            var chi2Test = PerformChi2UnitWeightTest(residuals, P, degreesOfFreedom);

            // 5. Résidus normalisés et covariance des résidus
            var (standardizedResiduals, residualCovariance) = 
                CalculateStandardizedResiduals(residuals, A, P, covarianceMatrix, sigma0Hat);

            // 6. Détection des fautes grossières
            var blundersDetected = DetectBlunders(standardizedResiduals, degreesOfFreedom);

            // 7. Test du facteur de variance
            double varianceFactorTest = CalculateVarianceFactorTest(chi2Test.TestStatistic, degreesOfFreedom);

            // 8. Évaluation globale de la qualité
            string qualityAssessment = AssessOverallQuality(sigma0Hat, chi2Test.IsSignificant, 
                blundersDetected.Count, standardizedResiduals.Max());

            var statistics = new CompensationStatistics
            {
                Sigma0Hat = sigma0Hat,
                DegreesOfFreedom = degreesOfFreedom,
                Chi2TestStatistic = chi2Test.TestStatistic,
                Chi2CriticalValue = chi2Test.CriticalValue,
                UnitWeightValid = !chi2Test.IsSignificant,
                MaxStandardizedResidual = standardizedResiduals.Select(Math.Abs).Max(),
                BlunderDetectionThreshold = StudentT.InvCDF(0, 1, degreesOfFreedom, 1 - _alpha/2),
                BlundersDetected = blundersDetected,
                ResidualCovarianceMatrix = residualCovariance,
                StandardizedResiduals = standardizedResiduals,
                VarianceFactorTest = varianceFactorTest,
                QualityAssessment = qualityAssessment
            };

            Console.WriteLine($"✅ Analyse terminée: σ₀={sigma0Hat:F4}, {blundersDetected.Count} fautes détectées");
            
            return statistics;
        }

        /// <summary>
        /// Calcul de l'écart-type a posteriori selon la formule géodésique
        /// σ₀ = √(vᵀPv/r) où v=résidus, P=poids, r=degrés de liberté
        /// </summary>
        private double CalculateSigma0Posteriori(Vector<double> residuals, Matrix<double> P, int degreesOfFreedom)
        {
            // Forme quadratique vᵀPv
            double vtPv = (residuals.ToRowMatrix() * P * residuals.ToColumnMatrix())[0, 0];
            
            // Écart-type a posteriori
            double sigma0Hat = Math.Sqrt(vtPv / degreesOfFreedom);
            
            Console.WriteLine($"📈 σ₀ a posteriori: {sigma0Hat:F6}");
            
            return sigma0Hat;
        }

        /// <summary>
        /// Test χ² du poids unitaire
        /// H₀: σ₀² = 1 (poids unitaire valide)
        /// H₁: σ₀² ≠ 1 (poids unitaire invalide)
        /// </summary>
        private StatisticalTestResult PerformChi2UnitWeightTest(Vector<double> residuals, Matrix<double> P, int degreesOfFreedom)
        {
            // Statistique de test: T = vᵀPv
            double testStatistic = (residuals.ToRowMatrix() * P * residuals.ToColumnMatrix())[0, 0];
            
            // Valeur critique du χ² à (1-α) avec r degrés de liberté
            double criticalValue = ChiSquared.InvCDF(degreesOfFreedom, _confidenceLevel);
            
            // Valeur p
            double pValue = 1.0 - ChiSquared.CDF(degreesOfFreedom, testStatistic);
            
            bool isSignificant = testStatistic > criticalValue;
            
            string interpretation = isSignificant 
                ? "Rejet H₀: poids unitaire invalide" 
                : "Acceptation H₀: poids unitaire valide";
            
            Console.WriteLine($"🧪 Test χ²: T={testStatistic:F2}, critique={criticalValue:F2}, " +
                            $"p={pValue:F4} → {interpretation}");
            
            return new StatisticalTestResult
            {
                TestName = "Chi-deux poids unitaire",
                TestStatistic = testStatistic,
                CriticalValue = criticalValue,
                PValue = pValue,
                IsSignificant = isSignificant,
                ConfidenceLevel = _confidenceLevel,
                Interpretation = interpretation
            };
        }

        /// <summary>
        /// Calcul des résidus normalisés pour détection de fautes
        /// r̂ᵢ = vᵢ / (σ₀ √qᵥᵥᵢ) où qᵥᵥᵢ est l'élément diagonal de la covariance des résidus
        /// </summary>
        private (double[] standardizedResiduals, Matrix<double> residualCovariance) 
            CalculateStandardizedResiduals(Vector<double> residuals, Matrix<double> A, Matrix<double> P, 
                                         Matrix<double> coordinateCovariance, double sigma0Hat)
        {
            // Matrice de covariance des résidus: Qᵥ = P⁻¹ - A Qₓ Aᵀ
            var P_inv = P.Inverse();
            var residualCovariance = P_inv - A * coordinateCovariance * A.Transpose();
            
            // Résidus normalisés
            var standardizedResiduals = new double[residuals.Count];
            
            for (int i = 0; i < residuals.Count; i++)
            {
                double qvv_ii = residualCovariance[i, i];
                if (qvv_ii > 0)
                {
                    standardizedResiduals[i] = residuals[i] / (sigma0Hat * Math.Sqrt(qvv_ii));
                }
                else
                {
                    standardizedResiduals[i] = 0; // Éviter division par zéro
                }
            }
            
            Console.WriteLine($"📏 Résidus normalisés calculés, max: {standardizedResiduals.Select(Math.Abs).Max():F3}");
            
            return (standardizedResiduals, residualCovariance);
        }

        /// <summary>
        /// Détection des fautes grossières par test de Student
        /// Utilise les résidus normalisés avec test bilatéral
        /// </summary>
        private List<BlunderDetection> DetectBlunders(double[] standardizedResiduals, int degreesOfFreedom)
        {
            var blunders = new List<BlunderDetection>();
            
            // Valeur critique du test de Student (test bilatéral)
            double criticalValue = StudentT.InvCDF(0, 1, degreesOfFreedom, 1 - _alpha/2);
            
            Console.WriteLine($"🔍 Détection fautes, seuil critique: ±{criticalValue:F3}");
            
            for (int i = 0; i < standardizedResiduals.Length; i++)
            {
                double absResidual = Math.Abs(standardizedResiduals[i]);
                
                if (absResidual > criticalValue)
                {
                    // Calcul de la valeur p pour ce résidu
                    double pValue = 2.0 * (1.0 - StudentT.CDF(0, 1, degreesOfFreedom, absResidual));
                    
                    // Détermination de la gravité
                    BlunderSeverity severity = DetermineBlunderSeverity(absResidual, criticalValue);
                    
                    var blunder = new BlunderDetection
                    {
                        ObservationIndex = i,
                        ObservationId = $"Obs_{i+1}",
                        StandardizedResidual = standardizedResiduals[i],
                        TestStatistic = absResidual,
                        CriticalValue = criticalValue,
                        SignificanceLevel = pValue,
                        Severity = severity,
                        Description = $"Faute {severity} détectée (|t|={absResidual:F3} > {criticalValue:F3})"
                    };
                    
                    blunders.Add(blunder);
                    
                    Console.WriteLine($"⚠️ {blunder.Description}");
                }
            }
            
            Console.WriteLine($"🎯 Total fautes détectées: {blunders.Count}");
            
            return blunders;
        }

        /// <summary>
        /// Détermine la gravité d'une faute selon l'amplitude du résidu normalisé
        /// </summary>
        private BlunderSeverity DetermineBlunderSeverity(double absResidual, double criticalValue)
        {
            double ratio = absResidual / criticalValue;
            
            if (ratio < 1.5) return BlunderSeverity.Minor;
            if (ratio < 2.0) return BlunderSeverity.Moderate;
            if (ratio < 3.0) return BlunderSeverity.Severe;
            return BlunderSeverity.Critical;
        }

        /// <summary>
        /// Test du facteur de variance a posteriori
        /// Vérifie si σ₀² est raisonnablement proche de 1
        /// </summary>
        private double CalculateVarianceFactorTest(double chi2Statistic, int degreesOfFreedom)
        {
            // Facteur de variance a posteriori
            double varianceFactor = chi2Statistic / degreesOfFreedom;
            
            Console.WriteLine($"📊 Facteur de variance: {varianceFactor:F4}");
            
            return varianceFactor;
        }

        /// <summary>
        /// Évaluation globale de la qualité de la compensation
        /// </summary>
        private string AssessOverallQuality(double sigma0Hat, bool chi2Significant, 
                                          int blunderCount, double maxStandardizedResidual)
        {
            var quality = new List<string>();
            
            // Évaluation σ₀
            if (sigma0Hat < 0.5)
                quality.Add("Précision exceptionnelle");
            else if (sigma0Hat < 1.0)
                quality.Add("Très bonne précision");
            else if (sigma0Hat < 2.0)
                quality.Add("Précision acceptable");
            else
                quality.Add("Précision insuffisante");
            
            // Test χ²
            if (!chi2Significant)
                quality.Add("Modèle stochastique validé");
            else
                quality.Add("Modèle stochastique à revoir");
            
            // Fautes
            if (blunderCount == 0)
                quality.Add("Aucune faute détectée");
            else if (blunderCount <= 2)
                quality.Add($"{blunderCount} faute(s) mineure(s)");
            else
                quality.Add($"{blunderCount} fautes - révision nécessaire");
            
            // Résidus
            if (maxStandardizedResidual < 2.0)
                quality.Add("Résidus dans les normes");
            else if (maxStandardizedResidual < 3.0)
                quality.Add("Résidus élevés mais acceptables");
            else
                quality.Add("Résidus critiques");
            
            return string.Join(" | ", quality);
        }

        /// <summary>
        /// Calcul des résidus: v = Ax̂ - l
        /// </summary>
        private Vector<double> CalculateResiduals(Matrix<double> A, Vector<double> solution, Vector<double> observations)
        {
            return A * solution - observations;
        }

        /// <summary>
        /// Génère un rapport statistique détaillé
        /// </summary>
        public string GenerateStatisticalReport(CompensationStatistics stats)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("═══════════════════════════════════════");
            report.AppendLine("    RAPPORT STATISTIQUE DÉTAILLÉ");
            report.AppendLine("═══════════════════════════════════════");
            report.AppendLine();
            
            // Statistiques de base
            report.AppendLine("📊 STATISTIQUES FONDAMENTALES:");
            report.AppendLine($"   Écart-type a posteriori (σ₀): {stats.Sigma0Hat:F6}");
            report.AppendLine($"   Degrés de liberté: {stats.DegreesOfFreedom}");
            report.AppendLine($"   Facteur de variance: {stats.VarianceFactorTest:F4}");
            report.AppendLine();
            
            // Test χ²
            report.AppendLine("🧪 TEST χ² DU POIDS UNITAIRE:");
            report.AppendLine($"   Statistique: {stats.Chi2TestStatistic:F2}");
            report.AppendLine($"   Valeur critique: {stats.Chi2CriticalValue:F2}");
            report.AppendLine($"   Résultat: {(stats.UnitWeightValid ? "✅ VALIDÉ" : "❌ REJETÉ")}");
            report.AppendLine();
            
            // Résidus
            report.AppendLine("📏 ANALYSE DES RÉSIDUS:");
            report.AppendLine($"   Résidu normalisé max: {stats.MaxStandardizedResidual:F3}");
            report.AppendLine($"   Seuil de détection: ±{stats.BlunderDetectionThreshold:F3}");
            report.AppendLine($"   RMS résidus normalisés: {Math.Sqrt(stats.StandardizedResiduals.Select(r => r*r).Average()):F3}");
            report.AppendLine();
            
            // Fautes grossières
            report.AppendLine("🔍 FAUTES GROSSIÈRES:");
            if (stats.BlundersDetected.Count == 0)
            {
                report.AppendLine("   ✅ Aucune faute détectée");
            }
            else
            {
                report.AppendLine($"   ⚠️ {stats.BlundersDetected.Count} faute(s) détectée(s):");
                foreach (var blunder in stats.BlundersDetected)
                {
                    report.AppendLine($"      • {blunder.ObservationId}: {blunder.Severity} " +
                                    $"(t={blunder.StandardizedResidual:F3}, p={blunder.SignificanceLevel:F4})");
                }
            }
            report.AppendLine();
            
            // Évaluation globale
            report.AppendLine("🎯 ÉVALUATION GLOBALE:");
            report.AppendLine($"   {stats.QualityAssessment}");
            report.AppendLine();
            
            report.AppendLine("═══════════════════════════════════════");
            
            return report.ToString();
        }

        /// <summary>
        /// Analyse de la normalité des résidus (test de Shapiro-Wilk simplifié)
        /// </summary>
        public StatisticalTestResult TestResidualNormality(double[] standardizedResiduals)
        {
            // Implémentation simplifiée - dans un projet réel, utiliser des tests plus sophistiqués
            var mean = standardizedResiduals.Average();
            var variance = standardizedResiduals.Select(r => (r - mean) * (r - mean)).Average();
            var skewness = CalculateSkewness(standardizedResiduals, mean, Math.Sqrt(variance));
            var kurtosis = CalculateKurtosis(standardizedResiduals, mean, Math.Sqrt(variance));
            
            // Test simple basé sur l'asymétrie et l'aplatissement
            bool isNormal = Math.Abs(skewness) < 1.0 && Math.Abs(kurtosis - 3.0) < 2.0;
            
            return new StatisticalTestResult
            {
                TestName = "Test de normalité (simplifié)",
                TestStatistic = Math.Max(Math.Abs(skewness), Math.Abs(kurtosis - 3.0)),
                IsSignificant = !isNormal,
                Interpretation = isNormal ? "Résidus normalement distribués" : "Distribution des résidus suspecte"
            };
        }

        private double CalculateSkewness(double[] values, double mean, double stdDev)
        {
            if (stdDev == 0) return 0;
            return values.Select(v => Math.Pow((v - mean) / stdDev, 3)).Average();
        }

        private double CalculateKurtosis(double[] values, double mean, double stdDev)
        {
            if (stdDev == 0) return 3;
            return values.Select(v => Math.Pow((v - mean) / stdDev, 4)).Average();
        }
    }
}