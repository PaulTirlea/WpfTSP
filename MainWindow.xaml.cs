using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfTSP
{
    public partial class MainWindow : Window
    {

        private PlotModel tspPlotModel;
        private CancellationTokenSource cancellationTokenSource;
        private bool isRunning = false;




        public MainWindow()
        {
            InitializeComponent();
            InitializeTspPlotModel();
        }

        private void InitializeTspPlotModel()
        {
            tspPlotModel = new PlotModel();
            TspPlot.Model = tspPlotModel;
        }

        private double[,] LoadDistanceMatrix(string filePath)
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

        private int[] SeedFunction(double[,] distanceMatrix)
        {
            var sequence = Enumerable.Range(1, distanceMatrix.GetLength(0)).OrderBy(x => Guid.NewGuid()).ToArray();
            return sequence.Concat(new[] { sequence[0] }).ToArray();
        }

        private int[] Stochastic2Opt(double[,] distanceMatrix, int[] cityTour)
        {
            var random = new Random();
            int i = random.Next(0, cityTour.Length - 1);
            int j = random.Next(0, cityTour.Length - 1);

            if (i > j)
            {
                (i, j) = (j, i);
            }

            var newTour = cityTour.Take(i)
                .Concat(cityTour.Skip(i).Take(j - i + 1).Reverse())
                .Concat(cityTour.Skip(j + 1))
                .ToArray();

            return newTour;
        }

        private int[] LocalSearch(double[,] distanceMatrix, int[] cityTour, int maxAttempts = 50, int neighbourhoodSize = 5)
        {
            var count = 0;
            var solution = cityTour.ToArray();

            while (count < maxAttempts)
            {
                int[] candidate = null;

                for (var i = 0; i < neighbourhoodSize; i++)
                {
                    candidate = Stochastic2Opt(distanceMatrix, solution);
                }

                if (DistanceCalc(distanceMatrix, candidate) < DistanceCalc(distanceMatrix, solution))
                {
                    solution = candidate.ToArray();
                    count = 0;
                }
                else
                {
                    count++;
                }
            }

            return solution;
        }

        private int[] VariableNeighborhoodSearch(double[,] distanceMatrix, int[] cityTour, int maxAttempts = 20, int neighbourhoodSize = 5, int iterations = 50, CancellationToken cancellationToken = default)
        {
            var count = 0;
            var solution = cityTour.ToArray();
            var bestSolution = cityTour.ToArray();
            double bestDistance = double.MaxValue;

            while (count < iterations)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                for (var i = 0; i < neighbourhoodSize; i++)
                {
                    for (var j = 0; j < neighbourhoodSize; j++)
                    {
                        solution = Stochastic2Opt(distanceMatrix, bestSolution);
                    }

                    solution = LocalSearch(distanceMatrix, solution, maxAttempts, neighbourhoodSize);

                    double currentDistance = DistanceCalc(distanceMatrix, solution);

                    if (currentDistance < bestDistance)
                    {
                        bestDistance = currentDistance;
                        bestSolution = solution.ToArray();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            BestDistanceTextBlock.Text = bestDistance.ToString("F2");
                        });

                        PlotTourWithColor(distanceMatrix, bestSolution, OxyColor.FromRgb(173, 216, 230));
                    }
                }

                count++;
                Console.WriteLine($"Iteration = {count} -> Distance = {bestDistance}");
            }

            return bestSolution;
        }




        private double DistanceCalc(double[,] distanceMatrix, int[] cityTour)
        {
            double distance = 0;

            for (int k = 0; k < cityTour.Length - 1; k++)
            {
                int m = k + 1;
                distance += distanceMatrix[cityTour[k] - 1, cityTour[m] - 1];
            }

            return distance;
        }

        private void PlotTourWithColor(double[,] coordinates, int[] cityTour, OxyColor lineColor)
        {
            var lineSeries = new LineSeries
            {
                Color = lineColor,
                MarkerSize = 4, // Setează dimensiunea marker-elor
                MarkerType = MarkerType.Circle, // Schimbă tipul de marker pentru a fi mai vizibil
                MarkerFill = OxyColor.FromRgb(128, 0, 128) // Schimbă culoarea marker-elor
            };

            cityTour = cityTour.Concat(new[] { cityTour[0] }).ToArray();

            for (int i = 0; i < cityTour.Length; i++)
            {
                int cityIndex = cityTour[i] - 1;
                lineSeries.Points.Add(new DataPoint(coordinates[cityIndex, 0], coordinates[cityIndex, 1]));
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                tspPlotModel.Series.Add(lineSeries);
                TspPlot.InvalidatePlot(true);
            });
        }



        private void PlotTourCoordinates(double[,] coordinates, int[] cityTour)
        {
            // Add a line series with the desired color
            var lineSeries = new LineSeries
            {
                Color = OxyColor.FromRgb(156, 39, 176)
            };

            // Adăugați ultimul punct pentru a crea un ciclu închis
            cityTour = cityTour.Concat(new[] { cityTour[0] }).ToArray();

            Task.Run(() =>
            {
                for (int i = 0; i < cityTour.Length; i++)
                {
                    // Convertiți orașul la indexul corect (începând de la 1)
                    int cityIndex = cityTour[i] - 1;

                    // Adăugați o linie de debug pentru a verifica valorile
                    Console.WriteLine($"City: {cityTour[i]}, Coordinates: ({coordinates[cityIndex, 0]}, {coordinates[cityIndex, 1]})");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        lineSeries.Points.Add(new DataPoint(coordinates[cityIndex, 0], coordinates[cityIndex, 1]));

                        // Refresh the plot
                        TspPlot.InvalidatePlot(true);
                    });

                    // Așteptați o scurtă pauză pentru a vedea modificarea în timp real
                    System.Threading.Thread.Sleep(50);
                }
            });

            // Add the new line series to the existing series
            tspPlotModel.Series.Add(lineSeries);
        }



        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                // Stop the algorithm
                cancellationTokenSource?.Cancel();
                isRunning = false;
                return;
            }

            string filePath = FilePathTextBox.Text;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                MessageBox.Show("Please select a file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            double[,] distanceMatrix = LoadDistanceMatrix(filePath);

            if (distanceMatrix == null)
            {
                return;
            }

            // Utilizați valorile din interfață sau valorile implicite dacă nu există nicio valoare validă
            int maxAttempts = int.TryParse(MaxAttemptsTextBox.Text, out int maxAttemptsValue) ? maxAttemptsValue : 25;
            int neighbourhoodSize = int.TryParse(NeighbourhoodSizeTextBox.Text, out int neighbourhoodSizeValue) ? neighbourhoodSizeValue : 5;
            int iterations = int.TryParse(IterationsTextBox.Text, out int iterationsValue) ? iterationsValue : 1000;

            // Set the cancellation token source for stopping the algorithm
            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            isRunning = true;

            try
            {
                // Use Task.Run to run the algorithm on a separate thread
                var lsvns = await Task.Run(() =>
                {
                    var seed = SeedFunction(distanceMatrix);
                    return VariableNeighborhoodSearch(distanceMatrix, seed, maxAttempts, neighbourhoodSize, iterations, cancellationToken);
                }, cancellationToken);

                // Update the UI with the result
                PlotTourCoordinates(distanceMatrix, lsvns);
            }
            catch (TaskCanceledException)
            {
                // Task was canceled (stop button was pressed)
                isRunning = false;
                cancellationTokenSource.Dispose();
            };
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop the algorithm
            cancellationTokenSource?.Cancel();
            isRunning = false;
        }

    }
}