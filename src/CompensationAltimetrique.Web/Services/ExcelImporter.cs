// ================================================================
// IMPORTATEUR EXCEL TEMPORAIRE
// Classe temporaire pour l'interface web avec données de test
// ================================================================

using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Web.Services
{
    public class ExcelLevelingImporter
    {
        public IEnumerable<LevelingData> ImportFromFile(string filePath)
        {
            // Pour la démonstration, retournons des données de test
            var data = new List<LevelingData>
            {
                new LevelingData("PT001")
                {
                    AR1 = 1.234,
                    AV1 = 1.657,
                    DIST1 = 150.5,
                    AR2 = 2.183,
                    AV2 = 2.606,
                    DIST2 = 150.5
                },
                new LevelingData("PT002")
                {
                    AR1 = 1.789,
                    AV1 = 1.456,
                    DIST1 = 175.2,
                    AR2 = 2.234,
                    AV2 = 1.901,
                    DIST2 = 175.2
                },
                new LevelingData("PT003")
                {
                    AR1 = 2.345,
                    AV1 = 1.987,
                    DIST1 = 200.8,
                    AR2 = 2.678,
                    AV2 = 2.320,
                    DIST2 = 200.8
                },
                new LevelingData("PT004")
                {
                    AR1 = 1.567,
                    AV1 = 2.123,
                    DIST1 = 125.3,
                    AR2 = 1.890,
                    AV2 = 2.446,
                    DIST2 = 125.3
                },
                new LevelingData("PT005")
                {
                    AR1 = 2.012,
                    AV1 = 1.789,
                    DIST1 = 180.7,
                    AR2 = 2.567,
                    AV2 = 2.344,
                    DIST2 = 180.7
                }
            };
            
            // Simulation d'un délai de lecture
            Task.Delay(1000).Wait();
            
            return data;
        }
    }
}