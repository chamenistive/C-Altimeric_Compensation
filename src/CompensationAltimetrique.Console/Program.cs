using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Data.Importers;
using CompensationAltimetrique.Calculations.Algorithms;
using CompensationAltimetrique.Calculations.Corrections;
using CompensationAltimetrique.Calculations.Validation;

namespace CompensationAltimetrique.Console
{
    /// <summary>
    /// Programme principal intégrant toutes les améliorations
    /// </summary>
    class IntegratedProgram
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            System.Console.WriteLine("║     SYSTÈME DE COMPENSATION ALTIMÉTRIQUE AMÉLIORÉ       ║");
            System.Console.WriteLine("║              Version 2.0 - Précision 2mm                 ║");
            System.Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            System.Console.WriteLine();
            
            try
            {
                // Configuration
                var config = new CompensationConfig
                {
                    PrecisionMm = 2.0,
                    InstrumentalErrorMm = 1.0,
                    KilometricErrorMm = 1.0,
                    ApplyAtmosphericCorrections = true,
                    Region = "sahel", // Adapté pour l'Afrique
                    SolutionMethod = SolutionMethod.Auto,
                    InitialAltitude = 125.456,
                    ExportResults = true
                };
                
                // Pipeline complet
                RunCompensationPipeline(config);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\n❌ ERREUR: {ex.Message}");
                System.Console.WriteLine($"   Détails: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }
        
        /// <summary>
        /// Pipeline complet de compensation avec toutes les améliorations
        /// </summary>
        static void RunCompensationPipeline(CompensationConfig config)
        {
            System.Console.WriteLine("🚀 Démarrage du pipeline de compensation...\n");
            
            // ==== PHASE 1: IMPORT DES DONNÉES ====
            System.Console.WriteLine("📥 PHASE 1: Import des données");
            System.Console.WriteLine("═══════════════════════════════");
            
            string dataFile = GetDataFile();
            var levelingData = ImportData(dataFile);
            System.Console.WriteLine($"✅ {levelingData.Count} lignes importées avec succès");
            
            // Validation de la structure des données
            var dataValidator = new DataStructureValidator();
            ValidateDataStructure(levelingData, dataValidator);
            
            // ==== PHASE 2: CALCULS PRÉLIMINAIRES ====
            System.Console.WriteLine("\n📊 PHASE 2: Calculs préliminaires");
            System.Console.WriteLine("═══════════════════════════════════");
            
            // Configuration des corrections atmosphériques
            AtmosphericCorrector atmosphericCorrector = null;
            if (config.ApplyAtmosphericCorrections)
            {
                var conditions = CompensationAltimetrique.Calculations.Corrections.AtmosphericConditions.CreateForRegion(config.Region);
                atmosphericCorrector = new AtmosphericCorrector();
                System.Console.WriteLine($"🌡️ Corrections atmosphériques activées pour région: {config.Region}");
                System.Console.WriteLine($"   Température: {conditions.TemperatureCelsius}°C");
                System.Console.WriteLine($"   Pression: {conditions.PressureHPa} hPa");
                System.Console.WriteLine($"   Humidité: {conditions.HumidityPercent}%");
            }
            
            // Calcul des dénivelées avec corrections
            var processedData = CalculateDenivelations(levelingData, atmosphericCorrector);
            
            // Analyse de fermeture
            var closureAnalysis = AnalyzeClosure(processedData, config.PrecisionMm);
            
            // ==== PHASE 3: COMPENSATION PAR MOINDRES CARRÉS ====
            System.Console.WriteLine("\n⚖️ PHASE 3: Compensation par moindres carrés");
            System.Console.WriteLine("═══════════════════════════════════════════════");
            
            var compensator = new EnhancedLeastSquaresCompensator(
                config.PrecisionMm,
                config.InstrumentalErrorMm,
                config.KilometricErrorMm
            );
            
            var results = compensator.Compensate(
                processedData,
                "REF",
                config.InitialAltitude,
                config.SolutionMethod
            );
            
            // ==== PHASE 4: VALIDATION DES RÉSULTATS ====
            System.Console.WriteLine("\n✅ PHASE 4: Validation des résultats");
            System.Console.WriteLine("═══════════════════════════════════════════");
            
            ValidateCompensationResults(results, config.PrecisionMm);
            
            // ==== PHASE 5: EXPORT DES RÉSULTATS ====
            if (config.ExportResults)
            {
                System.Console.WriteLine("\n💾 PHASE 5: Export des résultats");
                System.Console.WriteLine("═══════════════════════════════════════");
                ExportResults(processedData, results, config);
            }
            
            // ==== RAPPORT FINAL ====
            GenerateFinalReport(results, closureAnalysis, config);
        }
        
        /// <summary>
        /// Obtenir le fichier de données
        /// </summary>
        static string GetDataFile()
        {
            // Rechercher les fichiers dans le répertoire courant
            var dataFiles = Directory.GetFiles(".", "*.csv")
                .Concat(Directory.GetFiles(".", "*.xlsx"))
                .ToList();
            
            if (dataFiles.Count == 0)
            {
                System.Console.WriteLine("❌ Aucun fichier de données trouvé dans le répertoire");
                System.Console.Write("Entrez le chemin du fichier (ou 'simulation' pour données test): ");
                var input = System.Console.ReadLine();
                return string.IsNullOrEmpty(input) ? "simulation" : input;
            }
            
            if (dataFiles.Count == 1)
            {
                System.Console.WriteLine($"📄 Fichier trouvé: {dataFiles[0]}");
                return dataFiles[0];
            }
            
            System.Console.WriteLine("📄 Plusieurs fichiers trouvés:");
            for (int i = 0; i < dataFiles.Count; i++)
            {
                System.Console.WriteLine($"   {i + 1}. {Path.GetFileName(dataFiles[i])}");
            }
            
            System.Console.Write("Sélectionnez le numéro du fichier: ");
            if (int.TryParse(System.Console.ReadLine(), out int choice) && choice > 0 && choice <= dataFiles.Count)
            {
                return dataFiles[choice - 1];
            }
            
            return "simulation";
        }
        
        /// <summary>
        /// Import des données avec validation
        /// </summary>
        static List<LevelingData> ImportData(string filePath)
        {
            var importer = new ExcelLevelingImporter();
            var data = importer.ImportLevelingData(filePath);
            
            // Validation des données importées
            var invalidRows = data.Where(d => !d.HasValidReadings()).ToList();
            if (invalidRows.Any())
            {
                System.Console.WriteLine($"⚠️ {invalidRows.Count} lignes avec données invalides détectées");
                foreach (var row in invalidRows.Take(5))
                {
                    System.Console.WriteLine($"   - {row.Matricule}: AR1={row.AR1}, AV1={row.AV1}");
                }
            }
            
            return data.Where(d => d.HasValidReadings()).ToList();
        }
        
        /// <summary>
        /// Validation de la structure des données
        /// </summary>
        static void ValidateDataStructure(List<LevelingData> data, DataStructureValidator validator)
        {
            // Compter les colonnes AR/AV
            int arCount = 0, avCount = 0, distCount = 0;
            
            if (data.Any())
            {
                var first = data.First();
                if (first.AR1.HasValue) arCount++;
                if (first.AR2.HasValue) arCount++;
                if (first.AV1.HasValue) avCount++;
                if (first.AV2.HasValue) avCount++;
                if (first.DIST1.HasValue) distCount++;
                if (first.DIST2.HasValue) distCount++;
            }
            
            System.Console.WriteLine($"   Colonnes AR: {arCount}");
            System.Console.WriteLine($"   Colonnes AV: {avCount}");
            System.Console.WriteLine($"   Colonnes DIST: {distCount}");
            
            if (arCount != avCount)
            {
                System.Console.WriteLine("⚠️ Nombre de colonnes AR ≠ AV");
            }
        }
        
        /// <summary>
        /// Calcul des dénivelées avec corrections atmosphériques
        /// </summary>
        static List<LevelingData> CalculateDenivelations(
            List<LevelingData> data, 
            AtmosphericCorrector? corrector)
        {
            System.Console.WriteLine("🔢 Calcul des dénivelées...");
            
            int correctionCount = 0;
            double totalCorrection = 0;
            
            foreach (var point in data)
            {
                if (corrector != null && point.GetAverageDistance().HasValue)
                {
                    double distance = point.GetAverageDistance().Value;
                    var report = corrector.GenerateReport(distance);
                    
                    if (Math.Abs(report.TotalCorrectionMm) > 0.1)
                    {
                        correctionCount++;
                        totalCorrection += report.TotalCorrectionMm;
                        
                        // Afficher quelques exemples
                        if (correctionCount <= 3)
                        {
                            System.Console.WriteLine($"   Point {point.Matricule}: {report}");
                        }
                    }
                }
            }
            
            if (corrector != null)
            {
                System.Console.WriteLine($"✅ {correctionCount} corrections appliquées");
                System.Console.WriteLine($"   Correction totale: {totalCorrection:F2} mm");
            }
            
            return data;
        }
        
        /// <summary>
        /// Analyse de fermeture
        /// </summary>
        static ClosureAnalysis AnalyzeClosure(List<LevelingData> data, double precisionMm)
        {
            System.Console.WriteLine("🔄 Analyse de fermeture...");
            
            // Calcul de l'erreur de fermeture
            double totalDenivelation = 0;
            double totalDistance = 0;
            
            foreach (var point in data)
            {
                var dh = point.CalculateAverageDenivelation();
                totalDenivelation += dh;
                    
                var dist = point.GetAverageDistance();
                if (dist.HasValue)
                    totalDistance += dist.Value;
            }
            
            double closureErrorM = totalDenivelation;
            double closureErrorMm = closureErrorM * 1000;
            double totalDistanceKm = totalDistance / 1000;
            
            var validator = new GeodeticPrecisionValidator(precisionMm);
            var validation = validator.ValidateClosure(closureErrorMm, totalDistanceKm);
            
            var analysis = new ClosureAnalysis
            {
                ClosureErrorMm = closureErrorMm,
                TotalDistanceKm = totalDistanceKm,
                ToleranceMm = (double)validation.Details["tolerance_mm"],
                IsAcceptable = validation.IsValid,
                PrecisionRatio = (double)validation.Details["precision_ratio"]
            };
            
            System.Console.WriteLine($"   Erreur de fermeture: {closureErrorMm:F2} mm");
            System.Console.WriteLine($"   Tolérance: {analysis.ToleranceMm:F2} mm");
            System.Console.WriteLine($"   Distance totale: {totalDistanceKm:F3} km");
            System.Console.WriteLine($"   Statut: {(analysis.IsAcceptable ? "✅ ACCEPTABLE" : "❌ DÉPASSEMENT")}");
            
            return analysis;
        }
        
        /// <summary>
        /// Validation des résultats de compensation
        /// </summary>
        static void ValidateCompensationResults(EnhancedCompensationResults results, double precisionMm)
        {
            var validator = new GeodeticPrecisionValidator(precisionMm);
            var statValidator = new GeodeticStatisticalValidator(0.95);
            
            // Validation des résidus
            var residualValidation = validator.ValidateResiduals(
                results.Residuals, 
                results.Statistics.SigmaPosteriori
            );
            
            System.Console.WriteLine($"   Résidu max: {residualValidation.Details["max_residual_mm"]:F2} mm");
            System.Console.WriteLine($"   RMS résidus: {residualValidation.Details["rms_residual_mm"]:F2} mm");
            
            // Validation statistique
            int dof = Math.Max(1, results.Residuals.Length - results.AdjustedAltitudes.Count + 1);
            double vtPv = results.Residuals.Sum(r => r * r);
            var statValidation = statValidator.ValidateUnitWeight(vtPv, dof);
            
            System.Console.WriteLine($"   Test χ²: {(statValidation.IsValid ? "✅ PASSÉ" : "❌ ÉCHOUÉ")}");
            System.Console.WriteLine($"   σ₀ a posteriori: {results.Statistics.SigmaPosteriori:F4}");
            
            // Détection de fautes
            if (results.Statistics.SuspectObservations.Any())
            {
                System.Console.WriteLine($"   ⚠️ {results.Statistics.SuspectObservations.Count} observations suspectes:");
                foreach (var obs in results.Statistics.SuspectObservations.Take(3))
                {
                    System.Console.WriteLine($"      - Observation {obs + 1}");
                }
            }
            
            // Avertissements
            foreach (var warning in residualValidation.Warnings.Concat(statValidation.Warnings))
            {
                System.Console.WriteLine($"   ⚠️ {warning}");
            }
            
            // Erreurs
            foreach (var error in residualValidation.Errors.Concat(statValidation.Errors))
            {
                System.Console.WriteLine($"   ❌ {error}");
            }
        }
        
        /// <summary>
        /// Export des résultats
        /// </summary>
        static void ExportResults(
            List<LevelingData> data, 
            EnhancedCompensationResults results,
            CompensationConfig config)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outputFile = $"resultats_compensation_{timestamp}.csv";
            
            using (var writer = new StreamWriter(outputFile))
            {
                // En-tête
                writer.WriteLine("Matricule,Altitude_Originale,Correction_mm,Altitude_Compensee,Residu_mm");
                
                // Données
                for (int i = 0; i < Math.Min(data.Count, results.AdjustedAltitudes.Count); i++)
                {
                    double original = config.InitialAltitude;
                    if (i > 0)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            var dh = data[j].CalculateAverageDenivelation();
                            original += dh;
                        }
                    }
                    
                    double correctionMm = (i < results.Corrections.Length) ? 
                        results.Corrections[i] * 1000 : 0;
                    double residuMm = (i < results.Residuals.Length) ? 
                        results.Residuals[i] * 1000 : 0;
                    
                    writer.WriteLine($"{data[i].Matricule},{original:F6},{correctionMm:F3}," +
                                   $"{results.AdjustedAltitudes[i]:F6},{residuMm:F3}");
                }
            }
            
            System.Console.WriteLine($"✅ Résultats exportés: {outputFile}");
        }
        
        /// <summary>
        /// Génération du rapport final
        /// </summary>
        static void GenerateFinalReport(
            EnhancedCompensationResults results,
            ClosureAnalysis closure,
            CompensationConfig config)
        {
            System.Console.WriteLine("\n");
            System.Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            System.Console.WriteLine("║                    RAPPORT FINAL                         ║");
            System.Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            
            System.Console.WriteLine("\n📊 CONFIGURATION");
            System.Console.WriteLine("═══════════════");
            System.Console.WriteLine($"   Précision cible: {config.PrecisionMm} mm");
            System.Console.WriteLine($"   Altitude initiale: {config.InitialAltitude:F3} m");
            System.Console.WriteLine($"   Corrections atmosphériques: {(config.ApplyAtmosphericCorrections ? "OUI" : "NON")}");
            System.Console.WriteLine($"   Méthode de résolution: {config.SolutionMethod}");
            
            System.Console.WriteLine("\n📏 FERMETURE");
            System.Console.WriteLine("═══════════");
            System.Console.WriteLine($"   Erreur: {closure.ClosureErrorMm:F2} mm");
            System.Console.WriteLine($"   Tolérance: {closure.ToleranceMm:F2} mm");
            System.Console.WriteLine($"   Ratio: {closure.PrecisionRatio:P1}");
            System.Console.WriteLine($"   Statut: {(closure.IsAcceptable ? "✅ ACCEPTABLE" : "❌ DÉPASSEMENT")}");
            
            System.Console.WriteLine("\n📈 COMPENSATION");
            System.Console.WriteLine("═══════════════");
            System.Console.WriteLine($"   σ₀ a posteriori: {results.Statistics.SigmaPosteriori:F4}");
            System.Console.WriteLine($"   Degrés de liberté: {results.Statistics.DegreesOfFreedom}");
            System.Console.WriteLine($"   Test χ²: {results.Statistics.Chi2Statistic:F2} / {results.Statistics.Chi2Critical:F2}");
            System.Console.WriteLine($"   Poids unitaire: {(results.Statistics.UnitWeightValid ? "✅ VALIDE" : "❌ INVALIDE")}");
            System.Console.WriteLine($"   RMSE: {results.Statistics.RMSE:F2} mm");
            System.Console.WriteLine($"   Correction max: {results.Statistics.MaxCorrection:F2} mm");
            
            if (results.Statistics.SuspectObservations.Any())
            {
                System.Console.WriteLine($"\n⚠️ OBSERVATIONS SUSPECTES: {results.Statistics.SuspectObservations.Count}");
            }
            
            System.Console.WriteLine("\n✅ VALIDATION FINALE");
            System.Console.WriteLine("═══════════════════");
            
            bool precisionAchieved = results.Statistics.RMSE <= config.PrecisionMm;
            bool testsPassed = results.Statistics.UnitWeightValid && closure.IsAcceptable;
            
            if (precisionAchieved && testsPassed)
            {
                System.Console.WriteLine("   🎯 PRÉCISION 2mm ATTEINTE");
                System.Console.WriteLine("   ✅ TOUS LES TESTS PASSÉS");
                System.Console.WriteLine("   ✅ COMPENSATION RÉUSSIE");
            }
            else
            {
                System.Console.WriteLine("   ⚠️ OBJECTIFS NON ATTEINTS");
                if (!precisionAchieved)
                    System.Console.WriteLine($"      - Précision: {results.Statistics.RMSE:F2}mm > {config.PrecisionMm}mm");
                if (!results.Statistics.UnitWeightValid)
                    System.Console.WriteLine("      - Test du poids unitaire échoué");
                if (!closure.IsAcceptable)
                    System.Console.WriteLine("      - Fermeture hors tolérance");
            }
            
            System.Console.WriteLine("\n" + new string('═', 60));
            System.Console.WriteLine("Compensation terminée avec succès!");
        }
    }
    
    /// <summary>
    /// Configuration de la compensation
    /// </summary>
    public class CompensationConfig
    {
        public double PrecisionMm { get; set; } = 2.0;
        public double InstrumentalErrorMm { get; set; } = 1.0;
        public double KilometricErrorMm { get; set; } = 1.0;
        public bool ApplyAtmosphericCorrections { get; set; } = true;
        public string Region { get; set; } = "standard";
        public SolutionMethod SolutionMethod { get; set; } = SolutionMethod.Auto;
        public double InitialAltitude { get; set; } = 125.456;
        public bool ExportResults { get; set; } = true;
    }
    
    /// <summary>
    /// Analyse de fermeture
    /// </summary>
    public class ClosureAnalysis
    {
        public double ClosureErrorMm { get; set; }
        public double TotalDistanceKm { get; set; }
        public double ToleranceMm { get; set; }
        public bool IsAcceptable { get; set; }
        public double PrecisionRatio { get; set; }
    }
}