#!/bin/bash

echo "🔧 Correction des dépendances manquantes..."

# Créer le projet Core manquant
echo "📁 Création du projet Core..."
mkdir -p src/CompensationAltimetrique.Core/Models

cat > src/CompensationAltimetrique.Core/CompensationAltimetrique.Core.csproj << 'EOC'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
EOC

# Créer le modèle LevelingData
cat > src/CompensationAltimetrique.Core/Models/LevelingData.cs << 'EOL'
using System;

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
    }

    public class CompensationResults
    {
        public string Status { get; set; } = "Success";
        public double Precision { get; set; }
        public string Details { get; set; } = "";
    }
}
EOL

# Créer les conditions atmosphériques dans le bon namespace
cat > src/CompensationAltimetrique.Calculations/Corrections/AtmosphericConditions.cs << 'EOA'
using System;

namespace CompensationAltimetrique.Calculations.Corrections
{
    public class AtmosphericConditions
    {
        public double TemperatureCelsius { get; set; } = 15.0;
        public double PressureHpa { get; set; } = 1013.25;
        public double HumidityPercent { get; set; } = 60.0;
        public DateTime? TimeOfDay { get; set; } = null;
        public string WeatherCondition { get; set; } = "normal";
        public double EarthRadius { get; set; } = 6371000.0;

        public AtmosphericConditions() { }

        public AtmosphericConditions(double temperature, double pressure, double humidity)
        {
            TemperatureCelsius = temperature;
            PressureHpa = pressure;
            HumidityPercent = humidity;
        }

        public double CalculateRefractionCoefficient()
        {
            try
            {
                double k_base = 0.13;
                double temp_correction = -(TemperatureCelsius - 15.0) * 0.004;
                double pressure_correction = (PressureHpa - 1013.25) * 0.0001;
                double humidity_correction = (HumidityPercent - 60.0) * 0.0002;

                double time_correction = 0.0;
                if (TimeOfDay.HasValue)
                {
                    int hour = TimeOfDay.Value.Hour;
                    if (hour >= 10 && hour <= 16)
                        time_correction = 0.02;
                    else if (hour <= 8 || hour >= 18)
                        time_correction = -0.01;
                }

                double k_adjusted = k_base + temp_correction + pressure_correction + 
                                  humidity_correction + time_correction;

                k_adjusted = Math.Max(0.05, Math.Min(0.25, k_adjusted));
                return k_adjusted;
            }
            catch (Exception)
            {
                return 0.13;
            }
        }

        public override string ToString()
        {
            return $"T={TemperatureCelsius:F1}°C, P={PressureHpa:F1}hPa, H={HumidityPercent:F1}%, r={CalculateRefractionCoefficient():F3}";
        }
    }

    public static class AtmosphericConditionsFactory
    {
        public static AtmosphericConditions CreateStandardConditions(string region = "france")
        {
            return region.ToLower() switch
            {
                "france" => new AtmosphericConditions(15.0, 1013.25, 65.0),
                "sahel" => new AtmosphericConditions(32.0, 1008.0, 40.0),
                "tropical" => new AtmosphericConditions(28.0, 1010.0, 80.0),
                "arid" => new AtmosphericConditions(35.0, 1005.0, 25.0),
                _ => new AtmosphericConditions()
            };
        }
    }
}
EOA

# Ajouter le projet Core à la solution
dotnet sln add src/CompensationAltimetrique.Core/CompensationAltimetrique.Core.csproj

# Ajouter la référence Core au projet Calculations
dotnet add src/CompensationAltimetrique.Calculations/CompensationAltimetrique.Calculations.csproj reference src/CompensationAltimetrique.Core/CompensationAltimetrique.Core.csproj

echo "✅ Dépendances corrigées"
