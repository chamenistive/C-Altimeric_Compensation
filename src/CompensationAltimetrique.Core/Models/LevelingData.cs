using System;
using System.Linq;
using System.Collections.Generic;

namespace CompensationAltimetrique.Core.Models
{
    public class LevelingData
    {
        public string Matricule { get; set; }
        public double? AR1 { get; set; }  // Arrière session 1
        public double? AV1 { get; set; }  // Avant session 1
        public double? AR2 { get; set; }  // Arrière session 2
        public double? AV2 { get; set; }  // Avant session 2
        public double? DIST1 { get; set; } // Distance session 1
        public double? DIST2 { get; set; } // Distance session 2

        public LevelingData(string matricule)
        {
            Matricule = matricule ?? throw new ArgumentNullException(nameof(matricule));
        }

        // Méthodes ajoutées pour compatibilité
        public double CalculateDenivelation()
        {
            if (AR1.HasValue && AV1.HasValue)
                return AR1.Value - AV1.Value;
            return 0.0;
        }

        public double CalculateAverageDenivelation()
        {
            var values = new List<double>();
            if (AR1.HasValue && AV1.HasValue)
                values.Add(AR1.Value - AV1.Value);
            if (AR2.HasValue && AV2.HasValue)
                values.Add(AR2.Value - AV2.Value);
            
            return values.Any() ? values.Average() : 0.0;
        }

        public bool IsConsistent()
        {
            // Vérification basique de cohérence
            if (AR1.HasValue && AV1.HasValue && AR2.HasValue && AV2.HasValue)
            {
                double diff1 = AR1.Value - AV1.Value;
                double diff2 = AR2.Value - AV2.Value;
                return Math.Abs(diff1 - diff2) < 0.01; // Tolérance 1cm
            }
            return true; // Si pas de données doubles, considéré cohérent
        }

        /// <summary>
        /// Calcule la dénivelation pour une session spécifique
        /// </summary>
        public double? CalculateDenivelation(int session)
        {
            if (session == 1 && AR1.HasValue && AV1.HasValue)
                return AR1.Value - AV1.Value;
            if (session == 2 && AR2.HasValue && AV2.HasValue)
                return AR2.Value - AV2.Value;
            return null;
        }

        /// <summary>
        /// Obtient la distance moyenne entre les deux sessions
        /// </summary>
        public double? GetAverageDistance()
        {
            var distances = new List<double>();
            if (DIST1.HasValue) distances.Add(DIST1.Value);
            if (DIST2.HasValue) distances.Add(DIST2.Value);
            
            return distances.Any() ? distances.Average() : null;
        }

        /// <summary>
        /// Vérifie si les données ont des lectures valides
        /// </summary>
        public bool HasValidReadings()
        {
            return (AR1.HasValue && AV1.HasValue) || (AR2.HasValue && AV2.HasValue);
        }

        /// <summary>
        /// Indique si ce point a des données de distance
        /// </summary>
        public bool HasDistances => DIST1.HasValue || DIST2.HasValue;

        /// <summary>
        /// Corrections atmosphériques appliquées (métadonnées)
        /// </summary>
        public List<string> AppliedCorrections { get; set; } = new List<string>();
    }

}
