#!/usr/bin/env dotnet-script
// Test script for atmospheric corrections

#r "/home/stivax/compensation-csharp/src/CompensationAltimetrique.Core/bin/Release/net8.0/CompensationAltimetrique.Core.dll"

using System;
using CompensationAltimetrique.Calculations.Corrections;

public class AtmosphericTest 
{
    public static void Main()
    {
        Console.WriteLine("=== Test des Corrections Atmosphériques ===");
        
        // Test conditions France
        var franceConditions = AtmosphericConditionsFactory.CreateStandardConditions("france");
        Console.WriteLine($"France: {franceConditions}");
        
        // Test conditions Sahel  
        var sahelConditions = AtmosphericConditionsFactory.CreateStandardConditions("sahel");
        Console.WriteLine($"Sahel: {sahelConditions}");
        
        // Test correction atmosphérique
        var corrector = new AtmosphericCorrector();
        var result = corrector.CalculateAtmosphericCorrection(100.0, 0.0, franceConditions);
        Console.WriteLine($"Correction 100m: {result.TotalCorrectionMm:F3} mm");
        
        Console.WriteLine("✅ Corrections atmosphériques opérationnelles");
    }
}

AtmosphericTest.Main();