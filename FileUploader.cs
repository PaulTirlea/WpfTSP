using System;
using System.Windows;

namespace WpfTSP
{
    public class FileUploader
    {
        public static double[,] LoadDistanceMatrix(string filePath)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(filePath);

                // Ignorăm prima linie care conține numele orașelor
                var matrix = new double[lines.Length - 1, lines.Length - 1];

                for (int i = 1; i < lines.Length; i++)
                {
                    var values = lines[i].Split('\t');

                    for (int j = 1; j < values.Length; j++)
                    {
                        matrix[i - 1, j - 1] = double.Parse(values[j]);
                    }
                }

                return matrix;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading distance matrix: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}
