using Microsoft.Win32;
using OxyPlot;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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

            // Abonare la evenimentul de actualizare a celei mai bune distanțe
            TSPSolver.BestDistanceUpdated += UpdateBestDistance;
        }

        private void InitializeTspPlotModel()
        {
            tspPlotModel = new PlotModel();
            TspPlot.Model = tspPlotModel;
        }

        private void UpdateBestDistance(double bestDistance)
        {
            // Actualizați interfața grafică cu cea mai bună distanță
            Dispatcher.Invoke(() =>
            {
                BestDistanceTextBlock.Text = bestDistance.ToString("F2");
            });
        }


        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            tspPlotModel.Series.Clear();
            if (isRunning)
            {
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

            double[,] distanceMatrix = FileUploader.LoadDistanceMatrix(filePath);

            if (distanceMatrix == null)
            {
                return;
            }

            int maxAttempts = int.TryParse(MaxAttemptsTextBox.Text, out int maxAttemptsValue) ? maxAttemptsValue : 25;
            int neighbourhoodSize = int.TryParse(NeighbourhoodSizeTextBox.Text, out int neighbourhoodSizeValue) ? neighbourhoodSizeValue : 5;
            int iterations = int.TryParse(IterationsTextBox.Text, out int iterationsValue) ? iterationsValue : 1000;

            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            isRunning = true;

            try
            {
                var seed = TSPSolver.SeedFunction(distanceMatrix);
                int[] result;

                if (MethodComboBox.SelectedIndex == 0) // Local Search
                {
                    result = await Task.Run(() =>
                    {
                        return TSPSolver.LocalSearch(distanceMatrix, seed, maxAttempts, neighbourhoodSize, UpdateBestDistance);
                    }, cancellationToken);
                }
                else // Variable Neighborhood Search (VNS)
                {
                    result = await Task.Run(() =>
                    {
                        return TSPSolver.VariableNeighborhoodSearch(distanceMatrix, seed, maxAttempts, neighbourhoodSize, iterations, cancellationToken, tspPlotModel);
                    }, cancellationToken);
                }

                GraphPlotter.PlotTourCoordinates(distanceMatrix, result, tspPlotModel);
            }
            catch (TaskCanceledException)
            {
                isRunning = false;
                cancellationTokenSource.Dispose();
            }
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
            // Oprește algoritmul
            cancellationTokenSource?.Cancel();
            isRunning = false;
        }

        private void MethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Afiseaza/invizibilizeaza inputurile necesare in functie de metoda selectata
            if (MethodComboBox.SelectedIndex == 0) // Local Search
            {
                // Afiseaza sau ascunde inputurile pentru Local Search
                MaxAttemptsTextBox.Visibility = Visibility.Visible;
                NeighbourhoodSizeTextBox.Visibility = Visibility.Visible;
                IterationsTextBox.Visibility = Visibility.Collapsed; // Ascunde inputul pentru Iterations
            }
            else // Variable Neighborhood Search
            {
                // Afiseaza sau ascunde inputurile pentru Variable Neighborhood Search
                MaxAttemptsTextBox.Visibility = Visibility.Visible;
                NeighbourhoodSizeTextBox.Visibility = Visibility.Visible;
                IterationsTextBox.Visibility = Visibility.Visible; // Afiseaza inputul pentru Iterations
            }
        }

    }
}
