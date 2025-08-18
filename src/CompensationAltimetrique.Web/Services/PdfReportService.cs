using iTextSharp.text;
using iTextSharp.text.pdf;
using CompensationAltimetrique.Calculations.Design;
using CompensationAltimetrique.Web.Services;

namespace CompensationAltimetrique.Web.Services
{
    public class PdfReportService
    {
        public byte[] GenerateCompensationReport(DetailedCompensationResults results, CompensationService service)
        {
            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, 40, 40, 40, 40);
            var writer = PdfWriter.GetInstance(document, memoryStream);
            
            document.Open();
            
            try
            {
                // Polices
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DARK_GRAY);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLACK);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.GRAY);
                
                // Titre principal
                var title = new Paragraph("RAPPORT DE COMPENSATION ALTIMÉTRIQUE", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(title);
                
                // Informations générales
                var infoTable = new PdfPTable(2) { WidthPercentage = 100 };
                infoTable.SetWidths(new float[] { 1f, 1f });
                
                infoTable.AddCell(CreateCell("Date du rapport", headerFont));
                infoTable.AddCell(CreateCell(DateTime.Now.ToString("dd/MM/yyyy HH:mm"), normalFont));
                
                infoTable.AddCell(CreateCell("Points traités", headerFont));
                infoTable.AddCell(CreateCell(service.DataCount.ToString(), normalFont));
                
                infoTable.AddCell(CreateCell("Temps de calcul", headerFont));
                infoTable.AddCell(CreateCell(service.ComputationTime, normalFont));
                
                infoTable.AddCell(CreateCell("RMS des résidus", headerFont));
                infoTable.AddCell(CreateCell($"{results?.RmsResiduals:F3} mm", normalFont));
                
                infoTable.AddCell(CreateCell("Version logiciel", headerFont));
                infoTable.AddCell(CreateCell("DickPy C# 2.0", normalFont));
                
                document.Add(infoTable);
                document.Add(new Paragraph(" ", normalFont)); // Espacement
                
                // Paramètres de compensation
                var paramHeader = new Paragraph("PARAMÈTRES DE COMPENSATION", headerFont)
                {
                    SpacingBefore = 15,
                    SpacingAfter = 10
                };
                document.Add(paramHeader);
                
                var paramTable = new PdfPTable(2) { WidthPercentage = 100 };
                paramTable.SetWidths(new float[] { 1f, 1f });
                
                paramTable.AddCell(CreateCell("Point de référence", normalFont));
                paramTable.AddCell(CreateCell(service.ReferencePointId ?? "N/A", normalFont));
                
                paramTable.AddCell(CreateCell("Altitude de référence", normalFont));
                paramTable.AddCell(CreateCell($"{service.ReferenceAltitude:F3} m", normalFont));
                
                paramTable.AddCell(CreateCell("Précision cible", normalFont));
                paramTable.AddCell(CreateCell($"{service.TargetPrecision:F1} mm", normalFont));
                
                paramTable.AddCell(CreateCell("Erreur instrumentale", normalFont));
                paramTable.AddCell(CreateCell($"{service.InstrumentalError:F1} mm", normalFont));
                
                paramTable.AddCell(CreateCell("Erreur kilométrique", normalFont));
                paramTable.AddCell(CreateCell($"{service.KilometricError:F1} mm/km", normalFont));
                
                document.Add(paramTable);
                
                // Résultats
                if (results?.AdjustedPoints != null && results.AdjustedPoints.Any())
                {
                    var resultsHeader = new Paragraph("RÉSULTATS DE COMPENSATION", headerFont)
                    {
                        SpacingBefore = 20,
                        SpacingAfter = 10
                    };
                    document.Add(resultsHeader);
                    
                    var resultsTable = new PdfPTable(4) { WidthPercentage = 100 };
                    resultsTable.SetWidths(new float[] { 2f, 2f, 1.5f, 1.5f });
                    
                    // En-têtes
                    resultsTable.AddCell(CreateHeaderCell("Point", headerFont));
                    resultsTable.AddCell(CreateHeaderCell("Altitude (m)", headerFont));
                    resultsTable.AddCell(CreateHeaderCell("Précision (mm)", headerFont));
                    resultsTable.AddCell(CreateHeaderCell("Statut", headerFont));
                    
                    // Données
                    foreach (var point in results.AdjustedPoints.Take(20)) // Limiter pour éviter de surcharger
                    {
                        resultsTable.AddCell(CreateCell(point.Matricule, normalFont));
                        resultsTable.AddCell(CreateCell($"{point.Altitude:F4}", normalFont));
                        resultsTable.AddCell(CreateCell($"{(point.Precision * 1000):F1}", normalFont));
                        resultsTable.AddCell(CreateCell("Validé", normalFont));
                    }
                    
                    if (results.AdjustedPoints.Count > 20)
                    {
                        resultsTable.AddCell(CreateCell($"... et {results.AdjustedPoints.Count - 20} autres points", smallFont, 4));
                    }
                    
                    document.Add(resultsTable);
                }
                
                // Pied de page
                document.Add(new Paragraph(" ", normalFont));
                var footer = new Paragraph("Rapport généré automatiquement par DickPy C# 2.0", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 20
                };
                document.Add(footer);
            }
            finally
            {
                document.Close();
            }
            
            return memoryStream.ToArray();
        }
        
        private PdfPCell CreateCell(string text, Font font, int colspan = 1)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                Padding = 5,
                Border = Rectangle.BOX,
                Colspan = colspan
            };
            return cell;
        }
        
        private PdfPCell CreateHeaderCell(string text, Font font)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                Padding = 5,
                Border = Rectangle.BOX,
                BackgroundColor = BaseColor.LIGHT_GRAY,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            return cell;
        }
    }
}