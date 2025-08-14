using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Data.Interfaces;

namespace CompensationAltimetrique.Data.Importers
{
    /// <summary>
    /// Importeur de données CSV pour le nivellement
    /// </summary>
    public class CsvImporter : IDataImporter
    {
        private readonly char _separator;
        
        public CsvImporter(char separator = ',')
        {
            _separator = separator;
        }
        
        public List<AltitudePoint> ImportData(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Fichier non trouvé : {filePath}");
                
            var points = new List<AltitudePoint>();
            var lines = File.ReadAllLines(filePath);
            
            if (lines.Length < 2)
                throw new ArgumentException("Le fichier doit contenir au moins un en-tête et une ligne de données");
            
            // Parse l'en-tête pour détecter les colonnes
            var headers = ParseLine(lines[0]);
            var matriculeIndex = FindColumnIndex(headers, "Matricule");
            var altitudeIndex = FindColumnIndex(headers, new[] { "Altitude", "Alt", "Z" });
            
            // Parse les données
            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var values = ParseLine(lines[i]);
                    
                    if (values.Length > Math.Max(matriculeIndex, altitudeIndex))
                    {
                        var matricule = values[matriculeIndex].Trim();
                        
                        if (double.TryParse(values[altitudeIndex].Replace(',', '.'), 
                            NumberStyles.Float, CultureInfo.InvariantCulture, out double altitude))
                        {
                            // Détecter les points de référence (contiennent "REF", "AM", ou similaire)
                            bool isReference = matricule.ToUpper().Contains("REF") || 
                                             matricule.ToUpper().Contains("AM") ||
                                             matricule.ToUpper().StartsWith("R");
                            
                            points.Add(new AltitudePoint(matricule, altitude, isReference));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"⚠️  Erreur ligne {i + 1}: {ex.Message}");
                }
            }
            
            return points;
        }
        
        public bool ValidateFileFormat(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;
                
                var lines = File.ReadAllLines(filePath);
                if (lines.Length < 2) return false;
                
                var headers = ParseLine(lines[0]);
                return FindColumnIndex(headers, "Matricule") >= 0 && 
                       FindColumnIndex(headers, new[] { "Altitude", "Alt", "Z" }) >= 0;
            }
            catch
            {
                return false;
            }
        }
        
        private string[] ParseLine(string line)
        {
            return line.Split(_separator);
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
        
        private int FindColumnIndex(string[] headers, string[] possibleNames)
        {
            foreach (var name in possibleNames)
            {
                var index = FindColumnIndex(headers, name);
                if (index >= 0) return index;
            }
            return -1;
        }
    }
}