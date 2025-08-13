using System.Collections.Generic;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Data.Interfaces
{
    /// <summary>
    /// Interface pour l'import de données de nivellement
    /// </summary>
    public interface IDataImporter
    {
        /// <summary>
        /// Importe les données depuis un fichier
        /// </summary>
        /// <param name="filePath">Chemin vers le fichier</param>
        /// <returns>Liste des points altimétriques</returns>
        List<AltitudePoint> ImportData(string filePath);
        
        /// <summary>
        /// Valide le format du fichier
        /// </summary>
        /// <param name="filePath">Chemin vers le fichier</param>
        /// <returns>True si le format est valide</returns>
        bool ValidateFileFormat(string filePath);
    }
}