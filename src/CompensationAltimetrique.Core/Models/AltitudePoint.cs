using System;

namespace CompensationAltimetrique.Core.Models
{
    /// <summary>
    /// Représente un point altimétrique avec ses propriétés géodésiques
    /// </summary>
    public class AltitudePoint
    {
        public string Matricule { get; set; }
        public double Altitude { get; set; }
        public bool IsReference { get; set; }
        public double Precision { get; set; }
        
        public AltitudePoint(string matricule, double altitude, bool isReference = false)
        {
            Matricule = matricule ?? throw new ArgumentNullException(nameof(matricule));
            Altitude = altitude;
            IsReference = isReference;
            Precision = 0.002; // 2mm par défaut
        }
        
        public override string ToString()
        {
            return $"Point {Matricule}: {Altitude:F6}m {(IsReference ? "(REF)" : "")}";
        }
    }
}