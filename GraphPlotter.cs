using OxyPlot;
using OxyPlot.Series;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfTSP
{
    public class GraphPlotter
    {
        /// <summary>
        /// Adaugă un traseu la modelul de grafic cu o culoare specificată.
        /// </summary>
        /// <param name="coordinates">Coordonatele orașelor.</param>
        /// <param name="cityTour">Traseul orașelor.</param>
        /// <param name="lineColor">Culoarea liniei traseului.</param>
        /// <param name="tspPlotModel">Modelul de grafic pentru afișarea traseului.</param>
        public static void PlotTourWithColor(double[,] coordinates, int[] cityTour, OxyColor lineColor, PlotModel tspPlotModel)
        {
            var lineSeries = new LineSeries
            {
                Color = lineColor,
                MarkerSize = 4,
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColor.FromRgb(128, 0, 128)
            };

            // Adaugă primul oraș la sfârșit pentru a închide ciclul traseului
            cityTour = cityTour.Concat(new[] { cityTour[0] }).ToArray();

            // Adaugă punctele în funcție de coordonatele orașelor din traseu
            for (int i = 0; i < cityTour.Length; i++)
            {
                int cityIndex = cityTour[i] - 1;
                lineSeries.Points.Add(new DataPoint(coordinates[cityIndex, 0], coordinates[cityIndex, 1]));
            }

            // Adaugă seria la modelul de grafic și reface afișarea
            Application.Current.Dispatcher.Invoke(() =>
            {
                tspPlotModel.Series.Add(lineSeries);
                tspPlotModel.InvalidatePlot(true);
            });
        }

        /// <summary>
        /// Adaugă un traseu la modelul de grafic folosind coordonatele orașelor.
        /// Afișează gradual punctele pentru un efect de desenare lentă.
        /// </summary>
        /// <param name="coordinates">Coordonatele orașelor.</param>
        /// <param name="cityTour">Traseul orașelor.</param>
        /// <param name="tspPlotModel">Modelul de grafic pentru afișarea traseului.</param>
        public static void PlotTourCoordinates(double[,] coordinates, int[] cityTour, PlotModel tspPlotModel)
        {
            var lineSeries = new LineSeries
            {
                Color = OxyColor.FromRgb(156, 39, 176),
                MarkerSize = 4,
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColor.FromRgb(128, 0, 128)
            };

            // Adaugă primul oraș la sfârșit pentru a închide ciclul traseului
            cityTour = cityTour.Concat(new[] { cityTour[0] }).ToArray();

            // Folosind un fir de execuție separat pentru a afișa gradual punctele
            Task.Run(() =>
            {
                for (int i = 0; i < cityTour.Length; i++)
                {
                    int cityIndex = cityTour[i] - 1;

                    // Afișează punctul curent în modelul de grafic
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        lineSeries.Points.Add(new DataPoint(coordinates[cityIndex, 0], coordinates[cityIndex, 1]));
                        tspPlotModel.InvalidatePlot(true);
                    });

                    // Așteaptă pentru un efect de desenare lentă
                    System.Threading.Thread.Sleep(50);
                }
            });

            // Adaugă seria la modelul de grafic
            tspPlotModel.Series.Add(lineSeries);
        }
    }
}
