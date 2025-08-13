using System;
using System.Collections.Generic;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Console
{
    /// <summary>
    /// Programme principal de compensation altimétrique
    /// </summary>
    class Program
    {
        private const double PRECISION_TARGET_MM = 2.0;
        
        static void Main(string[] args)
        {
            PrintHeader();
            
            try
            {
                if (args.Length > 0)
                {
                    // Mode batch avec arguments
                    ProcessBatchMode(args);
                }
                else
                {
                    // Mode interactif
                    ProcessInteractiveMode();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Erreur : {ex.Message}");
                Environment.Exit(1);
            }
            
            System.Console.WriteLine("\n✅ Traitement terminé avec succès.");
        }
        
        private static void PrintHeader()
        {
            System.Console.WriteLine("================================================================================");
            System.Console.WriteLine("    SYSTÈME DE COMPENSATION ALTIMÉTRIQUE - VERSION C#");
            System.Console.WriteLine($"    Précision cible: {PRECISION_TARGET_MM} mm");
            System.Console.WriteLine("    Développé pour Ubuntu avec Mono");
            System.Console.WriteLine("================================================================================\n");
        }
        
        private static void ProcessBatchMode(string[] args)
        {
            System.Console.WriteLine("🔄 Mode batch détecté...");
            System.Console.WriteLine($"📁 Fichier : {args[0]}");
            
            // TODO: Implémenter le traitement batch
            System.Console.WriteLine("⚠️  Mode batch en cours de développement");
        }
        
        private static void ProcessInteractiveMode()
        {
            System.Console.WriteLine("🎯 Mode interactif activé");
            System.Console.WriteLine("\n📋 Fonctionnalités disponibles :");
            System.Console.WriteLine("1. Import de données Excel/CSV");
            System.Console.WriteLine("2. Calculs de nivellement");
            System.Console.WriteLine("3. Compensation par moindres carrés");
            System.Console.WriteLine("4. Export des résultats");
            
            // Démonstration basique
            TestBasicFunctionality();
        }
        
        private static void TestBasicFunctionality()
        {
            System.Console.WriteLine("\n🧪 Test des fonctionnalités de base...");
            
            // Création de points de test
            var points = new List<AltitudePoint>
            {
                new AltitudePoint("AM2", 125.456, true),
                new AltitudePoint("P001", 125.234),
                new AltitudePoint("P002", 125.890),
                new AltitudePoint("P003", 126.123)
            };
            
            System.Console.WriteLine($"📊 {points.Count} points de test créés :");
            foreach (var point in points)
            {
                System.Console.WriteLine($"   {point}");
            }
            
            System.Console.WriteLine("\n✅ Structure C# fonctionnelle !");
        }
    }
}