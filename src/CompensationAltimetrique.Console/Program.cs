using System;
using System.Collections.Generic;
using System.Linq;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Core.Services;
using CompensationAltimetrique.Data.Importers;
using CompensationAltimetrique.Calculations.Algorithms;
using CompensationAltimetrique.Calculations.Corrections;

namespace CompensationAltimetrique.Console
{
    class Program
    {
        private const double PRECISION_TARGET_MM = 2.0;
        private const double INITIAL_ALTITUDE = 518.519; // Altitude de référence AM2
        
        static void Main(string[] args)
        {
            PrintHeader();
            
            try
            {
                if (args.Length > 0)
                {
                    ProcessBatchMode(args);
                }
                else
                {
                    ProcessInteractiveMode();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Erreur : {ex.Message}");
                Environment.Exit(1);
            }
            
            System.Console.WriteLine("\n✅ Traitement terminé avec succès.");
        }
        
        private static void PrintHeader()
        {
            System.Console.WriteLine("================================================================================");
            System.Console.WriteLine("    SYSTÈME DE COMPENSATION ALTIMÉTRIQUE - VERSION C#");
            System.Console.WriteLine($"    Précision cible: {PRECISION_TARGET_MM} mm");
            System.Console.WriteLine("    Pipeline complet: Import → Calcul → Compensation → Export");
            System.Console.WriteLine("================================================================================\n");
        }
        
        private static void ProcessBatchMode(string[] args)
        {
            System.Console.WriteLine("🔄 Mode batch - Pipeline automatique");
            System.Console.WriteLine($"📁 Fichier : {args[0]}");
            
            ProcessCompletePipeline(args[0]);
        }
        
        private static void ProcessInteractiveMode()
        {
            System.Console.WriteLine("🎯 Mode interactif - Démonstration complète");
            System.Console.WriteLine("\n🚀 Exécution du pipeline complet de compensation...\n");
            
            ProcessCompletePipeline("simulation");
        }
        
        private static void ProcessCompletePipeline(string filePath)
        {
            // ================================================================
            // ÉTAPE 1 : IMPORT DES DONNÉES
            // ================================================================
            System.Console.WriteLine("📊 ÉTAPE 1 : IMPORT DES DONNÉES");
            System.Console.WriteLine(new string('-', 50));
            
            var importer = new ExcelLevelingImporter();
            var levelingData = importer.ImportLevelingData(filePath);
            
            System.Console.WriteLine($"✅ {levelingData.Count} points de nivellement importés");
            System.Console.WriteLine($"📍 Point de référence : AM2 (altitude = {INITIAL_ALTITUDE:F6} m)\n");
            
            // ================================================================
            // ÉTAPE 2 : VALIDATION ET ANALYSE PRÉLIMINAIRE
            // ================================================================
            System.Console.WriteLine("🔍 ÉTAPE 2 : VALIDATION ET ANALYSE PRÉLIMINAIRE");
            System.Console.WriteLine(new string('-', 50));
            
            int coherentCount = 0;
            double totalClosure = 0.0;
            var validData = new List<LevelingData>();
            
            foreach (var item in levelingData)
            {
                var dh = item.CalculateAverageDenivelation();
                var coherent = item.IsConsistent();
                
                if (dh.HasValue)
                {
                    validData.Add(item);
                    totalClosure += dh.Value;
                    
                    if (coherent) coherentCount++;
                    
                    string status = coherent ? "✅" : "⚠️";
                    System.Console.WriteLine($"{status} {item.Matricule}: ΔH = {dh.Value:F6} m");
                    
                    if (!coherent)
                    {
                        var dh1 = item.CalculateDenivelation(1);
                        var dh2 = item.CalculateDenivelation(2);
                        if (dh1.HasValue && dh2.HasValue)
                        {
                            var diff = Math.Abs(dh1.Value - dh2.Value) * 1000;
                            System.Console.WriteLine($"   ⚠️  Écart entre sessions: {diff:F1} mm");
                        }
                    }
                }
            }
            
            System.Console.WriteLine($"\n📈 STATISTIQUES PRÉLIMINAIRES:");
            System.Console.WriteLine($"   Points cohérents: {coherentCount}/{validData.Count} ({(double)coherentCount/validData.Count*100:F1}%)");
            System.Console.WriteLine($"   Fermeture brute: {totalClosure*1000:F1} mm");
            System.Console.WriteLine($"   Qualité données: {(Math.Abs(totalClosure*1000) < 50 ? "Bonne" : "À améliorer")}\n");
            
            // ================================================================
            // ÉTAPE 3 : CORRECTIONS ATMOSPHÉRIQUES
            // ================================================================
            System.Console.WriteLine("🌡️ ÉTAPE 3 : CORRECTIONS ATMOSPHÉRIQUES");
            System.Console.WriteLine(new string('-', 50));
            
            // Conditions atmosphériques (France métropolitaine par défaut)
            var atmosphericConditions = new AtmosphericConditions(15.0, 1013.25, 65.0);
            var corrector = new AtmosphericCorrector(atmosphericConditions, true);
            
            // Analyse avant correction
            var analysisBeforeCorrection = corrector.AnalyzeCorrections(validData);
            System.Console.WriteLine($"📊 Analyse pré-correction: {analysisBeforeCorrection.GetSummary()}");
            
            // Application des corrections
            var correctedData = corrector.ApplyCorrections(validData);
            
            // Recalcul des statistiques après correction
            coherentCount = 0;
            totalClosure = 0.0;
            foreach (var item in correctedData)
            {
                var dh = item.CalculateAverageDenivelation();
                if (dh.HasValue)
                {
                    totalClosure += dh.Value;
                    if (item.IsConsistent()) coherentCount++;
                }
            }
            
            System.Console.WriteLine($"📈 APRÈS CORRECTIONS ATMOSPHÉRIQUES:");
            System.Console.WriteLine($"   Points cohérents: {coherentCount}/{correctedData.Count} ({(double)coherentCount/correctedData.Count*100:F1}%)");
            System.Console.WriteLine($"   Nouvelle fermeture: {totalClosure*1000:F1} mm");
            System.Console.WriteLine($"   Amélioration: {(Math.Abs(totalClosure*1000) < Math.Abs(-14410.9) ? "✅ OUI" : "❌ MINIME")}\n");
            
            // ================================================================
            // ÉTAPE 4 : COMPENSATION PAR MOINDRES CARRÉS
            // ================================================================
            System.Console.WriteLine("⚙️  ÉTAPE 4 : COMPENSATION PAR MOINDRES CARRÉS");
            System.Console.WriteLine(new string('-', 50));
            
            var compensator = new LeastSquaresCompensator(PRECISION_TARGET_MM);
            var compensationResults = compensator.Compensate(correctedData, INITIAL_ALTITUDE);
            
            if (compensationResults.IsValid)
            {
                System.Console.WriteLine("✅ Compensation réussie !\n");
                
                System.Console.WriteLine("📊 RÉSULTATS DE LA COMPENSATION:");
                System.Console.WriteLine($"   σ₀ (écart-type unitaire): {compensationResults.Sigma0*1000:F1} mm");
                System.Console.WriteLine($"   Correction maximale: {compensationResults.MaxCorrection*1000:F1} mm");
                System.Console.WriteLine($"   RMS des résidus: {compensationResults.RmsResiduals*1000:F1} mm");
                System.Console.WriteLine($"   Qualité: {compensationResults.GetQualityAssessment()}");
                System.Console.WriteLine($"   Précision atteinte: {(compensationResults.Sigma0*1000 <= PRECISION_TARGET_MM ? "✅ OUI" : "❌ NON")}\n");
                
                // ================================================================
                // ÉTAPE 5 : ALTITUDES COMPENSÉES
                // ================================================================
                System.Console.WriteLine("📏 ÉTAPE 5 : ALTITUDES COMPENSÉES");
                System.Console.WriteLine(new string('-', 50));
                
                System.Console.WriteLine("Point        | Altitude compensée | Correction");
                System.Console.WriteLine("-------------|--------------------|-----------");
                
                foreach (var point in compensationResults.AdjustedPoints)
                {
                    var correction = 0.0;
                    if (!point.IsReference && compensationResults.Corrections.Length > 0)
                    {
                        var index = compensationResults.AdjustedPoints.IndexOf(point);
                        if (index < compensationResults.Corrections.Length)
                        {
                            correction = compensationResults.Corrections[index];
                        }
                    }
                    
                    string refMarker = point.IsReference ? " (REF)" : "";
                    System.Console.WriteLine($"{point.Matricule,-12} | {point.Altitude:F6} m       | {correction*1000:+F1;-F1;+0.0} mm{refMarker}");
                }
                
                // ================================================================
                // ÉTAPE 6 : GÉNÉRATION DES RAPPORTS
                // ================================================================
                System.Console.WriteLine($"\n📋 ÉTAPE 6 : GÉNÉRATION DES RAPPORTS");
                System.Console.WriteLine(new string('-', 50));
                
                var reportGenerator = new ReportGenerator(PRECISION_TARGET_MM);
                
                // Rapport de calculs (avec données corrigées)
                var calculationReport = reportGenerator.GenerateCalculationReport(correctedData, INITIAL_ALTITUDE);
                System.IO.File.WriteAllText("rapport_calculs.txt", calculationReport);
                System.Console.WriteLine("✅ Rapport de calculs généré: rapport_calculs.txt");
                
                // Rapport de compensation
                var compensationReport = reportGenerator.GenerateCompensationReport(compensationResults, correctedData, INITIAL_ALTITUDE);
                System.IO.File.WriteAllText("rapport_compensation.txt", compensationReport);
                System.Console.WriteLine("✅ Rapport de compensation généré: rapport_compensation.txt");
                
                // Rapport de corrections atmosphériques
                var atmosphericReport = GenerateAtmosphericReport(analysisBeforeCorrection, correctedData, validData);
                System.IO.File.WriteAllText("rapport_atmospherique.txt", atmosphericReport);
                System.Console.WriteLine("✅ Rapport atmosphérique généré: rapport_atmospherique.txt");
                
                // Export CSV (conservé)
                ExportCompensationResults(compensationResults, correctedData, "resultats_compensation.csv");
                
                // Affichage des rapports
                System.Console.WriteLine($"\n📄 RAPPORT DE CALCULS:");
                System.Console.WriteLine(calculationReport);
                
                System.Console.WriteLine($"\n📄 RAPPORT DE COMPENSATION:");
                System.Console.WriteLine(compensationReport);
            }
            else
            {
                System.Console.WriteLine("❌ Échec de la compensation !");
                System.Console.WriteLine("💡 Vérifiez la qualité et la cohérence des données d'entrée.");
            }
        }
        
        private static void ExportCompensationResults(CompensationResults results, List<LevelingData> originalData, string filePath)
        {
            var lines = new List<string>
            {
                "Point,Altitude_Compensee_m,Correction_mm,Type,Precision_mm"
            };
            
            foreach (var point in results.AdjustedPoints)
            {
                var correction = 0.0;
                if (!point.IsReference && results.Corrections.Length > 0)
                {
                    var index = results.AdjustedPoints.IndexOf(point);
                    if (index < results.Corrections.Length)
                    {
                        correction = results.Corrections[index] * 1000; // en mm
                    }
                }
                
                var type = point.IsReference ? "Reference" : "Point";
                var precision = results.Sigma0 * 1000; // en mm
                
                lines.Add($"{point.Matricule},{point.Altitude:F6},{correction:F1},{type},{precision:F1}");
            }
            
            // Ajout des statistiques globales
            lines.Add("");
            lines.Add("# STATISTIQUES GLOBALES");
            lines.Add($"# Sigma0_mm,{results.Sigma0*1000:F1}");
            lines.Add($"# Correction_max_mm,{results.MaxCorrection*1000:F1}");
            lines.Add($"# RMS_residus_mm,{results.RmsResiduals*1000:F1}");
            lines.Add($"# Qualite,{results.GetQualityAssessment().Substring(2)}"); // Enlever l'emoji
            lines.Add($"# Methode,{results.Method}");
            lines.Add($"# Date_calcul,{results.ComputationTime:yyyy-MM-dd HH:mm:ss}");
            
            System.IO.File.WriteAllLines(filePath, lines);
            System.Console.WriteLine($"✅ Résultats exportés vers: {filePath}");
            System.Console.WriteLine($"📊 Format: CSV avec {results.AdjustedPoints.Count} points + statistiques");
        }
        
        private static string GenerateAtmosphericReport(Calculations.Corrections.CorrectionAnalysis analysis, List<LevelingData> correctedData, List<LevelingData> originalData)
        {
            var report = new System.Text.StringBuilder();
            
            // En-tête
            report.AppendLine("======================================================================");
            report.AppendLine("    RAPPORT DE CORRECTIONS ATMOSPHÉRIQUES");
            report.AppendLine("======================================================================");
            report.AppendLine();
            
            // Conditions appliquées
            report.AppendLine("🌡️ CONDITIONS ATMOSPHÉRIQUES:");
            report.AppendLine("   Température: 15.0°C");
            report.AppendLine("   Pression: 1013.25 hPa");
            report.AppendLine("   Humidité: 65.0%");
            report.AppendLine("   Coefficient réfraction: 0.130");
            report.AppendLine("   Rayon terrestre: 6,371,000 m");
            report.AppendLine();
            
            // Statistiques des corrections
            report.AppendLine("📊 ANALYSE DES CORRECTIONS:");
            report.AppendLine($"   Observations traitées: {analysis.Corrections.Count}");
            report.AppendLine($"   Distance maximale: {analysis.MaxDistance:F1} m");
            report.AppendLine($"   Correction maximale: {analysis.MaxCorrection * 1000:F2} mm");
            report.AppendLine($"   Correction minimale: {analysis.MinCorrection * 1000:F2} mm");
            report.AppendLine($"   Correction moyenne: {analysis.AverageCorrection * 1000:F2} mm");
            report.AppendLine($"   Corrections significatives (>1mm): {analysis.SignificantCorrections}");
            report.AppendLine();
            
            // Impact sur la fermeture
            var originalClosure = originalData.Where(d => d.CalculateAverageDenivelation().HasValue)
                                           .Sum(d => d.CalculateAverageDenivelation().Value) * 1000;
            var correctedClosure = correctedData.Where(d => d.CalculateAverageDenivelation().HasValue)
                                              .Sum(d => d.CalculateAverageDenivelation().Value) * 1000;
            var improvement = Math.Abs(originalClosure) - Math.Abs(correctedClosure);
            
            report.AppendLine("📈 IMPACT SUR LA FERMETURE:");
            report.AppendLine($"   Fermeture avant correction: {originalClosure:F1} mm");
            report.AppendLine($"   Fermeture après correction: {correctedClosure:F1} mm");
            report.AppendLine($"   Amélioration: {improvement:F1} mm ({improvement/Math.Abs(originalClosure)*100:F1}%)");
            report.AppendLine($"   Statut: {(improvement > 0 ? "✅ AMÉLIORATION" : "⚠️ DÉGRADATION")}");
            report.AppendLine();
            
            // Recommandations
            report.AppendLine("🎯 RECOMMANDATIONS:");
            if (analysis.MaxDistance > 200)
                report.AppendLine("   ✅ Corrections justifiées (distances > 200m détectées)");
            else
                report.AppendLine("   ⚠️ Corrections mineures (distances courtes)");
            
            if (analysis.SignificantCorrections > analysis.Corrections.Count * 0.3)
                report.AppendLine("   ✅ Impact significatif sur la précision");
            else
                report.AppendLine("   ⚠️ Impact limité sur la précision");
            
            if (improvement > 100) // > 100mm d'amélioration
                report.AppendLine("   ✅ Forte amélioration de la fermeture");
            else if (improvement > 0)
                report.AppendLine("   ✅ Amélioration modérée de la fermeture");
            else
                report.AppendLine("   ❌ Pas d'amélioration significative");
            
            report.AppendLine();
            report.AppendLine($"Rapport généré le: {DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff}");
            report.AppendLine("======================================================================");
            
            return report.ToString();
        }
    }
}