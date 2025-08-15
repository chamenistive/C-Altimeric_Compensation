// ============================================================================
// EXEMPLE D'INTÉGRATION COMPLÈTE - COMPENSATION ALTIMÉTRIQUE AVEC CORRECTIONS
// Démonstration de l'utilisation conjointe des trois modules
// ============================================================================

using System;
using System.Linq;
using CompensationAltimetrique.Mathematics;
using CompensationAltimetrique.Statistics;
using CompensationAltimetrique.Atmospheric;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CompensationAltimetrique.Examples
{
    /// <summary>
    /// Exemple complet intégrant calculs matriciels, analyse statistique et corrections atmosphériques
    /// </summary>
    public class IntegratedCompensationExample
    {
        /// <summary>
        /// Exemple de compensation complète avec corrections atmosphériques
        /// </summary>
        public static void RunFullCompensationWithAtmosphericCorrections()
        {
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine("  COMPENSATION COMPLÈTE AVEC CORRECTIONS");
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine();

            // Initialisation des modules
            var matrixCalculator = new AdvancedMatrixCalculator();
            var statisticalAnalyzer = new StatisticalAnalyzer(0.95);
            var atmosphericCorrector = new AtmosphericCorrector();

            try
            {
                // ÉTAPE 1: DÉFINITION DU RÉSEAU ET CONDITIONS
                Console.WriteLine("📐 ÉTAPE 1: DÉFINITION DU RÉSEAU");
                
                // Réseau de nivellement avec distances et observations brutes
                var networkData = new[]
                {
                    (from: "A", to: "B", distance: 120.0, rawDeltaH: 2.547),
                    (from: "A", to: "C", distance: 85.0,  rawDeltaH: 1.823),
                    (from: "A", to: "D", distance: 200.0, rawDeltaH: 3.156),
                    (from: "B", to: "C", distance: 150.0, rawDeltaH: -0.721),
                    (from: "B", to: "D", distance: 180.0, rawDeltaH: 0.611),
                    (from: "C", to: "D", distance: 135.0, rawDeltaH: 1.335),
                    (from: "A", to: "E", distance: 300.0, rawDeltaH: 4.892),  // Observation supplémentaire
                    (from: "C", to: "E", distance: 250.0, rawDeltaH: 3.045)   // Observation supplémentaire
                };

                // Conditions atmosphériques (région France, conditions estivales)
                var atmosphericConditions = new AtmosphericConditions(22.0, 1015.0, 58.0, "temperate")
                {
                    WeatherCondition = "stable"
                };

                Console.WriteLine($"   Points: A (référence), B, C, D, E");
                Console.WriteLine($"   Observations: {networkData.Length}");
                Console.WriteLine($"   Conditions: T={atmosphericConditions.TemperatureCelsius}°C, " +
                                $"P={atmosphericConditions.PressureHPa}hPa, H={atmosphericConditions.HumidityPercent}%");
                Console.WriteLine();

                // ÉTAPE 2: CORRECTIONS ATMOSPHÉRIQUES
                Console.WriteLine("🌡️ ÉTAPE 2: CORRECTIONS ATMOSPHÉRIQUES");
                
                var observationsForCorrection = networkData.Select(d => (d.distance, d.rawDeltaH)).ToList();
                var atmosphericCorrections = atmosphericCorrector.ApplyCorrections(observationsForCorrection, atmosphericConditions);
                
                // Générer rapport des corrections
                var correctionReport = atmosphericCorrector.GenerateCorrectionReport(atmosphericCorrections, atmosphericConditions);
                
                Console.WriteLine($"   Corrections appliquées: {atmosphericCorrections.Count}");
                Console.WriteLine($"   Correction totale: {correctionReport.TotalCorrectionMm:F2}mm");
                Console.WriteLine($"   Correction maximale: {correctionReport.MaxCorrectionMm:F2}mm");
                Console.WriteLine($"   Corrections significatives: {correctionReport.SignificantCorrections}/{atmosphericCorrections.Count}");
                Console.WriteLine();

                // ÉTAPE 3: CONSTRUCTION DU SYSTÈME MATRICIEL AVEC OBSERVATIONS CORRIGÉES
                Console.WriteLine("📊 ÉTAPE 3: CONSTRUCTION DU SYSTÈME MATRICIEL");

                // Matrice de conception A (8 observations, 4 inconnues: HB, HC, HD, HE)
                var A = DenseMatrix.OfArray(new double[,]
                {
                    { 1,  0,  0,  0}, // A→B: HB - HA
                    { 0,  1,  0,  0}, // A→C: HC - HA  
                    { 0,  0,  1,  0}, // A→D: HD - HA
                    {-1,  1,  0,  0}, // B→C: HC - HB
                    {-1,  0,  1,  0}, // B→D: HD - HB
                    { 0, -1,  1,  0}, // C→D: HD - HC
                    { 0,  0,  0,  1}, // A→E: HE - HA
                    { 0,  1,  0, -1}  // C→E: HE - HC
                });

                // Observations corrigées
                var correctedObservations = DenseVector.OfArray(
                    atmosphericCorrections.Select(c => c.CorrectedDeltaH).ToArray()
                );

                // Matrice de poids (pondération selon distance et conditions)
                var P = CreateWeightMatrix(networkData, atmosphericCorrections);

                Console.WriteLine($"   Matrice A: {A.RowCount}×{A.ColumnCount}");
                Console.WriteLine($"   Degrés de liberté: {A.RowCount - A.ColumnCount}");
                Console.WriteLine($"   Matrice de poids construite selon les distances");
                Console.WriteLine();

                // ÉTAPE 4: RÉSOLUTION MATRICIELLE
                Console.WriteLine("🔧 ÉTAPE 4: RÉSOLUTION MATRICIELLE");
                
                var solutionResult = matrixCalculator.SolveOptimal(A, P, correctedObservations);
                
                Console.WriteLine($"   Méthode: {solutionResult.MethodUsed}");
                Console.WriteLine($"   Conditionnement: {solutionResult.ConditionNumber:E2}");
                Console.WriteLine($"   Stabilité: {(solutionResult.IsStable ? "✅" : "❌")}");
                Console.WriteLine();

                // ÉTAPE 5: ANALYSE STATISTIQUE
                Console.WriteLine("📈 ÉTAPE 5: ANALYSE STATISTIQUE");
                
                var statistics = statisticalAnalyzer.AnalyzeCompensation(
                    A, P, solutionResult.Solution.Column(0), correctedObservations, solutionResult.CovarianceMatrix);

                Console.WriteLine($"   σ₀ a posteriori: {statistics.Sigma0Hat:F4}");
                Console.WriteLine($"   Test χ² poids unitaire: {(statistics.UnitWeightValid ? "✅ VALIDÉ" : "❌ REJETÉ")}");
                Console.WriteLine($"   Facteur de variance: {statistics.VarianceFactorTest:F4}");
                Console.WriteLine($"   Fautes détectées: {statistics.BlundersDetected.Count}");
                Console.WriteLine();

                // ÉTAPE 6: RÉSULTATS FINAUX
                Console.WriteLine("🎯 ÉTAPE 6: RÉSULTATS FINAUX");
                
                var estimatedHeights = solutionResult.Solution.Column(0);
                var points = new[] { "B", "C", "D", "E" };
                
                Console.WriteLine("   ALTITUDES COMPENSÉES (référence HA = 100.000m):");
                for (int i = 0; i < estimatedHeights.Count; i++)
                {
                    double precision = statistics.Sigma0Hat * Math.Sqrt(solutionResult.CovarianceMatrix[i, i]);
                    Console.WriteLine($"     H{points[i]}: {100 + estimatedHeights[i]:F3} ± {precision * 1000:F1}mm");
                }
                Console.WriteLine();

                // ÉTAPE 7: ÉVALUATIONS CROISÉES
                Console.WriteLine("🔍 ÉTAPE 7: ÉVALUATIONS CROISÉES");
                
                // Impact des corrections atmosphériques sur la précision
                double atmosphericImpact = atmosphericCorrector.EstimateImpactOnPrecision(atmosphericCorrections);
                Console.WriteLine($"   Impact corrections atmosphériques: {atmosphericImpact:F2}mm RMS");
                
                // Test de significativité des corrections
                int significantAtmospheric = atmosphericCorrections.Count(c => Math.Abs(c.TotalCorrectionMm) > 0.5);
                Console.WriteLine($"   Corrections >0.5mm: {significantAtmospheric}/{atmosphericCorrections.Count}");
                
                // Évaluation globale intégrée
                string integratedQuality = AssessIntegratedQuality(statistics, correctionReport, atmosphericImpact);
                Console.WriteLine($"   Évaluation intégrée: {integratedQuality}");
                Console.WriteLine();

                // ÉTAPE 8: RAPPORTS DÉTAILLÉS
                Console.WriteLine("📋 ÉTAPE 8: RAPPORTS DÉTAILLÉS");
                
                // Rapport statistique
                Console.WriteLine(statisticalAnalyzer.GenerateStatisticalReport(statistics));
                
                // Rapport corrections atmosphériques
                Console.WriteLine(atmosphericCorrector.GenerateDetailedReport(correctionReport));

                // Analyse de sensibilité pour validation
                Console.WriteLine("🧪 ANALYSE DE SENSIBILITÉ:");
                var sensitivity = atmosphericCorrector.AnalyzeSensitivity(200.0, 2.0);
                foreach (var kvp in sensitivity)
                {
                    Console.WriteLine($"   {kvp.Key}: {kvp.Value:F3}");
                }
                Console.WriteLine();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de la compensation: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Détail: {ex.InnerException.Message}");
                }
            }
            
            Console.WriteLine("═══════════════════════════════════════════════");
        }

        /// <summary>
        /// Crée une matrice de poids basée sur les distances et corrections
        /// </summary>
        private static Matrix<double> CreateWeightMatrix(
            (string from, string to, double distance, double rawDeltaH)[] networkData,
            System.Collections.Generic.List<AtmosphericCorrection> corrections)
        {
            int n = networkData.Length;
            var weights = new double[n];

            for (int i = 0; i < n; i++)
            {
                // Poids inversement proportionnel à la distance
                // Plus la distance est grande, moins l'observation est précise
                double baseWeight = 1.0 / networkData[i].distance;
                
                // Ajustement selon l'amplitude de la correction atmosphérique
                double correctionFactor = 1.0 / (1.0 + Math.Abs(corrections[i].TotalCorrectionMm) / 10.0);
                
                weights[i] = baseWeight * correctionFactor;
            }

            // Normalisation pour éviter des poids trop extrêmes
            double maxWeight = weights.Max();
            for (int i = 0; i < n; i++)
            {
                weights[i] = weights[i] / maxWeight * 10.0; // Normalisation à [0-10]
            }

            var diagonalMatrix = DenseMatrix.Create(n, n, 0.0);
            for (int i = 0; i < n; i++)
            {
                diagonalMatrix[i, i] = weights[i];
            }
            return diagonalMatrix;
        }

        /// <summary>
        /// Évalue la qualité intégrée de la compensation
        /// </summary>
        private static string AssessIntegratedQuality(
            CompensationStatistics statistics, 
            AtmosphericCorrectionReport correctionReport,
            double atmosphericImpact)
        {
            var qualityFactors = new System.Collections.Generic.List<string>();

            // Qualité statistique
            if (statistics.Sigma0Hat < 1.0 && statistics.UnitWeightValid)
                qualityFactors.Add("Excellente qualité statistique");
            else if (statistics.Sigma0Hat < 2.0)
                qualityFactors.Add("Bonne qualité statistique");
            else
                qualityFactors.Add("Qualité statistique à améliorer");

            // Impact des corrections atmosphériques
            if (atmosphericImpact < 0.5)
                qualityFactors.Add("Corrections atmosphériques mineures");
            else if (atmosphericImpact < 2.0)
                qualityFactors.Add("Corrections atmosphériques modérées");
            else
                qualityFactors.Add("Corrections atmosphériques importantes");

            // Cohérence des corrections
            if (correctionReport.SignificantCorrections < correctionReport.Corrections.Count * 0.3)
                qualityFactors.Add("Impact atmosphérique faible");
            else
                qualityFactors.Add("Impact atmosphérique significatif");

            // Précision finale estimée
            double finalPrecisionMm = Math.Sqrt(statistics.Sigma0Hat * statistics.Sigma0Hat + atmosphericImpact * atmosphericImpact) * 1000;
            if (finalPrecisionMm < 2.0)
                qualityFactors.Add("Précision sub-millimétrique");
            else if (finalPrecisionMm < 5.0)
                qualityFactors.Add("Précision millimétrique");
            else
                qualityFactors.Add("Précision centimétrique");

            return string.Join(" | ", qualityFactors);
        }

        /// <summary>
        /// Exemple de comparaison avec/sans corrections atmosphériques
        /// </summary>
        public static void RunComparisonWithWithoutCorrections()
        {
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine("  COMPARAISON AVEC/SANS CORRECTIONS ATMOSPHÉRIQUES");
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine();

            var matrixCalculator = new AdvancedMatrixCalculator();
            var statisticalAnalyzer = new StatisticalAnalyzer(0.95);
            var atmosphericCorrector = new AtmosphericCorrector();

            // Données de test avec distances importantes
            var observations = new[]
            {
                (400.0, 3.245),  // Distance importante pour accentuer l'effet
                (350.0, -2.156),
                (450.0, 4.892),
                (300.0, 1.334),
                (500.0, 2.667)   // Distance limite
            };

            var conditions = new AtmosphericConditions(25.0, 1008.0, 75.0, "temperate");

            // Matrice simple pour test (5 obs, 2 inconnues)
            var A = DenseMatrix.OfArray(new double[,]
            {
                {1, 0}, {0, 1}, {1, 1}, {2, -1}, {1, 2}
            });
            var P = DenseMatrix.CreateIdentity(5);

            Console.WriteLine("📏 CALCUL SANS CORRECTIONS:");
            
            // Sans corrections
            var rawObservations = DenseVector.OfArray(observations.Select(o => o.Item2).ToArray());
            var resultWithoutCorrections = matrixCalculator.SolveOptimal(A, P, rawObservations);
            var statsWithout = statisticalAnalyzer.AnalyzeCompensation(A, P, 
                resultWithoutCorrections.Solution.Column(0), rawObservations, 
                resultWithoutCorrections.CovarianceMatrix);

            Console.WriteLine($"   σ₀ sans corrections: {statsWithout.Sigma0Hat:F4}");
            Console.WriteLine($"   Solution: [{resultWithoutCorrections.Solution[0,0]:F3}, {resultWithoutCorrections.Solution[1,0]:F3}]");
            Console.WriteLine();

            Console.WriteLine("🌡️ CALCUL AVEC CORRECTIONS:");
            
            // Avec corrections
            var corrections = atmosphericCorrector.ApplyCorrections(observations, conditions);
            var correctedObservations = DenseVector.OfArray(corrections.Select(c => c.CorrectedDeltaH).ToArray());
            var resultWithCorrections = matrixCalculator.SolveOptimal(A, P, correctedObservations);
            var statsWith = statisticalAnalyzer.AnalyzeCompensation(A, P,
                resultWithCorrections.Solution.Column(0), correctedObservations,
                resultWithCorrections.CovarianceMatrix);

            Console.WriteLine($"   σ₀ avec corrections: {statsWith.Sigma0Hat:F4}");
            Console.WriteLine($"   Solution: [{resultWithCorrections.Solution[0,0]:F3}, {resultWithCorrections.Solution[1,0]:F3}]");
            Console.WriteLine();

            Console.WriteLine("📊 COMPARAISON DES RÉSULTATS:");
            var deltaParam1 = Math.Abs(resultWithCorrections.Solution[0,0] - resultWithoutCorrections.Solution[0,0]);
            var deltaParam2 = Math.Abs(resultWithCorrections.Solution[1,0] - resultWithoutCorrections.Solution[1,0]);
            var deltaSigma0 = Math.Abs(statsWith.Sigma0Hat - statsWithout.Sigma0Hat);

            Console.WriteLine($"   Différence paramètre 1: {deltaParam1*1000:F1}mm");
            Console.WriteLine($"   Différence paramètre 2: {deltaParam2*1000:F1}mm");
            Console.WriteLine($"   Différence σ₀: {deltaSigma0:F4}");
            Console.WriteLine($"   Impact maximal: {corrections.Max(c => Math.Abs(c.TotalCorrectionMm)):F2}mm");
            
            if (deltaParam1 > 0.002 || deltaParam2 > 0.002)
                Console.WriteLine("   ✅ Corrections significatives - Application recommandée");
            else
                Console.WriteLine("   ℹ️ Corrections mineures pour ces distances");

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════");
        }
    }
}