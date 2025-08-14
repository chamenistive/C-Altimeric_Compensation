using System;
using System.Collections.Generic;
using System.Linq;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Calculations.Corrections
{
    /// <summary>
    /// Correcteur atmosphérique pour nivellement géométrique
    /// Applique les corrections de courbure terrestre et réfraction atmosphérique
    /// </summary>
    public class AtmosphericCorrector
    {
        private readonly AtmosphericConditions _conditions;
        private readonly bool _applyCorrections;
        
        public AtmosphericCorrector(AtmosphericConditions conditions = null, bool applyCorrections = true)
        {
            _conditions = conditions ?? new AtmosphericConditions();
            _applyCorrections = applyCorrections;
        }
        
        /// <summary>
        /// Applique les corrections atmosphériques à une liste de données de nivellement
        /// </summary>
        public List<LevelingData> ApplyCorrections(List<LevelingData> levelingData)
        {
            if (!_applyCorrections)
            {
                System.Console.WriteLine("⚠️  Corrections atmosphériques désactivées");
                return levelingData;
            }
            
            System.Console.WriteLine("🌡️ Application des corrections atmosphériques...");
            System.Console.WriteLine($"   Conditions: {_conditions}");
            
            var correctedData = new List<LevelingData>();
            double totalCorrection = 0.0;
            int correctedCount = 0;
            
            foreach (var data in levelingData)
            {
                var correctedItem = new LevelingData(data.Matricule);
                
                // Copie des données de base
                correctedItem.AR1 = data.AR1;
                correctedItem.AV1 = data.AV1;
                correctedItem.AR2 = data.AR2;
                correctedItem.AV2 = data.AV2;
                
                // Application des corrections sur les distances et dénivelées
                if (data.DIST1.HasValue)
                {
                    correctedItem.DIST1 = data.DIST1;
                    
                    // Correction de la dénivelée session 1
                    if (data.AR1.HasValue && data.AV1.HasValue)
                    {
                        var correction = CalculateAtmosphericCorrection(data.DIST1.Value);
                        correctedItem.AV1 = data.AV1.Value + correction;
                        totalCorrection += Math.Abs(correction);
                        correctedCount++;
                    }
                }
                
                if (data.DIST2.HasValue)
                {
                    correctedItem.DIST2 = data.DIST2;
                    
                    // Correction de la dénivelée session 2
                    if (data.AR2.HasValue && data.AV2.HasValue)
                    {
                        var correction = CalculateAtmosphericCorrection(data.DIST2.Value);
                        correctedItem.AV2 = data.AV2.Value + correction;
                        totalCorrection += Math.Abs(correction);
                        correctedCount++;
                    }
                }
                
                correctedData.Add(correctedItem);
            }
            
            var avgCorrection = correctedCount > 0 ? totalCorrection / correctedCount : 0.0;
            System.Console.WriteLine($"   ✅ {correctedCount} observations corrigées");
            System.Console.WriteLine($"   📊 Correction moyenne: {avgCorrection * 1000:F2} mm");
            
            return correctedData;
        }
        
        /// <summary>
        /// Calcule la correction atmosphérique pour une distance donnée
        /// </summary>
        /// <param name="distanceMeters">Distance de visée en mètres</param>
        /// <returns>Correction en mètres (à ajouter à la lecture AV)</returns>
        public double CalculateAtmosphericCorrection(double distanceMeters)
        {
            if (!_applyCorrections || distanceMeters <= 0)
                return 0.0;
            
            // Coefficients
            double k = 1.0; // Coefficient de courbure terrestre
            double r = _conditions.CalculateRefractionCoefficient(); // Coefficient de réfraction
            double R = _conditions.EarthRadius; // Rayon terrestre
            
            // Correction totale = (k - r) × d² / (2R)
            // Formule: C = (1 - r) × d² / (2R)
            double correction = (k - r) * distanceMeters * distanceMeters / (2.0 * R);
            
            return correction;
        }
        
        /// <summary>
        /// Évalue l'impact des corrections atmosphériques
        /// </summary>
        public CorrectionAnalysis AnalyzeCorrections(List<LevelingData> levelingData)
        {
            var analysis = new CorrectionAnalysis();
            
            foreach (var data in levelingData)
            {
                if (data.DIST1.HasValue)
                {
                    var correction1 = CalculateAtmosphericCorrection(data.DIST1.Value);
                    analysis.AddCorrection(data.DIST1.Value, correction1);
                }
                
                if (data.DIST2.HasValue)
                {
                    var correction2 = CalculateAtmosphericCorrection(data.DIST2.Value);
                    analysis.AddCorrection(data.DIST2.Value, correction2);
                }
            }
            
            return analysis;
        }
    }
    
    /// <summary>
    /// Analyse des corrections atmosphériques appliquées
    /// </summary>
    public class CorrectionAnalysis
    {
        public List<double> Distances { get; private set; }
        public List<double> Corrections { get; private set; }
        
        public CorrectionAnalysis()
        {
            Distances = new List<double>();
            Corrections = new List<double>();
        }
        
        public void AddCorrection(double distance, double correction)
        {
            Distances.Add(distance);
            Corrections.Add(correction);
        }
        
        public double MaxDistance => Distances.Count > 0 ? Distances.Max() : 0.0;
        public double MaxCorrection => Corrections.Count > 0 ? Corrections.Max() : 0.0;
        public double MinCorrection => Corrections.Count > 0 ? Corrections.Min() : 0.0;
        public double AverageCorrection => Corrections.Count > 0 ? Corrections.Average() : 0.0;
        public double TotalAbsoluteCorrection => Corrections.Sum(Math.Abs);
        public int SignificantCorrections => Corrections.Count(c => Math.Abs(c) > 0.001); // > 1mm
        
        public string GetSummary()
        {
            if (Corrections.Count == 0)
                return "Aucune correction appliquée";
            
            return $"Corrections: {Corrections.Count}, " +
                   $"Max: {MaxCorrection * 1000:F2}mm, " +
                   $"Moyenne: {AverageCorrection * 1000:F2}mm, " +
                   $"Significatives (>1mm): {SignificantCorrections}";
        }
    }
}