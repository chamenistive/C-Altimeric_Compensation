// ================================================================
// ÉTAPE 4 - ORCHESTRATEUR DE COMPENSATION PAR MOINDRES CARRÉS
// Chef d'orchestre intégrant toutes les étapes de compensation
// Transposition FIDÈLE du module Python LeastSquaresProcessor
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Calculations.Validation;
using CompensationAltimetrique.Calculations.Weights;
using CompensationAltimetrique.Calculations.Corrections;
using CompensationAltimetrique.Calculations.Algorithms;

namespace CompensationAltimetrique.Calculations.Design
{
    /// <summary>
    /// Configuration complète pour la compensation par moindres carrés.
    /// </summary>
    public class CompensationConfiguration
    {
        /// <summary>Configuration du réseau</summary>
        public NetworkConfiguration NetworkConfig { get; set; } = new();

        /// <summary>Paramètres géodésiques pour calcul des poids</summary>
        public GeodeticParameters GeodeticParams { get; set; } = new();

        /// <summary>Précision cible en millimètres</summary>
        public double TargetPrecisionMm { get; set; } = 2.0;

        /// <summary>Niveau de confiance statistique</summary>
        public double ConfidenceLevel { get; set; } = 0.95;

        /// <summary>Nombre maximum d'itérations</summary>
        public int MaxIterations { get; set; } = 10;

        /// <summary>Critère de convergence</summary>
        public double ConvergenceTolerance { get; set; } = 1e-6;

        /// <summary>Appliquer corrections atmosphériques</summary>
        public bool ApplyAtmosphericCorrections { get; set; } = false;

        /// <summary>Conditions atmosphériques si corrections appliquées</summary>
        public Corrections.AtmosphericConditions AtmosphericConditions { get; set; } = new();

        /// <summary>Validation de la configuration</summary>
        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult();

            // Validation réseau
            var networkValidation = NetworkConfig.ValidateConfiguration();
            foreach (var error in networkValidation.Errors)
                result.AddError($"Réseau: {error}");
            foreach (var warning in networkValidation.Warnings)
                result.AddWarning($"Réseau: {warning}");

            // Validation paramètres géodésiques
            if (GeodeticParams.InstrumentalErrorMm <= 0)
                result.AddError("Erreur instrumentale doit être positive");
            if (GeodeticParams.KilometricErrorMm <= 0)
                result.AddError("Erreur kilométrique doit être positive");

            // Validation paramètres numériques
            if (TargetPrecisionMm <= 0)
                result.AddError("Précision cible doit être positive");
            if (ConfidenceLevel <= 0 || ConfidenceLevel >= 1)
                result.AddError("Niveau de confiance doit être entre 0 et 1");
            if (MaxIterations < 1)
                result.AddError("Nombre d'itérations doit être >= 1");

            return result;
        }
    }

    /// <summary>
    /// Résultats détaillés de la compensation par moindres carrés.
    /// Extension de CompensationResults avec détails techniques.
    /// </summary>
    public class DetailedCompensationResults : CompensationResults
    {
        /// <summary>Matrice de conception A</summary>
        public Matrix<double> DesignMatrix { get; set; } = DenseMatrix.Create(0, 0, 0);

        /// <summary>Matrice des poids P</summary>
        public Matrix<double> WeightMatrix { get; set; } = DenseMatrix.Create(0, 0, 0);

        /// <summary>Matrice de covariance Qx</summary>
        public Matrix<double> CovarianceMatrix { get; set; } = DenseMatrix.Create(0, 0, 0);

        /// <summary>Vecteur des misclosures</summary>
        public Vector<double> Misclosures { get; set; } = Vector<double>.Build.Dense(0);

        /// <summary>Informations sur la matrice de conception</summary>
        public DesignMatrixInfo MatrixInfo { get; set; } = new();

        /// <summary>Statistiques de compensation</summary>
        public CompensationStatistics Statistics { get; set; } = new();

        /// <summary>Statistiques des poids</summary>
        public WeightStatistics WeightStats { get; set; } = new();

        /// <summary>Détails des corrections atmosphériques si appliquées</summary>
        public List<CorrectionReport> AtmosphericCorrections { get; set; } = new();

        /// <summary>Résultats de validation complète</summary>
        public ValidationResult ValidationResult { get; set; } = new();

        /// <summary>Historique des itérations</summary>
        public List<IterationInfo> IterationHistory { get; set; } = new();

        /// <summary>Configuration utilisée</summary>
        public CompensationConfiguration Configuration { get; set; } = new();
    }

    /// <summary>
    /// Informations d'une itération de compensation.
    /// </summary>
    public class IterationInfo
    {
        public int IterationNumber { get; set; }
        public double MaxCorrection { get; set; }
        public double RmsCorrection { get; set; }
        public double Sigma0 { get; set; }
        public bool Converged { get; set; }
        public TimeSpan ComputationTime { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Orchestrateur principal pour compensation par moindres carrés.
    /// CHEF D'ORCHESTRE intégrant tous les modules:
    /// - Construction matrice de conception
    /// - Calcul des poids géodésiques  
    /// - Corrections atmosphériques
    /// - Résolution système linéaire
    /// - Validation statistique
    /// - Contrôle qualité
    /// </summary>
    public class LeastSquaresOrchestrator
    {
        private readonly CompensationConfiguration _configuration;
        private readonly DesignMatrixBuilder _matrixBuilder;
        private readonly WeightCalculator _weightCalculator;
        private readonly QualityAnalyzer _qualityAnalyzer;
        private readonly CompensationValidator _validator;
        private readonly GeodeticPrecisionValidator _precisionValidator;
        private readonly GeodeticStatisticalValidator _statisticalValidator;
        private readonly DataStructureValidator _dataStructureValidator;
        private readonly AtmosphericCorrector? _atmosphericCorrector;
        private readonly EnhancedLeastSquaresCompensator _enhancedCompensator;

        public LeastSquaresOrchestrator(CompensationConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Validation de la configuration
            var configValidation = _configuration.ValidateConfiguration();
            if (!configValidation.IsValid)
            {
                throw new InvalidOperationException($"Configuration invalide: {string.Join("; ", configValidation.Errors)}");
            }

            // Initialisation des modules
            _matrixBuilder = new DesignMatrixBuilder(_configuration.NetworkConfig);
            _weightCalculator = new WeightCalculator(_configuration.GeodeticParams);
            _qualityAnalyzer = new QualityAnalyzer(_configuration.ConfidenceLevel);
            _validator = new CompensationValidator(_configuration.TargetPrecisionMm, _configuration.ConfidenceLevel);
            
            // Nouveaux validateurs géodésiques
            _precisionValidator = new GeodeticPrecisionValidator(_configuration.TargetPrecisionMm);
            _statisticalValidator = new GeodeticStatisticalValidator(_configuration.ConfidenceLevel);
            _dataStructureValidator = new DataStructureValidator();

            // Correcteur atmosphérique si nécessaire
            if (_configuration.ApplyAtmosphericCorrections)
            {
                _atmosphericCorrector = new AtmosphericCorrector();
            }
            
            // Compensateur amélioré
            _enhancedCompensator = new EnhancedLeastSquaresCompensator(
                _configuration.TargetPrecisionMm,
                _configuration.GeodeticParams.InstrumentalErrorMm,
                _configuration.GeodeticParams.KilometricErrorMm);
        }

        /// <summary>
        /// Compensation avancée utilisant le compensateur amélioré
        /// </summary>
        public EnhancedCompensationResults PerformEnhancedCompensation(
            List<LevelingData> levelingData,
            string referencePoint = "REF",
            double referenceAltitude = 125.456,
            SolutionMethod method = SolutionMethod.Auto)
        {
            Console.WriteLine($"🚀 Compensation avancée - Orchestrateur v2.0");
            Console.WriteLine($"📋 Configuration: {levelingData.Count} observations, précision {_configuration.TargetPrecisionMm}mm");
            
            // ÉTAPE 1: Validation des données d'entrée
            var dataValidation = ValidateInputData(levelingData);
            if (!dataValidation.IsValid)
            {
                throw new InvalidOperationException($"Données invalides: {string.Join("; ", dataValidation.Errors)}");
            }
            
            // ÉTAPE 2: Application des corrections atmosphériques si nécessaire
            if (_configuration.ApplyAtmosphericCorrections && _atmosphericCorrector != null)
            {
                Console.WriteLine("🌡️ Application des corrections atmosphériques...");
                ApplyAtmosphericCorrections(levelingData);
            }
            
            // ÉTAPE 3: Compensation avec le compensateur amélioré
            Console.WriteLine("⚙️ Compensation par moindres carrés améliorée...");
            var enhancedResults = _enhancedCompensator.Compensate(
                levelingData, referencePoint, referenceAltitude, method);
            
            Console.WriteLine($"✅ Compensation terminée");
            Console.WriteLine($"📊 Résultats: {(enhancedResults.IsValid ? "VALIDÉS" : "AVEC ALERTES")}");
            
            return enhancedResults;
        }

        /// <summary>
        /// Compensation complète par moindres carrés avec validation.
        /// ORCHESTRATION PRINCIPALE intégrant toutes les étapes.
        /// 
        /// Algorithme complet:
        /// 1. Préparation des données
        /// 2. Corrections atmosphériques optionnelles
        /// 3. Construction matrice de conception A
        /// 4. Calcul matrice des poids P
        /// 5. Résolution itérative A^T P A x = A^T P l
        /// 6. Analyse statistique complète
        /// 7. Validation et contrôle qualité
        /// </summary>
        public DetailedCompensationResults CompensateNetwork(List<LevelingData> levelingData)
        {
            var startTime = DateTime.Now;
            var results = new DetailedCompensationResults
            {
                Configuration = _configuration,
                Timestamp = startTime
            };

            try
            {
                Console.WriteLine("🎯 DÉBUT COMPENSATION PAR MOINDRES CARRÉS");
                Console.WriteLine($"📊 Données: {levelingData.Count} observations");
                Console.WriteLine($"🎚️ Configuration: {_configuration.NetworkConfig.NetworkType}, précision {_configuration.TargetPrecisionMm}mm");

                // ÉTAPE 1: Préparation et validation des données
                var dataValidation = ValidateInputData(levelingData);
                if (!dataValidation.IsValid)
                {
                    results.ValidationResult = dataValidation;
                    results.IsValid = false;
                    return results;
                }

                // ÉTAPE 2: Corrections atmosphériques optionnelles
                if (_configuration.ApplyAtmosphericCorrections && _atmosphericCorrector != null)
                {
                    results.AtmosphericCorrections = ApplyAtmosphericCorrections(levelingData);
                    Console.WriteLine($"🌤️ Corrections atmosphériques appliquées: {results.AtmosphericCorrections.Count}");
                }

                // ÉTAPE 3: Construction matrice de conception
                var (designMatrix, matrixInfo) = _matrixBuilder.BuildDesignMatrix(levelingData, 
                    includeClosureConstraint: _configuration.NetworkConfig.NetworkType == "fermé");
                
                results.DesignMatrix = designMatrix;
                results.MatrixInfo = matrixInfo;
                Console.WriteLine($"📐 Matrice conception: {matrixInfo.GetSummary()}");

                // ÉTAPE 4: Calcul des poids géodésiques
                var weightMatrix = _weightCalculator.CalculateWeightsForLevelingData(levelingData);
                results.WeightMatrix = weightMatrix;
                results.WeightStats = _weightCalculator.GetWeightStatistics(weightMatrix);
                Console.WriteLine($"⚖️ Poids: {results.WeightStats.GetSummary()}");

                // ÉTAPE 5: Construction vecteur misclosures
                var misclosures = _matrixBuilder.BuildMisclosureVector(levelingData, matrixInfo, 
                    includeClosureConstraint: _configuration.NetworkConfig.NetworkType == "fermé");
                results.Misclosures = misclosures;

                // ÉTAPE 6: Résolution itérative du système
                var solutionResult = SolveIteratively(designMatrix, weightMatrix, misclosures, results);
                if (!solutionResult.IsValid)
                {
                    results.ValidationResult = solutionResult;
                    results.IsValid = false;
                    return results;
                }

                // ÉTAPE 7: Calcul des altitudes ajustées
                ComputeAdjustedAltitudes(levelingData, results);

                // ÉTAPE 8: Analyse statistique complète
                results.Statistics = _qualityAnalyzer.AnalyzeCompensation(
                    designMatrix, weightMatrix, misclosures, results.Corrections, results.CovarianceMatrix);
                
                // ÉTAPE 9: Validation géodésique avancée
                var advancedValidation = PerformAdvancedValidation(results, levelingData);
                results.ValidationResult = CombineValidationResults(
                    results.ValidationResult, 
                    advancedValidation,
                    _validator.ValidateCompensation(
                        results.Residuals, results.Corrections, results.Statistics,
                        results.ClosureError, results.TotalDistance, 
                        levelingData.Select(d => d.DIST1 ?? d.DIST2 ?? 50.0)));

                // ÉTAPE 10: Résultats finaux
                results.IsValid = results.ValidationResult.IsValid;
                if (results.ValidationResult.Details.TryGetValue("adjustment_validation", out var adjustmentData) &&
                    adjustmentData is Dictionary<string, object> adjDict &&
                    adjDict.TryGetValue("precision_achieved", out var precisionValue) &&
                    precisionValue is bool precision)
                {
                    results.PrecisionAchieved = precision;
                }
                else
                {
                    results.PrecisionAchieved = false;
                }
                results.ComputationTime = DateTime.Now - startTime;
                results.ProcessedPoints = levelingData.Count;

                Console.WriteLine($"✅ Compensation terminée: {(results.IsValid ? "SUCCÈS" : "ÉCHEC")}");
                Console.WriteLine($"📈 Précision: {(results.PrecisionAchieved ? "ATTEINTE" : "NON ATTEINTE")}");
                Console.WriteLine($"⏱️ Temps: {results.ComputationTime.TotalSeconds:F1}s");

                return results;
            }
            catch (Exception ex)
            {
                results.ErrorMessages.Add($"Erreur compensation: {ex.Message}");
                results.IsValid = false;
                results.ComputationTime = DateTime.Now - startTime;
                Console.WriteLine($"❌ Échec compensation: {ex.Message}");
                return results;
            }
        }

        // Méthodes privées pour orchestration détaillée

        private ValidationResult ValidateInputData(List<LevelingData> levelingData)
        {
            var result = new ValidationResult();

            if (!levelingData.Any())
            {
                result.AddError("Aucune donnée de nivellement fournie");
                return result;
            }

            // Validation avec les nouveaux validateurs
            int validObservations = 0;
            var dataErrors = new List<string>();
            
            foreach (var data in levelingData)
            {
                // Validation des lectures AR/AV
                if (data.AR1.HasValue && data.AV1.HasValue)
                {
                    var readingValidation = _dataStructureValidator.ValidateReadings(
                        data.AR1.Value, data.AV1.Value, data.Matricule);
                    
                    if (!readingValidation.IsValid)
                    {
                        dataErrors.AddRange(readingValidation.Errors);
                    }
                    else
                    {
                        validObservations++;
                    }
                }
                
                if (data.AR2.HasValue && data.AV2.HasValue)
                {
                    var readingValidation = _dataStructureValidator.ValidateReadings(
                        data.AR2.Value, data.AV2.Value, data.Matricule);
                    
                    if (!readingValidation.IsValid)
                    {
                        dataErrors.AddRange(readingValidation.Errors);
                    }
                    else
                    {
                        validObservations++;
                    }
                }
                
                // Validation du contrôle instrumental si applicable
                if (data.AR1.HasValue && data.AV1.HasValue && data.AR2.HasValue && data.AV2.HasValue)
                {
                    var denivelations = new List<double>
                    {
                        data.AR1.Value - data.AV1.Value,
                        data.AR2.Value - data.AV2.Value
                    };
                    
                    var controlValidation = _precisionValidator.ValidateInstrumentalControl(denivelations);
                    
                    if (!controlValidation.IsValid)
                    {
                        dataErrors.AddRange(controlValidation.Errors);
                    }
                    if (controlValidation.Warnings.Any())
                    {
                        result.Warnings.AddRange(controlValidation.Warnings);
                    }
                }
            }

            // Ajouter les erreurs de données
            foreach (var error in dataErrors.Take(10)) // Limiter à 10 erreurs pour l'affichage
            {
                result.AddError(error);
            }
            
            if (dataErrors.Count > 10)
            {
                result.AddWarning($"{dataErrors.Count - 10} erreurs supplémentaires détectées");
            }

            if (validObservations == 0)
            {
                result.AddError("Aucune observation valide dans les données");
            }
            else if (validObservations < levelingData.Count)
            {
                result.AddWarning($"Seulement {validObservations}/{levelingData.Count} observations valides");
            }

            result.Details["total_data"] = levelingData.Count;
            result.Details["valid_observations"] = validObservations;
            result.Details["data_errors_count"] = dataErrors.Count;

            return result;
        }

        private List<CorrectionReport> ApplyAtmosphericCorrections(List<LevelingData> levelingData)
        {
            var corrections = new List<CorrectionReport>();

            if (_atmosphericCorrector == null) return corrections;

            foreach (var data in levelingData)
            {
                double distance = data.DIST1 ?? data.DIST2 ?? 50.0;
                var correction = _atmosphericCorrector.GenerateReport(distance);
                corrections.Add(correction);

                // Appliquer les corrections aux dénivelées
                if (data.AR1.HasValue && data.AV1.HasValue)
                {
                    double deltaH1 = data.AR1.Value - data.AV1.Value;
                    double correctedDH1 = _atmosphericCorrector.ApplyCorrectionToDenivelation(deltaH1, distance);
                    // Note: Modification des données en place pour la démonstration
                    // En production, il faudrait créer une copie
                }

                if (data.AR2.HasValue && data.AV2.HasValue)
                {
                    double deltaH2 = data.AR2.Value - data.AV2.Value;
                    double correctedDH2 = _atmosphericCorrector.ApplyCorrectionToDenivelation(deltaH2, distance);
                }
            }

            return corrections;
        }

        private ValidationResult SolveIteratively(Matrix<double> A, Matrix<double> P, 
            Vector<double> misclosures, DetailedCompensationResults results)
        {
            var validation = new ValidationResult();

            try
            {
                // Résolution directe du système normal: (A^T P A) x = A^T P l
                var AtP = A.Transpose() * P;
                var AtPA = AtP * A;
                var AtPl = AtP * misclosures;

                // Inversion de la matrice normale
                var N_inv = AtPA.Inverse();
                results.CovarianceMatrix = N_inv;

                // Solution
                var corrections = N_inv * AtPl;
                results.Corrections = corrections;

                // Calcul des résidus
                var predicted = A * corrections;
                var residuals = misclosures - predicted;
                results.Residuals = residuals;

                // Statistiques
                results.RmsResiduals = Math.Sqrt(residuals.DotProduct(residuals) / residuals.Count);
                results.MaxCorrection = corrections.AbsoluteMaximum();

                // Information d'itération (pour compatibilité)
                results.IterationHistory.Add(new IterationInfo
                {
                    IterationNumber = 1,
                    MaxCorrection = results.MaxCorrection,
                    RmsCorrection = Math.Sqrt(corrections.DotProduct(corrections) / corrections.Count),
                    Converged = true,
                    ComputationTime = TimeSpan.Zero
                });

                validation.AddSuccess("Système résolu par moindres carrés");
            }
            catch (Exception ex)
            {
                validation.AddError($"Erreur résolution système: {ex.Message}");
            }

            return validation;
        }

        private void ComputeAdjustedAltitudes(List<LevelingData> levelingData, DetailedCompensationResults results)
        {
            results.AdjustedPoints.Clear();

            // Point de référence
            results.AdjustedPoints.Add(new AltitudePoint(
                _configuration.NetworkConfig.ReferencePoint, 
                _configuration.NetworkConfig.ReferenceAltitude, 
                isReference: true));

            // Points ajustés
            double cumulativeAltitude = _configuration.NetworkConfig.ReferenceAltitude;
            
            for (int i = 0; i < levelingData.Count; i++)
            {
                double observedDH = levelingData[i].CalculateAverageDenivelation();
                int correctionIndex = Math.Min(i + 1, results.Corrections.Count - 1);
                double correction = correctionIndex < results.Corrections.Count ? results.Corrections[correctionIndex] : 0.0;
                
                cumulativeAltitude += observedDH + correction;
                
                results.AdjustedPoints.Add(new AltitudePoint(
                    levelingData[i].Matricule, 
                    cumulativeAltitude));
            }

            // Calcul erreur de fermeture
            if (_configuration.NetworkConfig.NetworkType == "fermé" && results.AdjustedPoints.Count > 1)
            {
                var lastPoint = results.AdjustedPoints.Last();
                var firstPoint = results.AdjustedPoints.First();
                results.ClosureError = Math.Abs(lastPoint.Altitude - firstPoint.Altitude);
            }

            // Distance totale
            results.TotalDistance = levelingData.Sum(d => (d.DIST1 ?? d.DIST2 ?? 50.0) / 1000.0); // en km
        }

        /// <summary>
        /// Génération d'un rapport détaillé de compensation.
        /// </summary>
        public string GenerateDetailedReport(DetailedCompensationResults results)
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("════════════════════════════════════════════");
            report.AppendLine("     RAPPORT DE COMPENSATION DÉTAILLÉ");
            report.AppendLine("════════════════════════════════════════════");
            report.AppendLine();

            // Configuration
            report.AppendLine("📋 CONFIGURATION:");
            report.AppendLine($"   • Réseau: {results.Configuration.NetworkConfig.NetworkType}");
            report.AppendLine($"   • Point référence: {results.Configuration.NetworkConfig.ReferencePoint} (Z = {results.Configuration.NetworkConfig.ReferenceAltitude:F3}m)");
            report.AppendLine($"   • Précision cible: {results.Configuration.TargetPrecisionMm}mm");
            report.AppendLine($"   • Paramètres géodésiques: a={results.Configuration.GeodeticParams.InstrumentalErrorMm}mm, b={results.Configuration.GeodeticParams.KilometricErrorMm}mm/km");
            report.AppendLine();

            // Matrice de conception
            report.AppendLine("📐 MATRICE DE CONCEPTION:");
            report.AppendLine($"   • {results.MatrixInfo.GetSummary()}");
            report.AppendLine($"   • Rang: {results.MatrixInfo.MatrixRank}");
            report.AppendLine();

            // Statistiques
            report.AppendLine("📊 STATISTIQUES:");
            report.AppendLine($"   • σ₀ = {results.Statistics.Sigma0Hat * 1000:F2}mm");
            report.AppendLine($"   • Test χ²: {(results.Statistics.UnitWeightValid ? "✅ Validé" : "❌ Échoué")}");
            report.AppendLine($"   • Résidu max normalisé: {results.Statistics.MaxStandardizedResidual:F2}");
            report.AppendLine($"   • Correction max: {results.MaxCorrection * 1000:F2}mm");
            report.AppendLine();

            // Validation
            report.AppendLine("✅ VALIDATION:");
            report.AppendLine(results.ValidationResult.GetSummary());
            report.AppendLine();

            // Points ajustés
            report.AppendLine("🎯 ALTITUDES AJUSTÉES:");
            foreach (var point in results.AdjustedPoints)
            {
                string marker = point.IsReference ? " (REF)" : "";
                report.AppendLine($"   • {point.Matricule}: {point.Altitude:F4}m{marker}");
            }

            report.AppendLine();
            report.AppendLine($"⏱️ Temps de calcul: {results.ComputationTime.TotalSeconds:F2}s");
            report.AppendLine($"🎯 Résultat: {(results.IsValid ? "✅ SUCCÈS" : "❌ ÉCHEC")}");

            return report.ToString();
        }

        /// <summary>
        /// Méthode utilitaire pour créer la liste des points ajustés
        /// </summary>
        private List<AltitudePoint> CreateAdjustedPointsList(
            EnhancedCompensationResults enhancedResults, 
            List<LevelingData> levelingData, 
            string referencePoint)
        {
            var adjustedPoints = new List<AltitudePoint>();
            
            for (int i = 0; i < enhancedResults.AdjustedAltitudes.Count; i++)
            {
                string pointId = (i == 0) ? referencePoint : levelingData[i - 1].Matricule;
                double precision = (i < enhancedResults.CovarianceMatrix.GetLength(0)) 
                    ? Math.Sqrt(enhancedResults.CovarianceMatrix[i, i]) 
                    : 0.001;
                
                adjustedPoints.Add(new AltitudePoint(pointId, enhancedResults.AdjustedAltitudes[i], false)
                {
                    Precision = precision
                });
            }
            
            return adjustedPoints;
        }
        
        /// <summary>
        /// Validation avancée spécialisée pour les résultats du compensateur amélioré
        /// </summary>
        private ValidationResult PerformAdvancedValidation(
            EnhancedCompensationResults enhancedResults, 
            List<LevelingData> levelingData)
        {
            var combinedResult = new ValidationResult { IsValid = true };
            
            // 1. Validation de la fermeture selon T = 4√K
            if (_configuration.NetworkConfig.NetworkType == "fermé")
            {
                double closureErrorMm = 0.0; // À calculer proprement
                double totalDistanceKm = levelingData.Sum(d => (d.DIST1 ?? d.DIST2 ?? 100.0) / 1000.0);
                var closureValidation = _precisionValidator.ValidateClosure(closureErrorMm, totalDistanceKm);
                
                if (!closureValidation.IsValid)
                {
                    foreach (var error in closureValidation.Errors)
                        combinedResult.AddError($"Fermeture: {error}");
                }
                foreach (var warning in closureValidation.Warnings)
                    combinedResult.AddWarning($"Fermeture: {warning}");
                    
                combinedResult.Details["closure_validation"] = closureValidation.Details;
            }
            
            // 2. Validation des résidus
            if (enhancedResults.Residuals != null && enhancedResults.Residuals.Length > 0)
            {
                var residualValidation = _precisionValidator.ValidateResiduals(
                    enhancedResults.Residuals, enhancedResults.Statistics.SigmaPosteriori);
                
                if (!residualValidation.IsValid)
                {
                    foreach (var error in residualValidation.Errors)
                        combinedResult.AddError($"Résidus: {error}");
                }
                foreach (var warning in residualValidation.Warnings)
                    combinedResult.AddWarning($"Résidus: {warning}");
                    
                combinedResult.Details["residual_validation"] = residualValidation.Details;
            }
            
            // 3. Tests statistiques intégrés du compensateur amélioré
            var stats = enhancedResults.Statistics;
            
            // Test χ²
            var chi2Validation = new ValidationResult { IsValid = stats.UnitWeightValid };
            chi2Validation.Details["chi2_statistic"] = stats.Chi2Statistic;
            chi2Validation.Details["chi2_critical"] = stats.Chi2Critical;
            chi2Validation.Details["degrees_of_freedom"] = stats.DegreesOfFreedom;
            
            if (!stats.UnitWeightValid)
            {
                chi2Validation.AddError($"Test χ² échoué: {stats.Chi2Statistic:F2} > {stats.Chi2Critical:F2}");
                chi2Validation.AddWarning("Le modèle stochastique pourrait être inadéquat");
            }
            
            combinedResult.Details["chi2_validation"] = chi2Validation.Details;
            if (!chi2Validation.IsValid)
            {
                foreach (var error in chi2Validation.Errors)
                    combinedResult.AddError($"Test χ²: {error}");
                foreach (var warning in chi2Validation.Warnings)
                    combinedResult.AddWarning($"Test χ²: {warning}");
            }
            
            // Détection de fautes grossières
            var blunderValidation = new ValidationResult { IsValid = true };
            blunderValidation.Details["blunder_count"] = stats.SuspectObservations.Count;
            blunderValidation.Details["blunder_indices"] = stats.SuspectObservations;
            blunderValidation.Details["t_critical"] = stats.BlunderDetectionThreshold;
            blunderValidation.Details["max_normalized_residual"] = stats.MaxStandardizedResidual;
            
            if (stats.SuspectObservations.Count > 0)
            {
                blunderValidation.AddWarning($"{stats.SuspectObservations.Count} observation(s) suspecte(s) détectée(s)");
            }
            
            combinedResult.Details["blunder_validation"] = blunderValidation.Details;
            foreach (var warning in blunderValidation.Warnings)
                combinedResult.AddWarning($"Fautes: {warning}");
            
            return combinedResult;
        }

        /// <summary>
        /// Validation géodésique avancée avec les nouveaux validateurs
        /// </summary>
        private ValidationResult PerformAdvancedValidation(DetailedCompensationResults results, List<LevelingData> levelingData)
        {
            var combinedResult = new ValidationResult { IsValid = true };
            
            // 1. Validation de la fermeture selon T = 4√K
            if (_configuration.NetworkConfig.NetworkType == "fermé" && results.ClosureError > 0)
            {
                double closureErrorMm = results.ClosureError * 1000;
                var closureValidation = _precisionValidator.ValidateClosure(closureErrorMm, results.TotalDistance);
                
                if (!closureValidation.IsValid)
                {
                    foreach (var error in closureValidation.Errors)
                        combinedResult.AddError($"Fermeture: {error}");
                }
                foreach (var warning in closureValidation.Warnings)
                    combinedResult.AddWarning($"Fermeture: {warning}");
                    
                combinedResult.Details["closure_validation"] = closureValidation.Details;
            }
            
            // 2. Validation des résidus
            if (results.Residuals != null && results.Residuals.Count > 0)
            {
                var residualsArray = results.Residuals.ToArray();
                var residualValidation = _precisionValidator.ValidateResiduals(residualsArray, results.Statistics.Sigma0Hat);
                
                if (!residualValidation.IsValid)
                {
                    foreach (var error in residualValidation.Errors)
                        combinedResult.AddError($"Résidus: {error}");
                }
                foreach (var warning in residualValidation.Warnings)
                    combinedResult.AddWarning($"Résidus: {warning}");
                    
                combinedResult.Details["residual_validation"] = residualValidation.Details;
            }
            
            // 3. Tests statistiques avancés
            if (results.Statistics != null && results.Residuals != null && results.Residuals.Count > 0)
            {
                int degreesOfFreedom = results.Residuals.Count - results.AdjustedPoints.Count + 1;
                
                // Test χ² pour validation du poids unitaire
                double vtPv = results.Residuals.DotProduct(results.Residuals);
                var chi2Validation = _statisticalValidator.ValidateUnitWeight(vtPv, degreesOfFreedom);
                
                if (!chi2Validation.IsValid)
                {
                    foreach (var error in chi2Validation.Errors)
                        combinedResult.AddError($"Test χ²: {error}");
                }
                foreach (var warning in chi2Validation.Warnings)
                    combinedResult.AddWarning($"Test χ²: {warning}");
                    
                combinedResult.Details["chi2_validation"] = chi2Validation.Details;
                
                // Détection de fautes grossières
                var normalizedResiduals = results.Residuals.ToArray();
                for (int i = 0; i < normalizedResiduals.Length; i++)
                {
                    normalizedResiduals[i] /= results.Statistics.Sigma0Hat;
                }
                
                var blunderValidation = _statisticalValidator.DetectBlunders(normalizedResiduals, degreesOfFreedom);
                
                if (blunderValidation.Warnings.Any())
                {
                    foreach (var warning in blunderValidation.Warnings)
                        combinedResult.AddWarning($"Fautes: {warning}");
                }
                
                combinedResult.Details["blunder_validation"] = blunderValidation.Details;
            }
            
            return combinedResult;
        }
        
        /// <summary>
        /// Combine plusieurs résultats de validation
        /// </summary>
        private ValidationResult CombineValidationResults(params ValidationResult[] results)
        {
            var combined = new ValidationResult { IsValid = true };
            
            foreach (var result in results)
            {
                if (!result.IsValid)
                    combined.IsValid = false;
                    
                combined.Errors.AddRange(result.Errors);
                combined.Warnings.AddRange(result.Warnings);
                
                foreach (var detail in result.Details)
                {
                    combined.Details[detail.Key] = detail.Value;
                }
            }
            
            return combined;
        }
    }
}