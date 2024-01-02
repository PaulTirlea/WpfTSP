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

        /// <summary>
        /// Constructorul clasei MainWindow.
        /// Inițializează componentele și se abonează la evenimentul de actualizare a celei mai bune distanțe.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            InitializeTspPlotModel();

            // Evenimentul de actualizare a celei mai bune distanțe
            TSPSolver.BestDistanceUpdated += UpdateBestDistance;
        }

        /// <summary>
        /// Inițializează modelul de grafic pentru afișarea traseului.
        /// </summary>
        private void InitializeTspPlotModel()
        {
            tspPlotModel = new PlotModel();
            TspPlot.Model = tspPlotModel;
        }

        /// <summary>
        /// Actualizează interfața grafică cu cea mai bună distanță.
        /// </summary>
        /// <param name="bestDistance">Cea mai bună distanță.</param>
        private void UpdateBestDistance(double bestDistance)
        {
            // Actualizați interfața grafică cu cea mai bună distanță
            Dispatcher.Invoke(() =>
            {
                BestDistanceTextBlock.Text = bestDistance.ToString("F2");
            });
        }

        /// <summary>
        /// Gestionează evenimentul de click pe butonul de start/stop al algoritmului.
        /// </summary>
        /// <param name="sender">Obiectul care a generat evenimentul.</param>
        /// <param name="e">Argumentele evenimentului.</param>
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
                MessageBox.Show("Selectați un fișier.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
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

                if (MethodComboBox.SelectedIndex == 0) // Căutare Locală
                {
                    result = await Task.Run(() =>
                    {
                        return TSPSolver.LocalSearch(distanceMatrix, seed, maxAttempts, neighbourhoodSize, UpdateBestDistance);
                    }, cancellationToken);
                }
                else // Căutare în Vecinătate Variabilă (VNS)
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

        /// <summary>
        /// Deschide o fereastră de dialog pentru selectarea unui fișier.
        /// </summary>
        /// <param name="sender">Obiectul care a generat evenimentul.</param>
        /// <param name="e">Argumentele evenimentului.</param>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Fișiere text (*.txt)|*.txt|Toate fișierele (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        /// <summary>
        /// Oprește execuția algoritmului.
        /// </summary>
        /// <param name="sender">Obiectul care a generat evenimentul.</param>
        /// <param name="e">Argumentele evenimentului.</param>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Oprește algoritmul
            cancellationTokenSource?.Cancel();
            isRunning = false;
        }

        /// <summary>
        /// Gestionează schimbarea selecției în combobox-ul de metode.
        /// Afișează/ascunde inputurile necesare în funcție de metoda selectată.
        /// </summary>
        /// <param name="sender">Obiectul care a generat evenimentul.</param>
        /// <param name="e">Argumentele evenimentului.</param>
        private void MethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Afișează/ascunde inputurile necesare în funcție de metoda selectată
            if (MethodComboBox.SelectedIndex == 0) // Căutare Locală
            {
                // Afișează sau ascunde inputurile pentru Căutare Locală
                MaxAttemptsTextBox.Visibility = Visibility.Visible;
                NeighbourhoodSizeTextBox.Visibility = Visibility.Visible;
                IterationsTextBox.Visibility = Visibility.Collapsed; // Ascunde inputul pentru Iterații
            }
            else // Căutare în Vecinătate Variabilă
            {
                // Afișează sau ascunde inputurile pentru Căutare în Vecinătate Variabilă
                MaxAttemptsTextBox.Visibility = Visibility.Visible;
                NeighbourhoodSizeTextBox.Visibility = Visibility.Visible;
                IterationsTextBox.Visibility = Visibility.Visible; // Afișează inputul pentru Iterații
            }
        }

    }
}
