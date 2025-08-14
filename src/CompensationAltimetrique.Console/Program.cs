using System;
using System.Collections.Generic;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Data.Importers;
using CompensationAltimetrique.Calculations.Algorithms;

namespace CompensationAltimetrique.Console
{
    class Program
    {
        private const double PRECISION_TARGET_MM = 2.0;
        private const double INITIAL_ALTITUDE = 125.456; // Altitude de référence AM2
        
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
            // ÉTAPE 3 : COMPENSATION PAR MOINDRES CARRÉS
            // ================================================================
            System.Console.WriteLine("⚙️  ÉTAPE 3 : COMPENSATION PAR MOINDRES CARRÉS");
            System.Console.WriteLine(new string('-', 50));
            
            var compensator = new LeastSquaresCompensator(PRECISION_TARGET_MM);
            var compensationResults = compensator.Compensate(validData, INITIAL_ALTITUDE);
            
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
                // ÉTAPE 4 : ALTITUDES COMPENSÉES
                // ================================================================
                System.Console.WriteLine("📏 ÉTAPE 4 : ALTITUDES COMPENSÉES");
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
                // ÉTAPE 5 : EXPORT DES RÉSULTATS
                // ================================================================
                System.Console.WriteLine($"\n💾 ÉTAPE 5 : EXPORT DES RÉSULTATS");
                System.Console.WriteLine(new string('-', 50));
                
                ExportCompensationResults(compensationResults, validData, "resultats_compensation.csv");
                
                // Résumé final
                System.Console.WriteLine($"\n🎯 RÉSUMÉ FINAL:");
                System.Console.WriteLine($"   ✅ {compensationResults.AdjustedPoints.Count} points compensés");
                System.Console.WriteLine($"   ✅ Précision: {compensationResults.Sigma0*1000:F1} mm (objectif: {PRECISION_TARGET_MM} mm)");
                System.Console.WriteLine($"   ✅ Méthode: {compensationResults.Method}");
                System.Console.WriteLine($"   ✅ Temps de calcul: instantané");
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
    }
}