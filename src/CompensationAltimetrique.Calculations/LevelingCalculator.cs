using System;
using System.Collections.Generic;
using System.Linq;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Calculations
{
    /// <summary>
    /// Calculateur de base pour nivellement géométrique
    /// </summary>
    public class LevelingCalculator
    {
        protected readonly double _precisionMm;
        
        public LevelingCalculator(double precisionMm = 2.0)
        {
            _precisionMm = precisionMm;
        }
        
        /// <summary>
        /// Calcul de base des altitudes par nivellement simple
        /// </summary>
        public virtual CompensationResults Calculate(List<LevelingData> data, double initialAltitude)
        {
            Console.WriteLine("📐 Calcul de nivellement de base...");
            
            var results = new CompensationResults
            {
                Timestamp = DateTime.Now,
                Status = "Success"
            };
            
            // Ajouter des métadonnées de calcul
            results.ValidationDetails["calculation_type"] = "basic_leveling";
            results.ValidationDetails["precision_mm"] = _precisionMm;
            
            // Calcul des altitudes cumulatives
            double currentAltitude = initialAltitude;
            results.AdjustedPoints.Add(new AltitudePoint("REF", currentAltitude, true));
            
            foreach (var levelingData in data)
            {
                if (levelingData.HasValidReadings())
                {
                    double denivelation = levelingData.CalculateAverageDenivelation();
                    currentAltitude += denivelation;
                    results.AdjustedPoints.Add(new AltitudePoint(levelingData.Matricule, currentAltitude));
                    results.ProcessedPoints++;
                }
            }
            
            results.IsValid = true;
            results.PrecisionAchieved = true;
            
            Console.WriteLine($"✅ {results.ProcessedPoints} points calculés");
            
            return results;
        }
    }
}