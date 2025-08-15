using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using CompensationAltimetrique.Core.Models;

namespace CompensationAltimetrique.Calculations.Algorithms
{
    /// <summary>
    /// Compensateur par moindres carrés pour données de nivellement
    /// </summary>
    public class LeastSquaresCompensator
    {
        private readonly double _precisionMm;
        
        public LeastSquaresCompensator(double precisionMm = 2.0)
        {
            _precisionMm = precisionMm;
        }
        
        /// <summary>
        /// Compensation d'un réseau de nivellement
        /// </summary>
        public CompensationResults Compensate(List<LevelingData> levelingData, double initialAltitude = 125.456)
        {
            System.Console.WriteLine("🔧 Début de la compensation par moindres carrés...");
            
            // 1. Préparation des données
            var validData = levelingData.Where(d => d.CalculateAverageDenivelation() != 0).ToList();
            int n = validData.Count + 1; // +1 pour le point de référence
            
            System.Console.WriteLine($"📊 {validData.Count} dénivelations valides, {n} points total");
            
            // 2. Construction du système d'équations A × X = L
            var observations = new List<double>();
            var designMatrix = new List<double[]>();
            var weights = new List<double>();
            
            // Point de référence (première ligne)
            observations.Add(initialAltitude);
            var refRow = new double[n];
            refRow[0] = 1.0; // Contrainte sur le premier point
            designMatrix.Add(refRow);
            weights.Add(1000.0); // Poids très fort pour la référence
            
            // Équations d'observation pour chaque dénivelation
            for (int i = 0; i < validData.Count; i++)
            {
                var dh = validData[i].CalculateAverageDenivelation();
                observations.Add(dh);
                
                var row = new double[n];
                row[i] = -1.0;     // Point arrière
                row[i + 1] = 1.0;  // Point avant
                designMatrix.Add(row);
                
                // Poids basé sur la distance (approximative)
                var dist = (validData[i].DIST1 ?? validData[i].DIST2 ?? 100.0) / 1000.0; // en km
                weights.Add(1.0 / (dist * dist)); // Poids inversement proportionnel au carré de la distance
            }
            
            // 3. Conversion en matrices
            int m = observations.Count; // Nombre d'observations
            var A = new double[m, n];
            var L = new double[m, 1];
            var P = new double[m, m]; // Matrice des poids (diagonale)
            
            for (int i = 0; i < m; i++)
            {
                L[i, 0] = observations[i];
                P[i, i] = weights[i];
                for (int j = 0; j < n; j++)
                {
                    A[i, j] = designMatrix[i][j];
                }
            }
            
            System.Console.WriteLine($"📐 Système: A({m}×{n}), L({m}×1), P({m}×{m})");
            
            // 4. Résolution: X = (A^T × P × A)^(-1) × A^T × P × L
            try
            {
                var At = MatrixCalculator.Transpose(A);
                var AtP = MatrixCalculator.Multiply(At, P);
                var AtPA = MatrixCalculator.Multiply(AtP, A);
                var AtPAinv = MatrixCalculator.Invert(AtPA);
                var AtPL = MatrixCalculator.Multiply(AtP, L);
                var X = MatrixCalculator.Multiply(AtPAinv, AtPL);
                
                System.Console.WriteLine("✅ Système résolu avec succès");
                
                // 5. Calcul des résidus et statistiques
                var AX = MatrixCalculator.Multiply(A, X);
                var residuals = new double[m];
                double sumSquaredResiduals = 0;
                
                for (int i = 0; i < m; i++)
                {
                    residuals[i] = L[i, 0] - AX[i, 0];
                    sumSquaredResiduals += residuals[i] * residuals[i] * P[i, i];
                }
                
                // Degrés de liberté
                int dof = m - n;
                double sigma0 = dof > 0 ? Math.Sqrt(sumSquaredResiduals / dof) : 0.0;
                
                // 6. Construction des résultats
                var results = new CompensationResults
                {
                    Sigma0 = sigma0,
                    Corrections = Vector<double>.Build.DenseOfArray(new double[n]),
                    Residuals = Vector<double>.Build.DenseOfArray(residuals),
                    RmsResiduals = Math.Sqrt(sumSquaredResiduals / m),
                    IsValid = sigma0 < 0.005 // 5mm
                };
                
                // Altitudes ajustées
                results.AdjustedPoints.Add(new AltitudePoint("AM2", X[0, 0], true));
                
                for (int i = 0; i < validData.Count; i++)
                {
                    var altitude = X[i + 1, 0];
                    results.AdjustedPoints.Add(new AltitudePoint(validData[i].Matricule, altitude));
                    results.Corrections[i + 1] = altitude - (X[0, 0] + GetCumulativeDH(validData, i));
                }
                
                results.MaxCorrection = results.Corrections.AbsoluteMaximum();
                
                System.Console.WriteLine($"📈 Compensation terminée: σ₀ = {sigma0*1000:F1} mm");
                
                return results;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Erreur lors de la compensation: {ex.Message}");
                return new CompensationResults { IsValid = false };
            }
        }
        
        private double GetCumulativeDH(List<LevelingData> data, int index)
        {
            double cumul = 0;
            for (int i = 0; i <= index; i++)
            {
                cumul += data[i].CalculateAverageDenivelation();
            }
            return cumul;
        }
    }
}