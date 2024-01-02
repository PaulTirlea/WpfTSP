using OxyPlot;
using OxyPlot.Series;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfTSP
{
    public class GraphPlotter
    {
        public static void PlotTourWithColor(double[,] coordinates, int[] cityTour, OxyColor lineColor, PlotModel tspPlotModel)
        {
            var lineSeries = new LineSeries
            {
                Color = lineColor,
                MarkerSize = 4,
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColor.FromRgb(128, 0, 128)
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
                tspPlotModel.InvalidatePlot(true);
            });
        }

        public static void PlotTourCoordinates(double[,] coordinates, int[] cityTour, PlotModel tspPlotModel)
        {
            var lineSeries = new LineSeries
            {
                Color = OxyColor.FromRgb(156, 39, 176),
                MarkerSize = 4,
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColor.FromRgb(128, 0, 128)
            };

            cityTour = cityTour.Concat(new[] { cityTour[0] }).ToArray();

            Task.Run(() =>
            {
                for (int i = 0; i < cityTour.Length; i++)
                {
                    int cityIndex = cityTour[i] - 1;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        lineSeries.Points.Add(new DataPoint(coordinates[cityIndex, 0], coordinates[cityIndex, 1]));
                        tspPlotModel.InvalidatePlot(true);
                    });

                    System.Threading.Thread.Sleep(50);
                }
            });

            tspPlotModel.Series.Add(lineSeries);
        }
    }
}
