using System;
using System.Collections.Generic;
using System.IO;
using CompensationAltimetrique.Core.Models;
using OfficeOpenXml;

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
                
                // Déterminer le type de fichier
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (extension == ".xlsx" || extension == ".xls")
                {
                    data = ImportFromExcel(filePath);
                }
                else
                {
                    data = ImportFromCsv(filePath);
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
                         $"{dhMoy.ToString("F6")}," +
                         $"{coherent}");
            }
            
            File.WriteAllLines(outputPath, lines);
            System.Console.WriteLine($"💾 Résultats exportés vers : {outputPath}");
        }
        
        /// <summary>
        /// Import depuis un fichier Excel (.xlsx/.xls)
        /// </summary>
        private List<LevelingData> ImportFromExcel(string filePath)
        {
            var data = new List<LevelingData>();
            
            try
            {
                System.Console.WriteLine($"🔍 Début de l'import Excel: {filePath}");
                
                // Configuration EPPlus pour éviter les erreurs de licence
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                System.Console.WriteLine($"📝 Package Excel créé, nombre de feuilles: {package.Workbook.Worksheets.Count}");
                
                if (package.Workbook.Worksheets.Count == 0)
                    throw new ArgumentException("Le fichier Excel ne contient aucune feuille de calcul");
                    
                var worksheet = package.Workbook.Worksheets[0]; // Première feuille
                System.Console.WriteLine($"📋 Feuille sélectionnée: {worksheet.Name}");
                
                System.Console.WriteLine($"📐 Dimension de la feuille: {worksheet.Dimension?.ToString() ?? "null"}");
                
                if (worksheet.Dimension == null)
                    throw new ArgumentException("La feuille Excel est vide");
                    
                var rowCount = worksheet.Dimension.Rows;
                var colCount = worksheet.Dimension.Columns;
                System.Console.WriteLine($"📊 Lignes: {rowCount}, Colonnes: {colCount}");
                
                if (rowCount < 2)
                    throw new ArgumentException("Le fichier Excel doit contenir au moins un en-tête et une ligne de données");
                
                // Lire l'en-tête (première ligne)
                var headers = new string[colCount];
                for (int col = 1; col <= colCount; col++)
                {
                    var cellValue = worksheet.Cells[1, col].Value;
                    headers[col - 1] = cellValue?.ToString()?.Trim() ?? "";
                }
                
                // Trouver les index des colonnes
                var matriculeIndex = FindColumnIndex(headers, "Matricule");
                var ar1Index = FindColumnIndex(headers, "AR 1");
                var av1Index = FindColumnIndex(headers, "AV 1");
                var dist1Index = FindColumnIndex(headers, "DIST 1");
                var ar2Index = FindColumnIndex(headers, "AR 2");
                var av2Index = FindColumnIndex(headers, "AV 2");
                var dist2Index = FindColumnIndex(headers, "DIST 2");
                
                // Lire les données (à partir de la ligne 2)
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        if (matriculeIndex >= 0 && matriculeIndex < colCount)
                        {
                            var matriculeCell = worksheet.Cells[row, matriculeIndex + 1].Value;
                            var matricule = matriculeCell?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(matricule))
                            {
                                var levelingData = new LevelingData(matricule);
                                
                                // Parse les valeurs numériques
                                if (ar1Index >= 0 && ar1Index < colCount)
                                {
                                    var ar1Cell = worksheet.Cells[row, ar1Index + 1].Value;
                                    levelingData.AR1 = ParseDouble(ar1Cell?.ToString());
                                }
                                
                                if (av1Index >= 0 && av1Index < colCount)
                                {
                                    var av1Cell = worksheet.Cells[row, av1Index + 1].Value;
                                    levelingData.AV1 = ParseDouble(av1Cell?.ToString());
                                }
                                
                                if (dist1Index >= 0 && dist1Index < colCount)
                                {
                                    var dist1Cell = worksheet.Cells[row, dist1Index + 1].Value;
                                    levelingData.DIST1 = ParseDouble(dist1Cell?.ToString());
                                }
                                
                                if (ar2Index >= 0 && ar2Index < colCount)
                                {
                                    var ar2Cell = worksheet.Cells[row, ar2Index + 1].Value;
                                    levelingData.AR2 = ParseDouble(ar2Cell?.ToString());
                                }
                                
                                if (av2Index >= 0 && av2Index < colCount)
                                {
                                    var av2Cell = worksheet.Cells[row, av2Index + 1].Value;
                                    levelingData.AV2 = ParseDouble(av2Cell?.ToString());
                                }
                                
                                if (dist2Index >= 0 && dist2Index < colCount)
                                {
                                    var dist2Cell = worksheet.Cells[row, dist2Index + 1].Value;
                                    levelingData.DIST2 = ParseDouble(dist2Cell?.ToString());
                                }
                                
                                data.Add(levelingData);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"⚠️  Erreur ligne {row}: {ex.Message}");
                    }
                }
            }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Erreur lors de l'import Excel : {ex.Message}");
                System.Console.WriteLine($"   Type: {ex.GetType().Name}");
                System.Console.WriteLine($"   Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                    System.Console.WriteLine($"   Inner Stack: {ex.InnerException.StackTrace}");
                }
                throw;
            }
            
            return data;
        }
        
        /// <summary>
        /// Import depuis un fichier CSV
        /// </summary>
        private List<LevelingData> ImportFromCsv(string filePath)
        {
            var data = new List<LevelingData>();
            
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
                    if (matriculeIndex >= 0 && values.Length > matriculeIndex)
                    {
                        var matricule = values[matriculeIndex].Trim();
                        if (!string.IsNullOrEmpty(matricule))
                        {
                            var levelingData = new LevelingData(matricule);
                            
                            // Parse les valeurs numériques
                            if (ar1Index >= 0 && values.Length > ar1Index)
                                levelingData.AR1 = ParseDouble(values[ar1Index]);
                            
                            if (av1Index >= 0 && values.Length > av1Index)
                                levelingData.AV1 = ParseDouble(values[av1Index]);
                            
                            if (dist1Index >= 0 && values.Length > dist1Index)
                                levelingData.DIST1 = ParseDouble(values[dist1Index]);
                            
                            if (ar2Index >= 0 && values.Length > ar2Index)
                                levelingData.AR2 = ParseDouble(values[ar2Index]);
                            
                            if (av2Index >= 0 && values.Length > av2Index)
                                levelingData.AV2 = ParseDouble(values[av2Index]);
                            
                            if (dist2Index >= 0 && values.Length > dist2Index)
                                levelingData.DIST2 = ParseDouble(values[dist2Index]);
                            
                            data.Add(levelingData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"⚠️  Erreur ligne {i + 1}: {ex.Message}");
                }
            }
            
            return data;
        }
        
        /// <summary>
        /// Parse une chaîne en double de façon sécurisée
        /// </summary>
        private double? ParseDouble(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            
            if (double.TryParse(text.Trim(), out double result))
                return result;
            
            return null;
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