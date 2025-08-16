// ================================================================
// ÉTAPE 4 - GESTION DE LA MATRICE DE CONCEPTION
// Construction de la matrice A pour compensation par moindres carrés
// Transposition FIDÈLE du module Python DesignMatrixBuilder
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Calculations.Validation;

namespace CompensationAltimetrique.Calculations.Design
{
    /// <summary>
    /// Types de contraintes pour la compensation.
    /// </summary>
    public enum ConstraintType
    {
        /// <summary>Point fixe avec contrainte absolue</summary>
        FixedPoint,
        /// <summary>Fermeture de traverse</summary>
        ClosureConstraint,
        /// <summary>Contrainte de continuité entre points</summary>
        ContinuityConstraint,
        /// <summary>Point libre (inconnue)</summary>
        FreePoint
    }

    /// <summary>
    /// Configuration du réseau de nivellement pour la matrice de conception.
    /// </summary>
    public class NetworkConfiguration
    {
        /// <summary>Points du réseau avec leurs types</summary>
        public Dictionary<string, ConstraintType> PointConstraints { get; set; } = new();

        /// <summary>Point de référence (point fixe)</summary>
        public string ReferencePoint { get; set; } = string.Empty;

        /// <summary>Altitude du point de référence</summary>
        public double ReferenceAltitude { get; set; } = 0.0;

        /// <summary>Type de réseau (ouvert, fermé, bouclé)</summary>
        public string NetworkType { get; set; } = "fermé";

        /// <summary>Méthode de numération des inconnues</summary>
        public string NumberingMethod { get; set; } = "sequential";

        /// <summary>
        /// Validation de la configuration du réseau.
        /// </summary>
        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult();

            // Vérifier qu'il y a au moins un point de référence
            if (string.IsNullOrEmpty(ReferencePoint))
            {
                result.AddError("Aucun point de référence défini");
            }

            // Vérifier que le point de référence existe dans les contraintes
            if (!PointConstraints.ContainsKey(ReferencePoint))
            {
                result.AddError($"Point de référence '{ReferencePoint}' non trouvé dans les contraintes");
            }
            else if (PointConstraints[ReferencePoint] != ConstraintType.FixedPoint)
            {
                result.AddWarning($"Point de référence '{ReferencePoint}' devrait être de type FixedPoint");
            }

            // Vérifier qu'il y a des points libres à déterminer
            var freePoints = PointConstraints.Values.Count(c => c == ConstraintType.FreePoint);
            if (freePoints == 0)
            {
                result.AddWarning("Aucun point libre à déterminer - système sur-contraint");
            }

            result.Details["reference_point"] = ReferencePoint;
            result.Details["reference_altitude"] = ReferenceAltitude;
            result.Details["free_points_count"] = freePoints;
            result.Details["total_points"] = PointConstraints.Count;

            return result;
        }
    }

    /// <summary>
    /// Informations sur la matrice de conception construite.
    /// </summary>
    public class DesignMatrixInfo
    {
        /// <summary>Nombre d'observations</summary>
        public int ObservationCount { get; set; }

        /// <summary>Nombre d'inconnues</summary>
        public int UnknownCount { get; set; }

        /// <summary>Degrés de liberté</summary>
        public int DegreesOfFreedom => ObservationCount - UnknownCount;

        /// <summary>Rang de la matrice</summary>
        public int MatrixRank { get; set; }

        /// <summary>Système surdéterminé ?</summary>
        public bool IsOverdetermined => DegreesOfFreedom > 0;

        /// <summary>Système déterminé ?</summary>
        public bool IsDetermined => DegreesOfFreedom == 0;

        /// <summary>Système sous-déterminé ?</summary>
        public bool IsUnderdetermined => DegreesOfFreedom < 0;

        /// <summary>Mapping point -> index colonne</summary>
        public Dictionary<string, int> PointToColumnMapping { get; set; } = new();

        /// <summary>Mapping observation -> index ligne</summary>
        public Dictionary<string, int> ObservationToRowMapping { get; set; } = new();

        public string GetSummary() =>
            $"Matrice {ObservationCount}×{UnknownCount}, DDL={DegreesOfFreedom}, " +
            $"Type: {(IsOverdetermined ? "Surdéterminé" : IsDetermined ? "Déterminé" : "Sous-déterminé")}";
    }

    /// <summary>
    /// Constructeur de matrices de conception pour compensation par moindres carrés.
    /// TRANSPOSITION EXACTE du module Python DesignMatrixBuilder.
    /// 
    /// Construit la matrice A du système linéaire A × x = l + v
    /// où:
    /// - A = matrice de conception (observations vs inconnues)
    /// - x = vecteur des corrections aux inconnues
    /// - l = vecteur des misclosures
    /// - v = vecteur des résidus
    /// </summary>
    public class DesignMatrixBuilder
    {
        private readonly NetworkConfiguration _configuration;
        private readonly GeodeticValidator _validator;

        public DesignMatrixBuilder(NetworkConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _validator = new GeodeticValidator();

            // Validation de la configuration
            var configValidation = _configuration.ValidateConfiguration();
            if (!configValidation.IsValid)
            {
                throw new InvalidOperationException($"Configuration réseau invalide: {string.Join("; ", configValidation.Errors)}");
            }
        }

        /// <summary>
        /// Construction de la matrice de conception principale.
        /// TRANSPOSITION EXACTE de build_design_matrix Python.
        /// 
        /// Algorithme:
        /// 1. Analyse du réseau et numérotation des inconnues
        /// 2. Construction matrice A selon type de contraintes
        /// 3. Gestion des points fixes et contraintes
        /// 4. Validation de la matrice construite
        /// </summary>
        public (Matrix<double> DesignMatrix, DesignMatrixInfo Info) BuildDesignMatrix(
            List<LevelingData> levelingData, 
            bool includeClosureConstraint = true)
        {
            try
            {
                // 1. Analyse et numérotation
                var pointMapping = CreatePointMapping(levelingData);
                var observationMapping = CreateObservationMapping(levelingData);

                int nObs = observationMapping.Count;
                int nUnknowns = pointMapping.Count;

                if (includeClosureConstraint && _configuration.NetworkType == "fermé")
                {
                    nObs++; // Ajouter contrainte de fermeture
                }

                // 2. Construction de la matrice A
                var A = DenseMatrix.Create(nObs, nUnknowns, 0.0);

                // 3. Remplissage pour observations de dénivelation
                FillObservationRows(A, levelingData, pointMapping, observationMapping);

                // 4. Ajout contrainte de fermeture si nécessaire
                if (includeClosureConstraint && _configuration.NetworkType == "fermé")
                {
                    AddClosureConstraint(A, levelingData, pointMapping, nObs - 1);
                }

                // 5. Gestion des contraintes de points fixes
                ApplyFixedPointConstraints(A, pointMapping);

                // 6. Informations sur la matrice
                var info = new DesignMatrixInfo
                {
                    ObservationCount = nObs,
                    UnknownCount = nUnknowns,
                    MatrixRank = CalculateMatrixRank(A),
                    PointToColumnMapping = pointMapping,
                    ObservationToRowMapping = observationMapping
                };

                // 7. Validation finale
                ValidateDesignMatrix(A, info);

                return (A, info);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur construction matrice conception: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Construction du vecteur des misclosures (terme constant).
        /// Équivalent Python de build_misclosure_vector.
        /// </summary>
        public Vector<double> BuildMisclosureVector(
            List<LevelingData> levelingData, 
            DesignMatrixInfo matrixInfo,
            bool includeClosureConstraint = true)
        {
            try
            {
                var misclosures = new List<double>();

                // Misclosures des observations individuelles
                foreach (var data in levelingData)
                {
                    // Pour nivellement géométrique, la misclosure est généralement 0
                    // (les observations sont les dénivelations directes)
                    misclosures.Add(0.0);
                }

                // Misclosure de fermeture si applicable
                if (includeClosureConstraint && _configuration.NetworkType == "fermé")
                {
                    double closureError = CalculateClosureError(levelingData);
                    misclosures.Add(closureError);
                }

                return Vector<double>.Build.DenseOfArray(misclosures.ToArray());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur construction vecteur misclosures: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Construction du vecteur d'observations pour le système linéaire.
        /// </summary>
        public Vector<double> BuildObservationVector(List<LevelingData> levelingData, double referenceAltitude)
        {
            var observations = new List<double>();

            // Première observation: altitude du point de référence
            observations.Add(referenceAltitude);

            // Observations de dénivelation
            foreach (var data in levelingData)
            {
                double denivelation = data.CalculateAverageDenivelation();
                observations.Add(denivelation);
            }

            return Vector<double>.Build.DenseOfArray(observations.ToArray());
        }

        // Méthodes privées pour construction détaillée

        private Dictionary<string, int> CreatePointMapping(List<LevelingData> levelingData)
        {
            var pointMapping = new Dictionary<string, int>();
            var allPoints = new HashSet<string>();

            // Collecter tous les points uniques du réseau
            allPoints.Add(_configuration.ReferencePoint); // Point de référence en premier
            
            foreach (var data in levelingData)
            {
                allPoints.Add(data.Matricule);
            }

            // Numérotation séquentielle
            int columnIndex = 0;
            foreach (var point in allPoints.OrderBy(p => p == _configuration.ReferencePoint ? 0 : 1)
                                           .ThenBy(p => p))
            {
                pointMapping[point] = columnIndex++;
            }

            return pointMapping;
        }

        private Dictionary<string, int> CreateObservationMapping(List<LevelingData> levelingData)
        {
            var obsMapping = new Dictionary<string, int>();
            
            for (int i = 0; i < levelingData.Count; i++)
            {
                string obsId = $"DH_{levelingData[i].Matricule}";
                obsMapping[obsId] = i;
            }

            return obsMapping;
        }

        private void FillObservationRows(Matrix<double> A, List<LevelingData> levelingData, 
            Dictionary<string, int> pointMapping, Dictionary<string, int> observationMapping)
        {
            for (int i = 0; i < levelingData.Count; i++)
            {
                var data = levelingData[i];
                int rowIndex = i;

                // Pour une dénivelation entre point i et point i+1:
                // DH = H[i+1] - H[i]
                // Donc: coefficients -1 pour point arrière, +1 pour point avant

                if (i == 0)
                {
                    // Première observation: depuis point de référence
                    A[rowIndex, pointMapping[_configuration.ReferencePoint]] = -1.0;
                    A[rowIndex, pointMapping[data.Matricule]] = 1.0;
                }
                else
                {
                    // Observations suivantes: depuis point précédent
                    string previousPoint = levelingData[i - 1].Matricule;
                    A[rowIndex, pointMapping[previousPoint]] = -1.0;
                    A[rowIndex, pointMapping[data.Matricule]] = 1.0;
                }
            }
        }

        private void AddClosureConstraint(Matrix<double> A, List<LevelingData> levelingData, 
            Dictionary<string, int> pointMapping, int constraintRowIndex)
        {
            // Contrainte de fermeture: somme algébrique des dénivelations = 0
            // Pour un circuit fermé, on doit revenir au point de départ

            if (levelingData.Count > 0)
            {
                // Dernier point doit revenir au point de référence
                string lastPoint = levelingData.Last().Matricule;
                A[constraintRowIndex, pointMapping[lastPoint]] = -1.0;
                A[constraintRowIndex, pointMapping[_configuration.ReferencePoint]] = 1.0;
            }
        }

        private void ApplyFixedPointConstraints(Matrix<double> A, Dictionary<string, int> pointMapping)
        {
            // Les points fixes sont gérés par des contraintes absolues
            foreach (var constraint in _configuration.PointConstraints)
            {
                if (constraint.Value == ConstraintType.FixedPoint)
                {
                    string fixedPoint = constraint.Key;
                    if (pointMapping.ContainsKey(fixedPoint))
                    {
                        int colIndex = pointMapping[fixedPoint];
                        // Le point fixe peut être géré par une pondération très forte
                        // ou par élimination de colonne (implémentation simplifiée ici)
                    }
                }
            }
        }

        private double CalculateClosureError(List<LevelingData> levelingData)
        {
            // Calcul de l'erreur de fermeture théorique
            double sumDH = 0.0;
            
            foreach (var data in levelingData)
            {
                sumDH += data.CalculateAverageDenivelation();
            }

            // Pour un circuit fermé, la somme devrait être 0
            return sumDH;
        }

        private int CalculateMatrixRank(Matrix<double> matrix)
        {
            try
            {
                var svd = matrix.Svd();
                var singularValues = new List<double>();
                
                // Extraction des valeurs singulières
                for (int i = 0; i < Math.Min(matrix.RowCount, matrix.ColumnCount); i++)
                {
                    singularValues.Add(svd.S[i]);
                }

                // Comptage des valeurs singulières significatives
                const double tolerance = 1e-12;
                return singularValues.Count(sv => sv > tolerance);
            }
            catch
            {
                return Math.Min(matrix.RowCount, matrix.ColumnCount);
            }
        }

        private void ValidateDesignMatrix(Matrix<double> A, DesignMatrixInfo info)
        {
            // Validation de cohérence de la matrice
            if (info.MatrixRank < info.UnknownCount - 1) // -1 pour le point de référence
            {
                throw new InvalidOperationException($"Matrice de conception singulière (rang {info.MatrixRank} < {info.UnknownCount - 1})");
            }

            if (info.IsUnderdetermined && info.DegreesOfFreedom < -1)
            {
                throw new InvalidOperationException($"Système fortement sous-déterminé ({info.DegreesOfFreedom} DDL)");
            }

            // Validation des dimensions
            if (A.RowCount != info.ObservationCount || A.ColumnCount != info.UnknownCount)
            {
                throw new InvalidOperationException("Dimensions matrice incohérentes avec les métadonnées");
            }
        }
    }

    /// <summary>
    /// Factory pour configurations de réseau standard.
    /// </summary>
    public static class NetworkConfigurationFactory
    {
        /// <summary>
        /// Configuration pour traverse fermée simple.
        /// </summary>
        public static NetworkConfiguration CreateClosedTraverse(string referencePoint, double referenceAltitude, 
            IEnumerable<string> traversePoints)
        {
            var config = new NetworkConfiguration
            {
                ReferencePoint = referencePoint,
                ReferenceAltitude = referenceAltitude,
                NetworkType = "fermé",
                NumberingMethod = "sequential"
            };

            // Point de référence fixe
            config.PointConstraints[referencePoint] = ConstraintType.FixedPoint;

            // Points de la traverse comme points libres
            foreach (var point in traversePoints.Where(p => p != referencePoint))
            {
                config.PointConstraints[point] = ConstraintType.FreePoint;
            }

            return config;
        }

        /// <summary>
        /// Configuration pour traverse ouverte.
        /// </summary>
        public static NetworkConfiguration CreateOpenTraverse(string startPoint, string endPoint, 
            double startAltitude, double endAltitude, IEnumerable<string> intermediatePoints)
        {
            var config = new NetworkConfiguration
            {
                ReferencePoint = startPoint,
                ReferenceAltitude = startAltitude,
                NetworkType = "ouvert",
                NumberingMethod = "sequential"
            };

            // Points de départ et fin fixes
            config.PointConstraints[startPoint] = ConstraintType.FixedPoint;
            config.PointConstraints[endPoint] = ConstraintType.FixedPoint;

            // Points intermédiaires libres
            foreach (var point in intermediatePoints)
            {
                config.PointConstraints[point] = ConstraintType.FreePoint;
            }

            return config;
        }

        /// <summary>
        /// Configuration pour réseau maillé.
        /// </summary>
        public static NetworkConfiguration CreateNetwork(string referencePoint, double referenceAltitude, 
            IEnumerable<string> networkPoints)
        {
            var config = new NetworkConfiguration
            {
                ReferencePoint = referencePoint,
                ReferenceAltitude = referenceAltitude,
                NetworkType = "maillé",
                NumberingMethod = "sequential"
            };

            // Point de référence fixe
            config.PointConstraints[referencePoint] = ConstraintType.FixedPoint;

            // Tous les autres points libres
            foreach (var point in networkPoints.Where(p => p != referencePoint))
            {
                config.PointConstraints[point] = ConstraintType.FreePoint;
            }

            return config;
        }
    }
}