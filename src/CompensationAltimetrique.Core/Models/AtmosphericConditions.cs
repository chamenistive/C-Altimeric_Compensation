using System;

namespace CompensationAltimetrique.Core.Models
{
    /// <summary>
    /// Conditions atmosphériques pour les corrections de nivellement
    /// </summary>
    public class AtmosphericConditions
    {
        public double TemperatureCelsius { get; set; }
        public double PressureHpa { get; set; }
        public double HumidityPercent { get; set; }
        public double EarthRadius { get; set; }
        
        /// <summary>
        /// Conditions atmosphériques standard (France métropolitaine)
        /// </summary>
        public AtmosphericConditions()
        {
            TemperatureCelsius = 15.0;
            PressureHpa = 1013.25;
            HumidityPercent = 65.0;
            EarthRadius = 6371000.0; // 6,371 km en mètres
        }
        
        /// <summary>
        /// Conditions atmosphériques personnalisées
        /// </summary>
        public AtmosphericConditions(double temperature, double pressure, double humidity)
        {
            TemperatureCelsius = temperature;
            PressureHpa = pressure;
            HumidityPercent = humidity;
            EarthRadius = 6371000.0;
        }
        
        /// <summary>
        /// Calcule le coefficient de réfraction selon les conditions atmosphériques
        /// </summary>
        public double CalculateRefractionCoefficient()
        {
            // Coefficient de base
            double r = 0.13;
            
            // Effet de la température
            double deltaTemp = -(TemperatureCelsius - 15.0) * 0.004;
            
            // Effet de la pression
            double deltaPress = (PressureHpa - 1013.25) * 0.0001;
            
            // Effet de l'humidité
            double deltaHumid = (HumidityPercent - 60.0) * 0.0002;
            
            // Coefficient final
            return r + deltaTemp + deltaPress + deltaHumid;
        }
        
        public override string ToString()
        {
            return $"T={TemperatureCelsius:F1}°C, P={PressureHpa:F1}hPa, H={HumidityPercent:F1}%, r={CalculateRefractionCoefficient():F3}";
        }
    }
}