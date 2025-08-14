using System;

namespace CompensationAltimetrique.Calculations.Algorithms
{
    /// <summary>
    /// Calculateur de matrices pour la compensation par moindres carrés
    /// </summary>
    public static class MatrixCalculator
    {
        /// <summary>
        /// Multiplication de matrices A × B
        /// </summary>
        public static double[,] Multiply(double[,] A, double[,] B)
        {
            int rowsA = A.GetLength(0);
            int colsA = A.GetLength(1);
            int colsB = B.GetLength(1);
            
            if (colsA != B.GetLength(0))
                throw new ArgumentException("Dimensions incompatibles pour la multiplication");
            
            var result = new double[rowsA, colsB];
            
            for (int i = 0; i < rowsA; i++)
            {
                for (int j = 0; j < colsB; j++)
                {
                    for (int k = 0; k < colsA; k++)
                    {
                        result[i, j] += A[i, k] * B[k, j];
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Transposée d'une matrice
        /// </summary>
        public static double[,] Transpose(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            var result = new double[cols, rows];
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[j, i] = matrix[i, j];
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Inversion d'une matrice par élimination de Gauss-Jordan
        /// </summary>
        public static double[,] Invert(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            if (n != matrix.GetLength(1))
                throw new ArgumentException("La matrice doit être carrée");
            
            // Création de la matrice augmentée [A|I]
            var augmented = new double[n, 2 * n];
            
            // Copie de A et création de I
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    augmented[i, j] = matrix[i, j];
                    augmented[i, j + n] = (i == j) ? 1.0 : 0.0;
                }
            }
            
            // Élimination de Gauss-Jordan
            for (int i = 0; i < n; i++)
            {
                // Recherche du pivot
                int pivotRow = i;
                for (int k = i + 1; k < n; k++)
                {
                    if (Math.Abs(augmented[k, i]) > Math.Abs(augmented[pivotRow, i]))
                        pivotRow = k;
                }
                
                // Échange de lignes si nécessaire
                if (pivotRow != i)
                {
                    for (int j = 0; j < 2 * n; j++)
                    {
                        var temp = augmented[i, j];
                        augmented[i, j] = augmented[pivotRow, j];
                        augmented[pivotRow, j] = temp;
                    }
                }
                
                // Vérification de la singularité
                if (Math.Abs(augmented[i, i]) < 1e-12)
                    throw new InvalidOperationException("Matrice singulière - inversion impossible");
                
                // Normalisation de la ligne pivot
                double pivot = augmented[i, i];
                for (int j = 0; j < 2 * n; j++)
                {
                    augmented[i, j] /= pivot;
                }
                
                // Élimination
                for (int k = 0; k < n; k++)
                {
                    if (k != i)
                    {
                        double factor = augmented[k, i];
                        for (int j = 0; j < 2 * n; j++)
                        {
                            augmented[k, j] -= factor * augmented[i, j];
                        }
                    }
                }
            }
            
            // Extraction de la matrice inverse
            var inverse = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    inverse[i, j] = augmented[i, j + n];
                }
            }
            
            return inverse;
        }
        
        /// <summary>
        /// Calcul de la norme d'un vecteur
        /// </summary>
        public static double VectorNorm(double[] vector)
        {
            double sum = 0;
            foreach (var value in vector)
            {
                sum += value * value;
            }
            return Math.Sqrt(sum);
        }
        
        /// <summary>
        /// Affichage d'une matrice (pour debug)
        /// </summary>
        public static void PrintMatrix(double[,] matrix, string name = "Matrix")
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            
            System.Console.WriteLine($"\n{name} ({rows}x{cols}):");
            for (int i = 0; i < rows; i++)
            {
                System.Console.Write("  ");
                for (int j = 0; j < cols; j++)
                {
                    System.Console.Write($"{matrix[i, j],10:F6}");
                }
                System.Console.WriteLine();
            }
        }
    }
}