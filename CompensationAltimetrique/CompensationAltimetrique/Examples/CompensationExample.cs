// ============================================================================
// EXEMPLE D'UTILISATION - COMPENSATION ALTIMÉTRIQUE COMPLÈTE
// Démonstration de l'intégration entre calculs matriciels et analyse statistique
// ============================================================================

using System;
using CompensationAltimetrique.Mathematics;
using CompensationAltimetrique.Statistics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CompensationAltimetrique.Examples
{
    /// <summary>
    /// Exemple complet de compensation altimétrique avec analyse statistique
    /// </summary>
    public class CompensationExample
    {
        /// <summary>
        /// Exemple de réseau de nivellement simple
        /// </summary>
        public static void RunSimpleNetworkExample()
        {
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine("    EXEMPLE: RÉSEAU DE NIVELLEMENT SIMPLE");
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine();

            // Données du réseau
            // Points: A (fixe), B, C, D
            // Observations: dénivelées entre points
            
            var matrixCalculator = new AdvancedMatrixCalculator();
            var statisticalAnalyzer = new StatisticalAnalyzer(0.95);

            // Matrice de conception A (6 observations, 3 inconnues: HB, HC, HD)
            // HA est fixe (référence)
            var A = DenseMatrix.OfArray(new double[,]
            {
                { 1,  0,  0}, // A→B: HB - HA
                { 0,  1,  0}, // A→C: HC - HA  
                { 0,  0,  1}, // A→D: HD - HA
                {-1,  1,  0}, // B→C: HC - HB
                {-1,  0,  1}, // B→D: HD - HB
                { 0, -1,  1}  // C→D: HD - HC
            });

            // Observations (dénivelées en mètres)
            var observations = DenseVector.OfArray(new double[]
            {
                 2.547,  // A→B
                 1.823,  // A→C
                 3.156,  // A→D
                -0.721,  // B→C
                 0.611,  // B→D
                 1.335   // C→D
            });

            // Matrice de poids (précision égale pour toutes les observations)
            var P = DenseMatrix.CreateIdentity(6);

            Console.WriteLine("📐 Résolution du système par moindres carrés...");
            
            try
            {
                // 1. RÉSOLUTION MATRICIELLE
                var solutionResult = matrixCalculator.SolveOptimal(A, P, observations);
                
                Console.WriteLine($"   Méthode utilisée: {solutionResult.MethodUsed}");
                Console.WriteLine($"   Conditionnement: {solutionResult.ConditionNumber:E2}");
                Console.WriteLine($"   Stabilité: {(solutionResult.IsStable ? "✅" : "❌")}");
                Console.WriteLine();
                
                // Extraction de la solution
                var estimatedHeights = solutionResult.Solution.Column(0);
                
                Console.WriteLine("🏔️ ALTITUDES ESTIMÉES:");
                Console.WriteLine($"   HA (référence): 100.000 m");
                Console.WriteLine($"   HB: {100 + estimatedHeights[0]:F3} m");
                Console.WriteLine($"   HC: {100 + estimatedHeights[1]:F3} m");
                Console.WriteLine($"   HD: {100 + estimatedHeights[2]:F3} m");
                Console.WriteLine();

                // 2. ANALYSE STATISTIQUE
                Console.WriteLine("📊 ANALYSE STATISTIQUE:");
                
                var statistics = statisticalAnalyzer.AnalyzeCompensation(
                    A, P, estimatedHeights, observations, solutionResult.CovarianceMatrix);
                
                // Affichage des statistiques principales
                Console.WriteLine($"   Écart-type a posteriori (σ₀): {statistics.Sigma0Hat:F4}");
                Console.WriteLine($"   Degrés de liberté: {statistics.DegreesOfFreedom}");
                Console.WriteLine($"   Test χ² poids unitaire: {(statistics.UnitWeightValid ? "✅ VALIDÉ" : "❌ REJETÉ")}");
                Console.WriteLine($"   Facteur de variance: {statistics.VarianceFactorTest:F4}");
                Console.WriteLine();
                
                // Analyse des résidus
                Console.WriteLine("📏 ANALYSE DES RÉSIDUS:");
                Console.WriteLine($"   Résidu normalisé max: ±{statistics.MaxStandardizedResidual:F3}");
                
                if (statistics.BlundersDetected.Count > 0)
                {
                    Console.WriteLine($"   ⚠️ {statistics.BlundersDetected.Count} faute(s) grossière(s) détectée(s):");
                    foreach (var blunder in statistics.BlundersDetected)
                    {
                        Console.WriteLine($"      • {blunder.ObservationId}: {blunder.Severity} " +
                                        $"(résidu normalisé: {blunder.StandardizedResidual:F3})");
                    }
                }
                else
                {
                    Console.WriteLine("   ✅ Aucune faute grossière détectée");
                }
                Console.WriteLine();
                
                // Évaluation globale
                Console.WriteLine("🎯 ÉVALUATION GLOBALE:");
                Console.WriteLine($"   {statistics.QualityAssessment}");
                Console.WriteLine();
                
                // Calcul des précisions
                Console.WriteLine("🎯 PRÉCISIONS ESTIMÉES:");
                for (int i = 0; i < estimatedHeights.Count; i++)
                {
                    double precision = statistics.Sigma0Hat * Math.Sqrt(solutionResult.CovarianceMatrix[i, i]);
                    char point = (char)('B' + i);
                    Console.WriteLine($"   Précision H{point}: ±{precision * 1000:F1} mm");
                }
                Console.WriteLine();
                
                // Rapport détaillé
                Console.WriteLine("📋 RAPPORT STATISTIQUE DÉTAILLÉ:");
                Console.WriteLine(statisticalAnalyzer.GenerateStatisticalReport(statistics));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors du calcul: {ex.Message}");
            }
            
            Console.WriteLine("═══════════════════════════════════════════════");
        }

        /// <summary>
        /// Exemple avec faute grossière intentionnelle
        /// </summary>
        public static void RunBlunderDetectionExample()
        {
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine("  EXEMPLE: DÉTECTION DE FAUTE GROSSIÈRE");
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine();

            var matrixCalculator = new AdvancedMatrixCalculator();
            var statisticalAnalyzer = new StatisticalAnalyzer(0.95);

            // Même réseau que précédemment
            var A = DenseMatrix.OfArray(new double[,]
            {
                { 1,  0,  0},
                { 0,  1,  0}, 
                { 0,  0,  1},
                {-1,  1,  0},
                {-1,  0,  1},
                { 0, -1,  1}
            });

            // Observations avec faute grossière sur la 4ème observation (B→C)
            var observations = DenseVector.OfArray(new double[]
            {
                 2.547,  // A→B
                 1.823,  // A→C
                 3.156,  // A→D
                -0.321,  // B→C (faute de +40cm par rapport à -0.721)
                 0.611,  // B→D
                 1.335   // C→D
            });

            var P = DenseMatrix.CreateIdentity(6);

            Console.WriteLine("🚨 Observation B→C contient une faute grossière de +40cm");
            Console.WriteLine();

            try
            {
                var solutionResult = matrixCalculator.SolveOptimal(A, P, observations);
                var statistics = statisticalAnalyzer.AnalyzeCompensation(
                    A, P, solutionResult.Solution.Column(0), observations, solutionResult.CovarianceMatrix);

                Console.WriteLine("📊 RÉSULTATS DE LA DÉTECTION:");
                Console.WriteLine($"   σ₀ a posteriori: {statistics.Sigma0Hat:F4} (élevé à cause de la faute)");
                Console.WriteLine($"   Test χ² poids unitaire: {(statistics.UnitWeightValid ? "✅ VALIDÉ" : "❌ REJETÉ")}");
                Console.WriteLine($"   Facteur de variance: {statistics.VarianceFactorTest:F4}");
                Console.WriteLine();

                if (statistics.BlundersDetected.Count > 0)
                {
                    Console.WriteLine($"✅ {statistics.BlundersDetected.Count} faute(s) détectée(s) avec succès:");
                    foreach (var blunder in statistics.BlundersDetected)
                    {
                        string obsName = blunder.ObservationIndex switch
                        {
                            0 => "A→B",
                            1 => "A→C", 
                            2 => "A→D",
                            3 => "B→C", // Celle avec la faute
                            4 => "B→D",
                            5 => "C→D",
                            _ => $"Obs_{blunder.ObservationIndex + 1}"
                        };
                        
                        Console.WriteLine($"   🎯 {obsName}: {blunder.Severity}");
                        Console.WriteLine($"      Résidu normalisé: {blunder.StandardizedResidual:F3}");
                        Console.WriteLine($"      Statistique t: {blunder.TestStatistic:F3}");
                        Console.WriteLine($"      Seuil critique: {blunder.CriticalValue:F3}");
                        Console.WriteLine($"      Niveau signification: {blunder.SignificanceLevel:F6}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Faute non détectée (seuils peut-être trop élevés)");
                    Console.WriteLine($"    Résidu normalisé max: {statistics.MaxStandardizedResidual:F3}");
                    Console.WriteLine($"    Seuil détection: {statistics.BlunderDetectionThreshold:F3}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur: {ex.Message}");
            }
            
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════");
        }
    }
}