using System;
using System.Collections.Generic;

namespace CompensationAltimetrique.Core.Models
{
    /// <summary>
    /// Données complètes de nivellement avec lectures AR/AV et distances
    /// </summary>
    public class LevelingData
    {
        public string Matricule { get; set; }
        public double? AR1 { get; set; }  // Lecture arrière session 1
        public double? AV1 { get; set; }  // Lecture avant session 1
        public double? DIST1 { get; set; } // Distance session 1
        public double? AR2 { get; set; }  // Lecture arrière session 2
        public double? AV2 { get; set; }  // Lecture avant session 2
        public double? DIST2 { get; set; } // Distance session 2
        
        public LevelingData(string matricule)
        {
            Matricule = matricule ?? throw new ArgumentNullException(nameof(matricule));
        }
        
        /// <summary>
        /// Calcule la dénivelée pour la session spécifiée
        /// </summary>
        public double? CalculateDenivelation(int session = 1)
        {
            if (session == 1 && AR1.HasValue && AV1.HasValue)
                return AR1.Value - AV1.Value;
            else if (session == 2 && AR2.HasValue && AV2.HasValue)
                return AR2.Value - AV2.Value;
            
            return null;
        }
        
        /// <summary>
        /// Calcule la dénivelée moyenne des deux sessions
        /// </summary>
        public double? CalculateAverageDenivelation()
        {
            var dh1 = CalculateDenivelation(1);
            var dh2 = CalculateDenivelation(2);
            
            if (dh1.HasValue && dh2.HasValue)
                return (dh1.Value + dh2.Value) / 2.0;
            else if (dh1.HasValue)
                return dh1.Value;
            else if (dh2.HasValue)
                return dh2.Value;
                
            return null;
        }
        
        /// <summary>
        /// Vérifie la cohérence entre les deux sessions
        /// </summary>
        public bool IsConsistent(double toleranceMs = 3.0)
        {
            var dh1 = CalculateDenivelation(1);
            var dh2 = CalculateDenivelation(2);
            
            if (dh1.HasValue && dh2.HasValue)
            {
                var diff = Math.Abs(dh1.Value - dh2.Value) * 1000; // en mm
                return diff <= toleranceMs;
            }
            
            return true; // Si une seule session, considéré comme cohérent
        }
        
        public override string ToString()
        {
            var dh = CalculateAverageDenivelation();
            return $"Point {Matricule}: ΔH={dh?.ToString("F6") ?? "N/A"}m";
        }
    }
}