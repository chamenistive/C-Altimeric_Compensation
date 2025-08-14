using System;
using System.Collections.Generic;
using System.IO;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Data.Importers
{
    /// <summary>
    /// Importeur spécialisé pour les données Excel de nivellement
    /// Format attendu : Matricule | AR 1 | AV 1 | DIST 1 | AR 2 | AV 2 | DIST 2
    /// </summary>
    public class ExcelLevelingImporter
    {
        public List<LevelingData> ImportLevelingData(string filePath)
        {
            var data = new List<LevelingData>();
            
            if (filePath == "simulation")
            {
                System.Console.WriteLine($"📊 Import de {filePath} (mode simulation)");
                // Données de test basées sur votre fichier canal_G1.xlsx
                data.Add(new LevelingData("AM2") { AR1 = 1.0038, AR2 = 1.01262 });
                data.Add(new LevelingData("1") { AR1 = 0.97702, AV1 = 1.99728, DIST1 = 20.2337, AR2 = 0.98953, AV2 = 2.00614, DIST2 = 20.234 });
                data.Add(new LevelingData("2") { AR1 = 1.24658, AV1 = 1.89112, DIST1 = 20.8439, AR2 = 1.2488, AV2 = 1.90365, DIST2 = 20.834 });
                data.Add(new LevelingData("3") { AR1 = 1.84004, AV1 = 2.0001, DIST1 = 22.7397, AR2 = 1.84127, AV2 = 2.00236, DIST2 = 22.7356 });
            }
            else if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Fichier non trouvé : {filePath}");
            }
            else
            {
                System.Console.WriteLine($"📊 Import de {filePath}");
                
                // Import réel depuis fichier CSV
                var lines = File.ReadAllLines(filePath);
                if (lines.Length < 2)
                    throw new ArgumentException("Le fichier doit contenir au moins un en-tête et une ligne de données");
                
                // Parse l'en-tête
                var headers = lines[0].Split(',');
                var matriculeIndex = FindColumnIndex(headers, "Matricule");
                var ar1Index = FindColumnIndex(headers, "AR 1");
                var av1Index = FindColumnIndex(headers, "AV 1");
                var dist1Index = FindColumnIndex(headers, "DIST 1");
                var ar2Index = FindColumnIndex(headers, "AR 2");
                var av2Index = FindColumnIndex(headers, "AV 2");
                var dist2Index = FindColumnIndex(headers, "DIST 2");
                
                // Parse les données
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = lines[i].Split(',');
                        if (values.Length > Math.Max(matriculeIndex, Math.Max(ar1Index, ar2Index)))
                        {
                            var matricule = values[matriculeIndex].Trim();
                            var levelingData = new LevelingData(matricule);
                            
                            // Parse les valeurs numériques
                            if (ar1Index >= 0 && values.Length > ar1Index && !string.IsNullOrEmpty(values[ar1Index]))
                                if (double.TryParse(values[ar1Index], out double ar1)) levelingData.AR1 = ar1;
                            
                            if (av1Index >= 0 && values.Length > av1Index && !string.IsNullOrEmpty(values[av1Index]))
                                if (double.TryParse(values[av1Index], out double av1)) levelingData.AV1 = av1;
                            
                            if (dist1Index >= 0 && values.Length > dist1Index && !string.IsNullOrEmpty(values[dist1Index]))
                                if (double.TryParse(values[dist1Index], out double dist1)) levelingData.DIST1 = dist1;
                            
                            if (ar2Index >= 0 && values.Length > ar2Index && !string.IsNullOrEmpty(values[ar2Index]))
                                if (double.TryParse(values[ar2Index], out double ar2)) levelingData.AR2 = ar2;
                            
                            if (av2Index >= 0 && values.Length > av2Index && !string.IsNullOrEmpty(values[av2Index]))
                                if (double.TryParse(values[av2Index], out double av2)) levelingData.AV2 = av2;
                            
                            if (dist2Index >= 0 && values.Length > dist2Index && !string.IsNullOrEmpty(values[dist2Index]))
                                if (double.TryParse(values[dist2Index], out double dist2)) levelingData.DIST2 = dist2;
                            
                            data.Add(levelingData);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"⚠️  Erreur ligne {i + 1}: {ex.Message}");
                    }
                }
            }
            
            return data;
        }
        
        public void ExportResults(List<LevelingData> data, string outputPath)
        {
            var lines = new List<string>
            {
                "Matricule,AR1,AV1,DIST1,AR2,AV2,DIST2,DH1,DH2,DH_Moyenne,Coherent"
            };
            
            foreach (var item in data)
            {
                var dh1 = item.CalculateDenivelation(1);
                var dh2 = item.CalculateDenivelation(2);
                var dhMoy = item.CalculateAverageDenivelation();
                var coherent = item.IsConsistent();
                
                lines.Add($"{item.Matricule}," +
                         $"{item.AR1?.ToString("F6") ?? ""}," +
                         $"{item.AV1?.ToString("F6") ?? ""}," +
                         $"{item.DIST1?.ToString("F3") ?? ""}," +
                         $"{item.AR2?.ToString("F6") ?? ""}," +
                         $"{item.AV2?.ToString("F6") ?? ""}," +
                         $"{item.DIST2?.ToString("F3") ?? ""}," +
                         $"{dh1?.ToString("F6") ?? ""}," +
                         $"{dh2?.ToString("F6") ?? ""}," +
                         $"{dhMoy?.ToString("F6") ?? ""}," +
                         $"{coherent}");
            }
            
            File.WriteAllLines(outputPath, lines);
            System.Console.WriteLine($"💾 Résultats exportés vers : {outputPath}");
        }
        
        private int FindColumnIndex(string[] headers, string columnName)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i].Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }
    }
}